namespace HL7Processor.Core.Communication.Queue;

public interface IMessageQueue
{
    Task PublishAsync(string queueName, byte[] payload, CancellationToken cancellationToken = default);
    Task<byte[]?> ReceiveAsync(string queueName, CancellationToken cancellationToken = default);
    Task AcknowledgeAsync(string queueName, string messageId, CancellationToken cancellationToken = default);
    Task PublishToDeadLetterAsync(string queueName, byte[] payload, string reason, CancellationToken cancellationToken = default);
    Task<byte[]?> ReceiveFromDeadLetterAsync(string queueName, CancellationToken cancellationToken = default);
} 