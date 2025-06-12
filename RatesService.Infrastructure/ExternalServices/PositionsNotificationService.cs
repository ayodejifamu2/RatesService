using Microsoft.Extensions.Logging;
using RatesService.Domain.Services;
using RatesService.Domain.ValueObjects;
using RatesService.Infrastructure.Messaging.Contracts;

namespace RatesService.Infrastructure.ExternalServices;

public class PositionsNotificationService(
    IMessageProducer<RateChangeMessage> messageProducer,
    ILogger<PositionsNotificationService> logger)
    : IPositionsNotificationService
{
    private readonly IMessageProducer<RateChangeMessage> _messageProducer = messageProducer ?? throw new ArgumentNullException(nameof(messageProducer));
    private readonly ILogger<PositionsNotificationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task NotifyRateChangeAsync(string symbol, Money newRate)
    {
        var message = new RateChangeMessage(
            InstrumentSymbol: symbol,
            CurrentRateAmount: newRate.Amount,
            Currency: newRate.Currency,
            Timestamp: DateTime.UtcNow
        );

        try
        {
            await _messageProducer.ProduceAsync(message);
            _logger.LogInformation("Successfully sent rate change notification for {Symbol} to message broker.", symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send rate change notification for {Symbol} to message broker: {Message}", symbol, ex.Message);
        }
    }
}