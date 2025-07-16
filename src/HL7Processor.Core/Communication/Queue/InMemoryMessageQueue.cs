using System.Collections.Concurrent;

namespace HL7Processor.Core.Communication.Queue;

public class InMemoryMessageQueue : IMessageQueue
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<(string Id, byte[] Payload)>> _queues = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<(string Id, byte[] Payload, string? Reason)>> _deadLetterQueues = new();

    public Task PublishAsync(string queueName, byte[] payload, CancellationToken cancellationToken = default)
    {
        var queue = _queues.GetOrAdd(queueName, _ => new());
        var id = Guid.NewGuid().ToString();
        queue.Enqueue((id, payload));
        return Task.CompletedTask;
    }

    public Task<byte[]?> ReceiveAsync(string queueName, CancellationToken cancellationToken = default)
    {
        if (_queues.TryGetValue(queueName, out var queue) && queue.TryDequeue(out var item))
        {
            return Task.FromResult<byte[]?>(item.Payload);
        }
        return Task.FromResult<byte[]?>(null);
    }

    public Task AcknowledgeAsync(string queueName, string messageId, CancellationToken cancellationToken = default)
    {
        // No-op for in-memory queue
        return Task.CompletedTask;
    }

    public Task PublishToDeadLetterAsync(string queueName, byte[] payload, string reason, CancellationToken cancellationToken = default)
    {
        var dlq = _deadLetterQueues.GetOrAdd(queueName, _ => new());
        dlq.Enqueue((Guid.NewGuid().ToString(), payload, reason));
        return Task.CompletedTask;
    }

    public Task<byte[]?> ReceiveFromDeadLetterAsync(string queueName, CancellationToken cancellationToken = default)
    {
        if (_deadLetterQueues.TryGetValue(queueName, out var dlq) && dlq.TryDequeue(out var item))
        {
            return Task.FromResult<byte[]?>(item.Payload);
        }
        return Task.FromResult<byte[]?>(null);
    }
} 