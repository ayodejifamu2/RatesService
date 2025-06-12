using RatesService.Domain.Entities;
using RatesService.Domain.ValueObjects;

namespace RatesService.Domain.Aggregates;

public class CryptoInstrument
{
    public Guid Id { get; private set; }
    public string Symbol { get; private set; }
    public string Name { get; private set; }
    public Money CurrentRate { get; private set; }
    public DateTime LastUpdated { get; private set; }

    private readonly List<HistoricalRate> _historicalRates;
    public IReadOnlyCollection<HistoricalRate> HistoricalRates => _historicalRates.AsReadOnly();

    private CryptoInstrument()
    {
        _historicalRates = new List<HistoricalRate>();
    }

    public CryptoInstrument(string symbol, string name, Money initialRate, DateTime lastUpdated)
    {
        if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("Symbol cannot be empty.", nameof(symbol));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty.", nameof(name));
        if (initialRate == null) throw new ArgumentNullException(nameof(initialRate));
        if (lastUpdated == default) throw new ArgumentException("LastUpdated timestamp cannot be default.", nameof(lastUpdated));


        Id = Guid.NewGuid();
        Symbol = symbol.ToUpperInvariant(); 
        Name = name;
        CurrentRate = initialRate;
        LastUpdated = lastUpdated;
        _historicalRates = new List<HistoricalRate>();
        AddHistoricalRate(initialRate, lastUpdated);
    }

    public void UpdateRate(Money newRate, DateTime updatedTimestamp)
    {
        if (newRate == null) throw new ArgumentNullException(nameof(newRate));
        if (newRate.Currency != CurrentRate.Currency)
        {
            throw new InvalidOperationException("Cannot update rate with a different currency.");
        }
        if (updatedTimestamp <= LastUpdated)
        {
            throw new InvalidOperationException("Cannot update rate with an older or same timestamp.");
        }

        CurrentRate = newRate;
        LastUpdated = updatedTimestamp;
        AddHistoricalRate(newRate, updatedTimestamp);
    }

    private void AddHistoricalRate(Money rate, DateTime timestamp)
    {
        _historicalRates.Add(new HistoricalRate(timestamp, rate));
        CleanOldHistoricalRates();
    }

    private void CleanOldHistoricalRates()
    {
        var twentyFourHoursAgo = DateTime.UtcNow.AddHours(-24).AddMinutes(-5);
        _historicalRates.RemoveAll(hr => hr.Timestamp < twentyFourHoursAgo);
    }

    public HistoricalRate GetOldestRateWithin(TimeSpan timeSpan)
    {
        var threshold = DateTime.UtcNow.Subtract(timeSpan);
        return _historicalRates
            .Where(hr => hr.Timestamp >= threshold)
            .OrderBy(hr => hr.Timestamp)
            .FirstOrDefault();
    }
}