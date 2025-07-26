using HL7Processor.Core.Communication.Queue;
using Microsoft.Extensions.Logging;

namespace HL7Processor.Application.UseCases;

public class SubmitMessageUseCase : ISubmitMessageUseCase
{
    private readonly IMessageQueue _queue;
    private readonly ILogger<SubmitMessageUseCase> _logger;

    public SubmitMessageUseCase(IMessageQueue queue, ILogger<SubmitMessageUseCase> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(string hl7Message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hl7Message))
        {
            _logger.LogWarning("Attempted to submit empty HL7 message");
            return false;
        }

        try
        {
            await _queue.PublishAsync("hl7_in", System.Text.Encoding.UTF8.GetBytes(hl7Message), cancellationToken);
            _logger.LogInformation("HL7 message queued successfully for processing");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue HL7 message for processing");
            return false;
        }
    }
}