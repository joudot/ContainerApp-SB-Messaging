using Azure.Messaging.ServiceBus;
using ServiceBusQueue;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace DemoSbJob
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            string serviceBusConnectionString = Environment.GetEnvironmentVariable("AZURE_SB_CONNECTION_STRING") ?? "Unknown";
            string applicationInsightsInstrumentationKey = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_INSTRUMENTATIONKEY") ?? "Unknown";
            string queueName = Environment.GetEnvironmentVariable("AZURE_SB_QUEUE_NAME_JOB") ?? "Unknown";
            ILogger<Program> logger = null;
            TelemetryClient telemetryClient = null;
            // Create the DI container.
            IServiceCollection services = new ServiceCollection();
            
            try
            {
                // Being a regular console app, there is no appsettings.json or configuration providers enabled by default.
                // Hence instrumentation key/ connection string and any changes to default logging level must be specified here.
                services.AddLogging(loggingBuilder => loggingBuilder.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>("Category", LogLevel.Information));
                services.AddApplicationInsightsTelemetryWorkerService((ApplicationInsightsServiceOptions options) => options.ConnectionString = $"InstrumentationKey={applicationInsightsInstrumentationKey}");

                // Build ServiceProvider.
                IServiceProvider serviceProvider = services.BuildServiceProvider();

                // Obtain logger instance from DI.
                logger = serviceProvider.GetRequiredService<ILogger<Program>>();

                // Obtain TelemetryClient instance from DI, for additional manual tracking or to flush.
                telemetryClient = serviceProvider.GetRequiredService<TelemetryClient>();
           
                // since ServiceBusClient implements IAsyncDisposable we create it with "await using"
                await using var client = new ServiceBusClient(serviceBusConnectionString);

                // create a receiver for our subscription that we can use to receive the message
                ServiceBusReceiver receiver = client.CreateReceiver(queueName);

                logger.LogInformation("Starting Worker at: {time} UTC", DateTimeOffset.UtcNow);


                var reader = new ServiceBusQueueReader(receiver, serviceProvider.GetRequiredService< ILogger<ServiceBusQueueReader>>());
                using (telemetryClient.StartOperation<RequestTelemetry>("Message Processing"))
                {
                    await reader.ReceiveMessageAsync();
                }

                telemetryClient.TrackEvent("Message Processed");

                logger.LogInformation("Stopping Worker at: {time} UTC", DateTimeOffset.UtcNow);
            }
            catch (Exception e)
            {
                logger.LogError(0, e, "Fail to process message");
            }
        }
    }
}
