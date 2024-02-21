namespace ServiceBusWriterAPI.Interfaces
{
    public interface IMessageWriter
    {
        public Task SendMessageAsync(ILogger logger, string queueName);
    }
}
