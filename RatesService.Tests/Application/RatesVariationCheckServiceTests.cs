using Microsoft.Extensions.Logging;
using Moq;
using RatesService.Application.Services;
using RatesService.Domain.Aggregates;
using RatesService.Domain.Entities;
using RatesService.Domain.ValueObjects;
using Xunit;

namespace RatesService.Tests.Application;

public class RatesVariationCheckServiceTests
    {
        private readonly RatesVariationCheckService _service;
        private readonly Mock<ILogger<RatesVariationCheckService>> _mockLogger;

        public RatesVariationCheckServiceTests()
        {
            _mockLogger = new Mock<ILogger<RatesVariationCheckService>>();
            _service = new RatesVariationCheckService(_mockLogger.Object);
        }

        [Fact]
        public void CheckVariation_ShouldReturnSignificant_WhenVariationExceedsThreshold()
        {
            // Arrange
            var initialRateTime = DateTime.UtcNow.AddHours(-23);
            var initialRate = new Money(58400.82m, "USD");
            var currentRate = new Money(61800.00m, "USD");

            var instrument = new CryptoInstrument("BTC", "Bitcoin", initialRate, initialRateTime.AddHours(-1)); 
            instrument.UpdateRate(initialRate, initialRateTime);
            instrument.UpdateRate(currentRate, DateTime.UtcNow);


            decimal threshold = 0.05m;

            // Act
            var result = _service.CheckVariation(instrument, threshold);

            // Assert
            Assert.True(result.IsSignificant);
            Assert.True(result.PercentageChange > threshold);
            Assert.Equal("BTC", result.Symbol);
            Assert.Equal(initialRate, result.OldestRate);
            Assert.Equal(currentRate, result.CurrentRate);
        }

        [Fact]
        public void CheckVariation_ShouldReturnNotSignificant_WhenVariationIsBelowThreshold()
        {
            // Arrange
            var initialRateTime = DateTime.UtcNow.AddHours(-23);
            var initialRate = new Money(58400.82m, "USD");
            var currentRate = new Money(59000.00m, "USD");

            var instrument = new CryptoInstrument("BTC", "Bitcoin", initialRate, initialRateTime.AddHours(-1));
            instrument.UpdateRate(initialRate, initialRateTime);
            instrument.UpdateRate(currentRate, DateTime.UtcNow);

            decimal threshold = 0.05m;

            // Act
            var result = _service.CheckVariation(instrument, threshold);

            // Assert
            Assert.False(result.IsSignificant);
            Assert.True(result.PercentageChange < threshold);
            Assert.Equal("BTC", result.Symbol);
            Assert.Equal(initialRate, result.OldestRate);
            Assert.Equal(currentRate, result.CurrentRate);
        }

        [Fact]
        public void CheckVariation_ShouldReturnNotSignificant_WhenNoHistoricalDataInTimeframe()
        {
            // Arrange
            var instrument = new CryptoInstrument("ETH", "Ethereum", new Money(3000m, "USD"), DateTime.UtcNow.AddHours(-30));

            decimal threshold = 0.05m;

            // Act
            var result = _service.CheckVariation(instrument, threshold);

            // Assert
            Assert.False(result.IsSignificant);
            Assert.Equal(0m, result.PercentageChange);
            Assert.Null(result.OldestRate);
            Assert.Equal(instrument.CurrentRate, result.CurrentRate);
        }

        [Fact]
        public void CheckVariation_ShouldReturnNotSignificant_WhenOldestRateIsZero()
        {
            // Arrange
            var initialRateTime = DateTime.UtcNow.AddHours(-23);
            var initialRate = new Money(0m, "USD");
            var currentRate = new Money(10m, "USD");

            var instrument = new CryptoInstrument("DOGE", "Dogecoin", initialRate, initialRateTime.AddHours(-1));
            instrument.UpdateRate(initialRate, initialRateTime);
            instrument.UpdateRate(currentRate, DateTime.UtcNow);

            decimal threshold = 0.05m;

            // Act
            var result = _service.CheckVariation(instrument, threshold);

            // Assert
            Assert.False(result.IsSignificant); // Cannot calculate meaningful percentage change from zero
            Assert.Equal(0m, result.PercentageChange);
            Assert.Equal(initialRate, result.OldestRate);
            Assert.Equal(currentRate, result.CurrentRate);
        }
    }