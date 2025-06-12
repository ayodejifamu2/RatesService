using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RatesService.Infrastructure.Messaging;

public class AzureServiceBusConsumer<T> : IHostedService, IAsyncDisposable
    {
        private readonly ServiceBusProcessor _processor;
        private readonly ILogger<AzureServiceBusConsumer<T>> _logger;
        private Func<T, Task> _messageHandler;
        private readonly string _consumerQueueOrSubscriptionName;

        public AzureServiceBusConsumer(
            ServiceBusClient serviceBusClient,
            string consumerQueueOrSubscriptionName,
            ILogger<AzureServiceBusConsumer<T>> logger)
        {
            if (serviceBusClient == null) throw new ArgumentNullException(nameof(serviceBusClient));
            if (string.IsNullOrWhiteSpace(consumerQueueOrSubscriptionName))
                throw new ArgumentException("Consumer queue or subscription name cannot be null or empty.", nameof(consumerQueueOrSubscriptionName));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _consumerQueueOrSubscriptionName = consumerQueueOrSubscriptionName;

            // Create a processor for the queue/subscription
            // Options can be configured, e.g., MaxConcurrentCalls, MaxAutoLockRenewalDuration
            _processor = serviceBusClient.CreateProcessor(consumerQueueOrSubscriptionName, new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 1,
                AutoCompleteMessages = false
            });

            _processor.ProcessMessageAsync += MessageHandlerWrapper;
            _processor.ProcessErrorAsync += ErrorHandler;
            _logger.LogInformation("AzureServiceBusConsumer initialized for: {ConsumerQueueOrSubscriptionName}", consumerQueueOrSubscriptionName);
        }
        public void SetMessageHandler(Func<T, Task> handler)
        {
            _messageHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        private async Task MessageHandlerWrapper(ProcessMessageEventArgs args)
        {
            var body = args.Message.Body.ToString();
            _logger.LogInformation("Received message: {MessageId} from {QueueOrSubscription}. Body: {Body}",
                args.Message.MessageId, _consumerQueueOrSubscriptionName, body);

            try
            {
                var message = JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (_messageHandler != null)
                {
                    await _messageHandler.Invoke(message);
                    await args.CompleteMessageAsync(args.Message); // Mark message as successfully processed
                    _logger.LogInformation("Message {MessageId} processed and completed.", args.Message.MessageId);
                }
                else
                {
                    _logger.LogError("Message handler not set for AzureServiceBusConsumer of type {MessageType}.", typeof(T).Name);
                    await args.DeadLetterMessageAsync(args.Message, "NoMessageHandlerConfigured", "The application message handler was not set up for this consumer.");
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize message {MessageId} from {QueueOrSubscription}. Body: {Body}",
                    args.Message.MessageId, _consumerQueueOrSubscriptionName, body);
                await args.DeadLetterMessageAsync(args.Message, "DeserializationFailed", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {MessageId} from {QueueOrSubscription}. Body: {Body}",
                    args.Message.MessageId, _consumerQueueOrSubscriptionName, body);
                await args.AbandonMessageAsync(args.Message);
            }
        }

        private Task ErrorHandler(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "Error in Azure Service Bus processor: {ErrorSource}, {FullyQualifiedNamespace}, {EntityPath}",
                args.ErrorSource, args.FullyQualifiedNamespace, args.EntityPath);
            return Task.CompletedTask;
        }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Azure Service Bus processor for {ConsumerQueueOrSubscriptionName}.", _consumerQueueOrSubscriptionName);
            await _processor.StartProcessingAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Azure Service Bus processor for {ConsumerQueueOrSubscriptionName}.", _consumerQueueOrSubscriptionName);
            await _processor.StopProcessingAsync(cancellationToken);
            await DisposeAsync();
        }
        
        public async ValueTask DisposeAsync()
        {
            if (_processor != null && !_processor.IsClosed)
            {
                _logger.LogInformation("Disposing ServiceBusProcessor for {ConsumerQueueOrSubscriptionName}.", _consumerQueueOrSubscriptionName);
                await _processor.DisposeAsync();
            }
            GC.SuppressFinalize(this);
        }
    }