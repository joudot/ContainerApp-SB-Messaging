using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace ServiceBusQueue
{
    public class ServiceBusQueueReader
    {
        private ServiceBusReceiver _receiver;
        private ILogger<ServiceBusQueueReader> _logger;

        public ServiceBusQueueReader(ServiceBusReceiver receiver, ILogger<ServiceBusQueueReader> logger)
        {
            _receiver = receiver;
            _logger = logger;
        }

        public async Task ReceiveMessageAsync()
        {
            // the received message is a different type as it contains some service set properties
            ServiceBusReceivedMessage receivedMessage = await _receiver.ReceiveMessageAsync();

            if (receivedMessage != null)
            {
                var processedTime = DateTimeOffset.UtcNow;
                var enqueuedTime = receivedMessage?.EnqueuedTime.ToUniversalTime() ?? DateTimeOffset.MinValue;
                var dequeueTime = processedTime.Subtract(enqueuedTime);
                var messageId = receivedMessage?.MessageId ?? "Unknown Message Id";

                // get the message body as a string
                string body = receivedMessage?.Body?.ToString() ?? "Body Empty";
                //
                _logger.LogInformation("Receiving Message: {MessageId} - {Body}", messageId, body);

                // Freeing the lock so it does not end up in the dead letter queue
                await _receiver.CompleteMessageAsync(receivedMessage);

                _logger.LogInformation("Time to process message {MessageId} once queued: {seconds} sec", messageId, dequeueTime.TotalSeconds);

            }
            else
            {
                _logger.LogInformation("No message found {entity} - {prefetch count}", _receiver.EntityPath, _receiver.PrefetchCount);
            }
        }
    }
}
