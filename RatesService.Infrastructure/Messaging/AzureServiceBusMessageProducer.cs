using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using RatesService.Domain.Services;

namespace RatesService.Infrastructure.Messaging;

public class AzureServiceBusMessageProducer<T> : IMessageProducer<T>, IAsyncDisposable
    {
        private readonly ServiceBusSender _sender;
        private readonly ILogger<AzureServiceBusMessageProducer<T>> _logger;

        public AzureServiceBusMessageProducer(
            ServiceBusClient serviceBusClient,
            string topicOrQueueName,
            ILogger<AzureServiceBusMessageProducer<T>> logger)
        {
            if (serviceBusClient == null) throw new ArgumentNullException(nameof(serviceBusClient));
            if (string.IsNullOrWhiteSpace(topicOrQueueName)) throw new ArgumentException("Topic or queue name cannot be null or empty.", nameof(topicOrQueueName));

            _sender = serviceBusClient.CreateSender(topicOrQueueName);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("AzureServiceBusMessageProducer initialized for topic/queue: {TopicOrQueueName}", topicOrQueueName);
        }

        public async Task ProduceAsync(T message)
        {
            try
            {
                var jsonMessage = JsonSerializer.Serialize(message);
                var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(jsonMessage))
                {
                    ContentType = "application/json",
                    MessageId = Guid.NewGuid().ToString()
                };

                await _sender.SendMessageAsync(serviceBusMessage);
                _logger.LogInformation("Successfully sent message to Azure Service Bus topic/queue. MessageId: {MessageId}", serviceBusMessage.MessageId);
                _logger.LogDebug("Sent message content: {JsonMessage}", jsonMessage);
            }
            catch (ServiceBusException ex) when (ex.IsTransient)
            {
                 _logger.LogWarning(ex, "Transient error sending message to Azure Service Bus. Retrying will be handled by hosting framework or policies. Message: {Message}", ex.Message);
                 throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to Azure Service Bus: {Message}", ex.Message);
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_sender != null && !_sender.IsClosed)
            {
                _logger.LogInformation("Closing ServiceBusSender.");
                await _sender.CloseAsync();
            }
            GC.SuppressFinalize(this);
        }
    }