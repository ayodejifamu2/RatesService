namespace RatesService.Domain.Services;

// Generic interface for producing messages to an external system
public interface IMessageProducer<T>
{
    Task ProduceAsync(T message);
}