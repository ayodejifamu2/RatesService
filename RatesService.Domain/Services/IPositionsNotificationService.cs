using RatesService.Domain.ValueObjects;

namespace RatesService.Domain.Services;

public interface IPositionsNotificationService
{
    // Defines the contract for notifying the Positions Service
    Task NotifyRateChangeAsync(string symbol, Money newRate);
}