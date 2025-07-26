using HL7Processor.Core.Communication.Queue;
using Microsoft.Extensions.Logging;

namespace HL7Processor.Application.UseCases;

public class RequeueMessageUseCase : IRequeueMessageUseCase
{
    private readonly IMessageQueue _queue;
    private readonly ILogger<RequeueMessageUseCase> _logger;

    public RequeueMessageUseCase(IMessageQueue queue, ILogger<RequeueMessageUseCase> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(string queueName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(queueName))
        {
            _logger.LogWarning("Attempted to requeue from empty queue name");
            return false;
        }

        try
        {
            var payload = await _queue.ReceiveFromDeadLetterAsync(queueName, cancellationToken);
            if (payload is null)
            {
                _logger.LogInformation("No messages found in dead letter queue: {QueueName}", queueName);
                return false;
            }

            await _queue.PublishAsync(queueName, payload, cancellationToken);
            _logger.LogInformation("Message successfully requeued from dead letter to: {QueueName}", queueName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to requeue message from dead letter queue: {QueueName}", queueName);
            return false;
        }
    }
}