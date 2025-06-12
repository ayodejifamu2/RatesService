using RatesService.Domain.Aggregates;
using RatesService.Domain.ValueObjects;
using Xunit;

namespace RatesService.Tests.Domain;

public class CryptoInstrumentTests
{
    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        string symbol = "BTC";
        string name = "Bitcoin";
        var initialRate = new Money(60000m, "USD");
        var lastUpdated = DateTime.UtcNow;

        // Act
        var instrument = new CryptoInstrument(symbol, name, initialRate, lastUpdated);

        // Assert
        Assert.NotEqual(Guid.Empty, instrument.Id);
        Assert.Equal(symbol, instrument.Symbol);
        Assert.Equal(name, instrument.Name);
        Assert.Equal(initialRate, instrument.CurrentRate);
        Assert.Equal(lastUpdated, instrument.LastUpdated);
        Assert.Single(instrument.HistoricalRates);
        Assert.Equal(initialRate, instrument.HistoricalRates.First().Rate);
        Assert.Equal(lastUpdated, instrument.HistoricalRates.First().Timestamp);
    }

    [Theory]
    [InlineData(null, "Bitcoin", 60000, "USD")]
    [InlineData("BTC", null, 60000, "USD")]
    public void Constructor_ShouldThrowArgumentException_ForInvalidArguments(
        string symbol, string name, decimal rateAmount, string currency)
    {
        // Arrange
        Money rate = null;
        if (currency != null)
        {
            try { rate = new Money(rateAmount, currency); }
            catch { }
        }

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new CryptoInstrument(symbol, name, rate, DateTime.UtcNow));
    }

    [Fact]
    public void UpdateRate_ShouldUpdateCurrentRateAndAddHistoricalRecord()
    {
        // Arrange
        var instrument = new CryptoInstrument("ETH", "Ethereum", new Money(3000m, "USD"), DateTime.UtcNow.AddMinutes(-10));
        var newRate = new Money(3050m, "USD");
        var newTimestamp = DateTime.UtcNow;

        // Act
        instrument.UpdateRate(newRate, newTimestamp);

        // Assert
        Assert.Equal(newRate, instrument.CurrentRate);
        Assert.Equal(newTimestamp, instrument.LastUpdated);
        Assert.Equal(2, instrument.HistoricalRates.Count);
        Assert.Contains(instrument.HistoricalRates, hr => hr.Rate == newRate && hr.Timestamp == newTimestamp);
    }

    [Fact]
    public void UpdateRate_ShouldThrowInvalidOperationException_ForOlderTimestamp()
    {
        // Arrange
        var initialTimestamp = DateTime.UtcNow;
        var instrument = new CryptoInstrument("LTC", "Litecoin", new Money(100m, "USD"), initialTimestamp);
        var newRate = new Money(101m, "USD");
        var olderTimestamp = initialTimestamp.AddMinutes(-5);
        var sameTimestamp = initialTimestamp;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => instrument.UpdateRate(newRate, olderTimestamp));
        Assert.Throws<InvalidOperationException>(() => instrument.UpdateRate(newRate, sameTimestamp));
    }

    [Fact]
    public void UpdateRate_ShouldThrowInvalidOperationException_ForDifferentCurrencies()
    {
        // Arrange
        var instrument = new CryptoInstrument("XRP", "Ripple", new Money(0.5m, "USD"), DateTime.UtcNow.AddMinutes(-10));
        var newRate = new Money(0.55m, "EUR");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => instrument.UpdateRate(newRate, DateTime.UtcNow));
    }

    [Fact]
    public void GetOldestRateWithin_ShouldReturnOldestRateInTimeframe()
    {
        // Arrange
        var instrument = new CryptoInstrument("ADA", "Cardano", new Money(0.5m, "USD"), DateTime.UtcNow.AddHours(-25)); 
        instrument.UpdateRate(new Money(0.51m, "USD"), DateTime.UtcNow.AddHours(-23));
        instrument.UpdateRate(new Money(0.52m, "USD"), DateTime.UtcNow.AddHours(-12));
        instrument.UpdateRate(new Money(0.53m, "USD"), DateTime.UtcNow.AddMinutes(-5));

        // Act
        var oldestRate = instrument.GetOldestRateWithin(TimeSpan.FromHours(24));

        // Assert
        Assert.NotNull(oldestRate);
        Assert.Equal(0.51m, oldestRate.Rate.Amount);
        Assert.True(oldestRate.Timestamp > DateTime.UtcNow.AddHours(-24));
    }

    [Fact]
    public void GetOldestRateWithin_ShouldReturnNull_IfNoRatesInTimeframe()
    {
        // Arrange
        var instrument = new CryptoInstrument("SOL", "Solana", new Money(150m, "USD"), DateTime.UtcNow.AddHours(-30));

        // Act
        var oldestRate = instrument.GetOldestRateWithin(TimeSpan.FromHours(24));

        // Assert
        Assert.Null(oldestRate);
    }

    [Fact]
    public void HistoricalRates_ShouldBeCleanedAutomatically()
    {
        // Arrange
        var instrument = new CryptoInstrument("DOT", "Polkadot", new Money(10m, "USD"), DateTime.UtcNow.AddHours(-30));
        instrument.UpdateRate(new Money(10.1m, "USD"), DateTime.UtcNow.AddHours(-28));
        instrument.UpdateRate(new Money(10.2m, "USD"), DateTime.UtcNow.AddHours(-26));
        instrument.UpdateRate(new Money(10.3m, "USD"), DateTime.UtcNow.AddHours(-22));
        instrument.UpdateRate(new Money(10.4m, "USD"), DateTime.UtcNow.AddHours(-1));

        // Assert
        Assert.True(instrument.HistoricalRates.All(hr => hr.Timestamp >= DateTime.UtcNow.AddHours(-24).AddMinutes(-5)));
        Assert.Equal(2, instrument.HistoricalRates.Count); // Expect only rates from -22h and -1h after cleaning
    }
}