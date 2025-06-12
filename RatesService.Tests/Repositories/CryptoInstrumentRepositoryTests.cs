using Microsoft.EntityFrameworkCore;
using Moq;
using RatesService.Domain.Aggregates;
using RatesService.Domain.ValueObjects;
using RatesService.Infrastructure.Data;
using RatesService.Infrastructure.Repositories;
using RatesService.Tests.Repositories.Fixture;
using Xunit;

namespace RatesService.Tests.Repositories;

public class CryptoInstrumentRepositoryTests : IDisposable
{
    private readonly RatesServiceDbContext _dbContext;
    private readonly CryptoInstrumentRepository _repository;

    public CryptoInstrumentRepositoryTests()
    {
        // Configure DbContext to use an in-memory database
        var options = new DbContextOptionsBuilder<RatesServiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new RatesServiceDbContext(options);
        _dbContext.Database.EnsureCreated();

        _repository = new CryptoInstrumentRepository(_dbContext);
    }

    [Fact]
    public async Task GetBySymbolAsync_ShouldReturnInstrument_WhenFoundIncludingOwnedTypes()
    {
        // Arrange
        var symbol = "BTC";
        var initialRate = new Money(60000m, "USD");
        var instrumentToSeed = new CryptoInstrument(symbol, "Bitcoin", initialRate, DateTime.UtcNow.AddDays(-1));
        instrumentToSeed.UpdateRate(new Money(61000m, "USD"), DateTime.UtcNow.AddHours(-12));
        instrumentToSeed.UpdateRate(new Money(62000m, "USD"), DateTime.UtcNow.AddMinutes(-5));

        await _dbContext.CryptoInstruments.AddAsync(instrumentToSeed);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetBySymbolAsync(symbol);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(instrumentToSeed.Id, result.Id);
        Assert.Equal(symbol, result.Symbol);
        Assert.NotNull(result.CurrentRate);
        Assert.Equal(62000m, result.CurrentRate.Amount);
        Assert.Equal("USD", result.CurrentRate.Currency);

        Assert.NotNull(result.HistoricalRates);
        Assert.True(result.HistoricalRates.Any());
        Assert.Equal(3, result.HistoricalRates.Count);
        Assert.Contains(result.HistoricalRates, hr => hr.Rate.Amount == 60000m);
        Assert.Contains(result.HistoricalRates, hr => hr.Rate.Amount == 61000m);
        Assert.Contains(result.HistoricalRates, hr => hr.Rate.Amount == 62000m);
    }

    [Fact]
    public async Task GetBySymbolAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange (no data seeded)
        var symbol = "XYZ";

        // Act
        var result = await _repository.GetBySymbolAsync(symbol);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_ShouldAddInstrumentToDatabase()
    {
        // Arrange
        var newInstrument = new CryptoInstrument("NEW", "New Coin", new Money(1.0m, "USD"), DateTime.UtcNow);

        // Act
        await _repository.AddAsync(newInstrument);
        await _dbContext.SaveChangesAsync();

        // Assert
        var fetchedInstrument = await _dbContext.CryptoInstruments
                                                .Include(ci => ci.CurrentRate)
                                                .Include(ci => ci.HistoricalRates)
                                                    .ThenInclude(hr => hr.Rate)
                                                .FirstOrDefaultAsync(ci => ci.Id == newInstrument.Id);

        Assert.NotNull(fetchedInstrument);
        Assert.Equal(newInstrument.Symbol, fetchedInstrument.Symbol);
        Assert.Equal(newInstrument.CurrentRate, fetchedInstrument.CurrentRate);
        Assert.Single(fetchedInstrument.HistoricalRates);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateInstrumentInDatabase()
    {
        // Arrange
        var existingInstrument = new CryptoInstrument("UPD", "Updated Coin", new Money(2.0m, "USD"), DateTime.UtcNow);
        await _dbContext.CryptoInstruments.AddAsync(existingInstrument);

        var newRate = new Money(2.5m, "USD");
        var currentDate = DateTime.UtcNow.AddMinutes(1);
        existingInstrument.UpdateRate(newRate, currentDate);

        // Act
        await _dbContext.SaveChangesAsync();

        // Assert
        var fetchedInstrument = await _dbContext.CryptoInstruments
                                                .Include(ci => ci.CurrentRate)
                                                .Include(ci => ci.HistoricalRates)
                                                    .ThenInclude(hr => hr.Rate)
                                                .FirstOrDefaultAsync(ci => ci.Id == existingInstrument.Id);

        Assert.NotNull(fetchedInstrument);
        Assert.Equal(newRate, fetchedInstrument.CurrentRate);
        Assert.Equal(2, fetchedInstrument.HistoricalRates.Count);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}