using Azure.Messaging.ServiceBus;

namespace ServiceBusWriterAPI.Interfaces
{
    public class ServiceBusMessageWriter : IMessageWriter
    {
        private ServiceBusClient _client;
        // Just for the sake of having different messages, there will be duplicates when scaling horizontally. In that case, use MessageId metadata to differentiate messages
        private int _messageCnt = 0;
        public ServiceBusMessageWriter()
        {
            string serviceBusConnectionString = Environment.GetEnvironmentVariable("AZURE_SB_CONNECTION_STRING") ?? "Unknown";
            string applicationInsightsConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING") ?? "Unknown";

            _client = new ServiceBusClient(serviceBusConnectionString);
        }

        public async Task SendMessageAsync(ILogger logger, string queueName)
        {
            // create a message that we can send. UTF-8 encoding is used when providing a string.
            ServiceBusMessage message = new ServiceBusMessage($"Message number {_messageCnt++}");

            // create the sender
            ServiceBusSender sender = _client.CreateSender(queueName);

            message.MessageId = Guid.NewGuid().ToString();

            // send the message
            await sender.SendMessageAsync(message);

            logger.LogInformation(" Message {MessageId} sent in queue {QueueName} - {Body} ", message.MessageId, queueName, message.Body.ToString());
        }
    }
}
