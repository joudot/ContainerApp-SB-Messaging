using Microsoft.AspNetCore.Mvc;
using ServiceBusWriterAPI.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace ServiceBusWriterAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class MessageWriterController : ControllerBase
    {
        private readonly ILogger<MessageWriterController> _logger;
        private TelemetryClient _telemetryClient;
        private readonly IMessageWriter _messageWriter;
        private readonly string AZURE_SB_QUEUE_NAME_WORKER;
        private readonly string AZURE_SB_QUEUE_NAME_JOB;


        public MessageWriterController(IMessageWriter messageWriter, ILogger<MessageWriterController> logger, TelemetryClient telemetryClient)
        {
            _logger = logger;
            _messageWriter = messageWriter;
            _telemetryClient = telemetryClient;
            AZURE_SB_QUEUE_NAME_WORKER = Environment.GetEnvironmentVariable("AZURE_SB_QUEUE_NAME_WORKER") ?? "Unknown";
            AZURE_SB_QUEUE_NAME_JOB = Environment.GetEnvironmentVariable("AZURE_SB_QUEUE_NAME_JOB") ?? "Unknown";
        }

        [HttpGet(Name = "WriteMessageWorker")]
        public async Task<ActionResult> WriteMessageWorker()
        {
            try
            {
                using (var operation = _telemetryClient.StartOperation<RequestTelemetry>("MessageWriteDuration"))
                {
                    await _messageWriter.SendMessageAsync(_logger, AZURE_SB_QUEUE_NAME_WORKER);
                    _telemetryClient.TrackEvent($"WriteMessage {AZURE_SB_QUEUE_NAME_WORKER}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.ToString());
            }
            return NoContent();
        }

        [HttpGet(Name = "WriteMessageJob")]
        public async Task<ActionResult> WriteMessageJob()
        {
            try
            {
                using (var operation = _telemetryClient.StartOperation<RequestTelemetry>("MessageWriteDuration"))
                {
                    await _messageWriter.SendMessageAsync(_logger, AZURE_SB_QUEUE_NAME_JOB);
                    _telemetryClient.TrackEvent($"WriteMessage {AZURE_SB_QUEUE_NAME_JOB}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.ToString());
            }
            return NoContent();
        }
    }
}
