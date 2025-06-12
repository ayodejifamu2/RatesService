using RatesService.Domain.Aggregates;

namespace RatesService.Domain.Repositories;

public interface ICryptoInstrumentRepository
{
    Task<CryptoInstrument> GetBySymbolAsync(string symbol);
    Task AddAsync(CryptoInstrument instrument);
    Task UpdateAsync(CryptoInstrument instrument);
}