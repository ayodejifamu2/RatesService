using Microsoft.Extensions.Logging;
using RatesService.Domain.Aggregates;
using RatesService.Domain.Services;

namespace RatesService.Application.Services;

public class RatesVariationCheckService(ILogger<RatesVariationCheckService> logger) : IRatesVariationCheckService
{
    private const int ComparisonTimeframeHours = 24;
    private readonly ILogger<RatesVariationCheckService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public RateVariationCheckResult CheckVariation(CryptoInstrument instrument, decimal percentageThreshold)
    {
        var oldestRateEntry = instrument.GetOldestRateWithin(TimeSpan.FromHours(ComparisonTimeframeHours));

        if (oldestRateEntry == null || oldestRateEntry.Rate == null || oldestRateEntry.Rate.Amount == 0)
        {
            _logger.LogDebug("  No sufficient historical data (24h) or oldest rate is zero for {Symbol}. Cannot perform variation check.", instrument.Symbol);
            return new RateVariationCheckResult(instrument.Symbol, false, 0m, oldestRateEntry?.Rate, instrument.CurrentRate);
        }

        var oldestRate = oldestRateEntry.Rate;
        var currentRate = instrument.CurrentRate;

        if (oldestRate.Currency != currentRate.Currency)
        {
            _logger.LogError("  Currency mismatch between oldest ({OldCurrency}) and current ({CurrentCurrency}) rates for {Symbol}. Cannot calculate variation.",
                oldestRate.Currency, currentRate.Currency, instrument.Symbol);
            return new RateVariationCheckResult(instrument.Symbol, false, 0m, oldestRate, currentRate);
        }

        decimal percentageChange = 0m;
        if (oldestRate.Amount != 0) { percentageChange = Math.Abs((currentRate.Amount - oldestRate.Amount) / oldestRate.Amount); }

        bool isSignificant = percentageChange > percentageThreshold;

        return new RateVariationCheckResult(instrument.Symbol, isSignificant, percentageChange, oldestRate, currentRate);
    }
}