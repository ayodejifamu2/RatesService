using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RatesService.Domain.Aggregates;
using RatesService.Domain.ValueObjects;

namespace RatesService.Infrastructure.Data;

public static class RatesServiceSeeder
{
    public static async Task SeedInitialData(RatesServiceDbContext dbContext, ILogger logger)
    {
        if (await dbContext.CryptoInstruments.AnyAsync(ci => ci.Symbol == "BTC"))
        {
            logger.LogInformation("BTC already exists, skipping seeding.");
            return;
        }

        logger.LogInformation("Seeding initial BTC data...");

        var btcInstrument = new CryptoInstrument("BTC", "Bitcoin", new Money(58400.82m, "USD"), DateTime.UtcNow.AddHours(-25));

        btcInstrument.UpdateRate(new Money(59000.00m, "USD"), DateTime.UtcNow.AddHours(-23));

        await dbContext.CryptoInstruments.AddAsync(btcInstrument);

        await dbContext.SaveChangesAsync();
        logger.LogInformation("Seeded BTC with initial rate {RateAmount1} {Currency} at {Timestamp1} and historical rate {RateAmount2} {Currency} at {Timestamp2}",
            btcInstrument.HistoricalRates.OrderBy(hr => hr.Timestamp).First().Rate.Amount,
            btcInstrument.HistoricalRates.First().Rate.Currency,
            btcInstrument.HistoricalRates.OrderBy(hr => hr.Timestamp).First().Timestamp,
            btcInstrument.CurrentRate.Amount,
            btcInstrument.CurrentRate.Currency,
            btcInstrument.LastUpdated);
    }
}