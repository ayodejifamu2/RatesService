using RatesService.Domain.ValueObjects;

namespace RatesService.Domain.Entities;

public class HistoricalRate
{
    public Guid Id { get; private set; }
    public DateTime Timestamp { get; private set; }
    public Money Rate { get; private set; } // Value Object

    // Private constructor for ORM materialization
    private HistoricalRate() { }

    // Constructor called by the CryptoInstrument aggregate root
    internal HistoricalRate(DateTime timestamp, Money rate)
    {
        if (rate == null) throw new ArgumentNullException(nameof(rate));
        // No default check for timestamp, as DateTime.MinValue is a valid default for some ORMs
        // In C# 10+, use 'DateOnly' if you only care about date
        // or enforce UTC if always storing UTC: timestamp.Kind != DateTimeKind.Utc

        Id = Guid.NewGuid();
        Timestamp = timestamp;
        Rate = rate;
    }
}