namespace RatesService.Infrastructure.Messaging.Commands;

public record FetchRatesCommand
{
    public DateTime TriggeredAt { get; init; } = DateTime.UtcNow;
}