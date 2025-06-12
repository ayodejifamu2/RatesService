using RatesService.Domain.ValueObjects;
using Xunit;

namespace RatesService.Tests.Domain;

public class MoneyTests
    {
        [Fact]
        public void Constructor_ShouldCreateMoneyObject_WithValidArguments()
        {
            // Arrange
            decimal amount = 100.50m;
            string currency = "USD";

            // Act
            var money = new Money(amount, currency);

            // Assert
            Assert.Equal(amount, money.Amount);
            Assert.Equal(currency, money.Currency);
        }

        [Theory]
        [InlineData(-1.00, "USD")]
        [InlineData(10.00, null)]
        [InlineData(10.00, "")]
        [InlineData(10.00, " ")]
        public void Constructor_ShouldThrowArgumentException_ForInvalidArguments(decimal amount, string currency)
        {
            // Act & Assert
            if (amount < 0)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new Money(amount, currency));
            }
            else
            {
                Assert.Throws<ArgumentException>(() => new Money(amount, currency));
            }
        }

        [Fact]
        public void Equals_ShouldReturnTrue_ForSameValueObjects()
        {
            // Arrange
            var money1 = new Money(100.50m, "USD");
            var money2 = new Money(100.50m, "USD");

            // Act & Assert
            Assert.True(money1.Equals(money2));
            Assert.True(money1 == money2);
            Assert.Equal(money1.GetHashCode(), money2.GetHashCode());
        }

        [Fact]
        public void Equals_ShouldReturnFalse_ForDifferentValueObjects()
        {
            // Arrange
            var money1 = new Money(100.50m, "USD");
            var money2 = new Money(200.00m, "USD");
            var money3 = new Money(100.50m, "EUR");

            // Act & Assert
            Assert.False(money1.Equals(money2));
            Assert.False(money1 == money2);
            Assert.False(money1.Equals(money3));
            Assert.False(money1 == money3);
        }

        [Fact]
        public void Add_ShouldCorrectlyAddAmounts_WithSameCurrency()
        {
            // Arrange
            var money1 = new Money(50.00m, "USD");
            var money2 = new Money(75.50m, "USD");

            // Act
            var result = money1.Add(money2);

            // Assert
            Assert.Equal(125.50m, result.Amount);
            Assert.Equal("USD", result.Currency);
        }

        [Fact]
        public void Add_ShouldThrowInvalidOperationException_ForDifferentCurrencies()
        {
            // Arrange
            var money1 = new Money(50.00m, "USD");
            var money2 = new Money(75.50m, "EUR");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => money1.Add(money2));
        }

        [Fact]
        public void Multiply_ShouldCorrectlyMultiplyAmount()
        {
            // Arrange
            var money = new Money(25.00m, "USD");
            decimal factor = 2.5m;

            // Act
            var result = money.Multiply(factor);

            // Assert
            Assert.Equal(62.50m, result.Amount);
            Assert.Equal("USD", result.Currency);
        }

        [Fact]
        public void Multiply_ShouldHandleZeroFactor()
        {
            // Arrange
            var money = new Money(25.00m, "USD");
            decimal factor = 0m;

            // Act
            var result = money.Multiply(factor);

            // Assert
            Assert.Equal(0m, result.Amount);
            Assert.Equal("USD", result.Currency);
        }
    }