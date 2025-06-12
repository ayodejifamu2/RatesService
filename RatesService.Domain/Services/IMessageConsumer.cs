namespace RatesService.Domain.Services;

public interface IMessageConsumer<T>
{
    Task StartConsumingAsync(Func<T, Task> messageHandler, CancellationToken cancellationToken);
    Task StopConsumingAsync(CancellationToken cancellationToken);
}