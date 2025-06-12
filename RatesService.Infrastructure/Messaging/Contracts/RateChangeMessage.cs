namespace RatesService.Infrastructure.Messaging.Contracts;

public record RateChangeMessage(
    string InstrumentSymbol,
    decimal CurrentRateAmount,
    string Currency,
    DateTime Timestamp
);