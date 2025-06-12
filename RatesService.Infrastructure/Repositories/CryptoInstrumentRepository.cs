using Microsoft.EntityFrameworkCore;
using RatesService.Domain.Aggregates;
using RatesService.Domain.Repositories;
using RatesService.Infrastructure.Data;

namespace RatesService.Infrastructure.Repositories;

public class CryptoInstrumentRepository : ICryptoInstrumentRepository
{
    private readonly RatesServiceDbContext _dbContext;

    public CryptoInstrumentRepository(RatesServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CryptoInstrument> GetBySymbolAsync(string symbol)
    {
        return await _dbContext.CryptoInstruments
            .Include(ci => ci.CurrentRate)
            .Include(ci => ci.HistoricalRates)
            .ThenInclude(hr => hr.Rate)
            .FirstOrDefaultAsync(ci => ci.Symbol == symbol.ToUpperInvariant());
    }

    public async Task AddAsync(CryptoInstrument instrument)
    {
        await _dbContext.CryptoInstruments.AddAsync(instrument);
    }

    public Task UpdateAsync(CryptoInstrument instrument)
    {
        var entry = _dbContext.Entry(instrument);
        if (entry.State == EntityState.Detached)
        {
            _dbContext.CryptoInstruments.Attach(instrument);
            entry.State = EntityState.Modified;
        }
        return Task.CompletedTask;
    }
}