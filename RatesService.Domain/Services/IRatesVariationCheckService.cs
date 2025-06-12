using RatesService.Domain.Aggregates;
using RatesService.Domain.ValueObjects;

namespace RatesService.Domain.Services;

public interface IRatesVariationCheckService
{
    RateVariationCheckResult CheckVariation(CryptoInstrument instrument, decimal percentageThreshold);

    
}

public class RateVariationCheckResult(
    string symbol,
    bool isSignificant,
    decimal percentageChange,
    Money? oldestRate,
    Money currentRate)
{
    public bool IsSignificant { get; } = isSignificant;
    public decimal PercentageChange { get; } = percentageChange;
    public Money? OldestRate { get; } = oldestRate; // Nullable if oldest rate might not exist
    public Money CurrentRate { get; } = currentRate;
    public string Symbol { get; } = symbol;
}