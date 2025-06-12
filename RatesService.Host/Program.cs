using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RatesService.Application.Services;
using RatesService.Domain.Repositories;
using RatesService.Domain.Services;
using RatesService.Infrastructure.Data;
using RatesService.Infrastructure.ExternalServices;
using RatesService.Infrastructure.Messaging;
using RatesService.Infrastructure.Messaging.Commands;
using RatesService.Infrastructure.Messaging.Contracts;
using RatesService.Infrastructure.Repositories;

namespace RatesService.Host;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        using (var scope = host.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<RatesServiceDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>(); // Get logger for Program
            var
                seederLogger =
                    scope.ServiceProvider.GetRequiredService<ILogger<Program>>(); // Get logger for Seeder

            logger.LogInformation("Database ensured created.");

            // Call the dedicated seeder
            await RatesServiceSeeder.SeedInitialData(dbContext, seederLogger);
        }

        await host.RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                var configuration = hostContext.Configuration;
                var coinMarketCapApiKey = configuration["CoinMarketCap:ApiKey"] ??
                                          throw new InvalidOperationException("CoinMarketCap API Key not configured.");
                var serviceBusConnectionString = configuration["AzureServiceBus:ConnectionString"] ??
                                                 throw new InvalidOperationException(
                                                     "Azure Service Bus Connection String not configured.");
                var serviceBusPublisherTopicOrQueueName = configuration["AzureServiceBus:TopicOrQueueName"] ??
                                                          throw new InvalidOperationException(
                                                              "Azure Service Bus Topic/Queue Name not configured.");
                var serviceBusConsumerQueueName = configuration["AzureServiceBus:ConsumerQueueName"] ?? "rates-service-trigger";

                services.AddDbContext<RatesServiceDbContext>(options =>
                {
                    //options.UseInMemoryDatabase("RatesServiceDb");
                    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
                });

                services.AddHttpClient<CoinMarketCapApiClient>(client =>
                {
                    client.BaseAddress = new Uri(configuration["CoinMarketCap:BaseUrl"] ??
                                                 throw new InvalidOperationException(
                                                     "CoinMarketCap BaseUrl not configured."));
                });

                services.AddScoped<ICryptoInstrumentRepository, CryptoInstrumentRepository>();

                services.AddScoped<IRatesVariationCheckService, RatesVariationCheckService>();

                services.AddSingleton(new ServiceBusClient(serviceBusConnectionString));

                services.AddScoped<IMessageProducer<RateChangeMessage>>(provider =>
                    new AzureServiceBusMessageProducer<RateChangeMessage>(
                        provider.GetRequiredService<ServiceBusClient>(),
                        serviceBusPublisherTopicOrQueueName,
                        provider.GetRequiredService<ILogger<AzureServiceBusMessageProducer<RateChangeMessage>>>()
                    ));

                services.AddScoped<IPositionsNotificationService, PositionsNotificationService>();

                services.AddScoped<RatesFetchingService>();

                services.AddSingleton<AzureServiceBusConsumer<FetchRatesCommand>>(provider =>
                {
                    var consumer = new AzureServiceBusConsumer<FetchRatesCommand>(
                        provider.GetRequiredService<ServiceBusClient>(),
                        serviceBusConsumerQueueName,
                        provider.GetRequiredService<ILogger<AzureServiceBusConsumer<FetchRatesCommand>>>()
                    );

                    consumer.SetMessageHandler(async (command) =>
                    {
                        using (var scope = provider.CreateScope()) // Create new scope for each message
                        {
                            var ratesFetchingService = scope.ServiceProvider.GetRequiredService<RatesFetchingService>();
                            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                            logger.LogInformation("Invoking RatesFetchingService due to FetchRatesCommand triggered at {TriggeredAt}", command.TriggeredAt);
                            await ratesFetchingService.FetchAndProcessRatesAsync();
                            logger.LogInformation("RatesFetchingService invocation completed for FetchRatesCommand.");
                        }
                    });
                    return consumer;
                });
                services.AddHostedService(provider => provider.GetRequiredService<AzureServiceBusConsumer<FetchRatesCommand>>());
            });
}