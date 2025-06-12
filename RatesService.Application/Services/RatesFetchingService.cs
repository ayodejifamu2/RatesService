using System.Text.Json;
using Microsoft.Extensions.Logging;
using RatesService.Domain.Aggregates;
using RatesService.Domain.Repositories;
using RatesService.Domain.Services;
using RatesService.Domain.ValueObjects;
using RatesService.Infrastructure.Data;
using RatesService.Infrastructure.ExternalServices;

namespace RatesService.Application.Services;

public class RatesFetchingService(
    CoinMarketCapApiClient apiClient,
    ICryptoInstrumentRepository cryptoInstrumentRepository,
    IRatesVariationCheckService ratesVariationCheckService,
    IPositionsNotificationService positionsNotificationService,
    RatesServiceDbContext dbContext,
    ILogger<RatesFetchingService> logger)
{
        private readonly CoinMarketCapApiClient _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        private readonly ICryptoInstrumentRepository _cryptoInstrumentRepository = cryptoInstrumentRepository ?? throw new ArgumentNullException(nameof(cryptoInstrumentRepository));
        private readonly IRatesVariationCheckService _ratesVariationCheckService = ratesVariationCheckService ?? throw new ArgumentNullException(nameof(ratesVariationCheckService));
        private readonly IPositionsNotificationService _positionsNotificationService = positionsNotificationService ?? throw new ArgumentNullException(nameof(positionsNotificationService));
        private readonly RatesServiceDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        private readonly ILogger<RatesFetchingService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        private const decimal _rateVariationThreshold = 0.05m;

        public async Task FetchAndProcessRatesAsync()
        {
            _logger.LogInformation("RatesFetchingService: Trigger received. Fetching rates from CoinMarketCap...");

            try
            {
                var apiResponse = await _apiClient.GetLatestListingsAsync();
                if (apiResponse?.data == null || !apiResponse.data.Any()) { _logger.LogWarning("CoinMarketCap API returned no data."); return; }

                foreach (var cryptoData in apiResponse.data)
                {
                    if (cryptoData.quote.USD != null)
                    {
                        var symbol = cryptoData.symbol;
                        var newRateMoney = new Money(Convert.ToDecimal(cryptoData.quote.USD.price), nameof(cryptoData.quote.USD));
                        var lastUpdated = cryptoData.last_updated;

                        using (var transaction = await _dbContext.Database.BeginTransactionAsync())
                        {
                            try
                            {
                                var instrument = await _cryptoInstrumentRepository.GetBySymbolAsync(symbol);

                                if (instrument == null)
                                {
                                    instrument = new CryptoInstrument(symbol, cryptoData.name, newRateMoney, lastUpdated);
                                    await _cryptoInstrumentRepository.AddAsync(instrument);
                                    _logger.LogInformation("  Added new instrument: {Symbol} at {RateAmount}{RateCurrency}",
                                        instrument.Symbol, instrument.CurrentRate.Amount, instrument.CurrentRate.Currency);
                                }
                                else if (lastUpdated > instrument.LastUpdated)
                                {
                                    instrument.UpdateRate(newRateMoney, lastUpdated);
                                    await _cryptoInstrumentRepository.UpdateAsync(instrument);
                                    _logger.LogInformation("  Updated instrument: {Symbol} to {RateAmount}{RateCurrency}",
                                        instrument.Symbol, instrument.CurrentRate.Amount, instrument.CurrentRate.Currency);
                                }
                                else
                                {
                                    _logger.LogInformation("  Instrument {Symbol} current data is not newer, skipping update.", symbol);
                                    await transaction.RollbackAsync();
                                    continue;
                                }

                                await _dbContext.SaveChangesAsync();

                                var checkResult = _ratesVariationCheckService.CheckVariation(instrument, _rateVariationThreshold);

                                if (checkResult.IsSignificant)
                                {
                                    _logger.LogWarning("  Significant variation for {Symbol}: {PercentageChange:P2} (Old: {OldRateAmount}, Current: {CurrentRateAmount})",
                                        checkResult.Symbol, checkResult.PercentageChange, checkResult.OldestRate?.Amount, checkResult.CurrentRate.Amount);
                                    await _positionsNotificationService.NotifyRateChangeAsync(checkResult.Symbol, checkResult.CurrentRate); // Call remains the same
                                }
                                else
                                {
                                    _logger.LogInformation("  No significant variation for {Symbol} ({PercentageChange:P2})",
                                        checkResult.Symbol, checkResult.PercentageChange);
                                }

                                await transaction.CommitAsync();
                            }
                            catch (Exception ex)
                            {
                                await transaction.RollbackAsync();
                                _logger.LogError(ex, "  Error processing crypto instrument {Symbol}: {Message}", cryptoData.symbol, ex.Message);
                            }
                        }
                    }
                    else { _logger.LogWarning("  No USD quote found for crypto instrument: {Symbol}. Skipping.", cryptoData.symbol); }
                }
            }
            catch (HttpRequestException ex) { _logger.LogError(ex, "HTTP request error fetching rates from CoinMarketCap: {Message}", ex.Message); }
            catch (JsonException ex) { _logger.LogError(ex, "JSON parsing error from CoinMarketCap API response: {Message}", ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "An unexpected error occurred during rates fetching: {Message}", ex.Message); }
            _logger.LogInformation("RatesFetchingService: Rates fetching cycle completed.");
        }
    }