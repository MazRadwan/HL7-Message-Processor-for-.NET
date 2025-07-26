namespace HL7Processor.Core.Models;

public class ArchivedMessage
{
    public Guid Id { get; private set; }
    public Guid OriginalMessageId { get; private set; }
    public string MessageType { get; private set; }
    public string Version { get; private set; }
    public DateTime OriginalTimestamp { get; private set; }
    public DateTime ArchivedAt { get; private set; }

    private ArchivedMessage()
    {
        MessageType = string.Empty;
        Version = string.Empty;
    }

    public ArchivedMessage(
        Guid id,
        Guid originalMessageId,
        string messageType,
        string version,
        DateTime originalTimestamp,
        DateTime archivedAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty", nameof(id));
        if (originalMessageId == Guid.Empty)
            throw new ArgumentException("Original message ID cannot be empty", nameof(originalMessageId));
        if (string.IsNullOrWhiteSpace(messageType))
            throw new ArgumentException("Message type cannot be null or empty", nameof(messageType));
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version cannot be null or empty", nameof(version));

        Id = id;
        OriginalMessageId = originalMessageId;
        MessageType = messageType;
        Version = version;
        OriginalTimestamp = originalTimestamp;
        ArchivedAt = archivedAt;
    }

    public static ArchivedMessage Create(
        Guid originalMessageId,
        string messageType,
        string version,
        DateTime originalTimestamp)
    {
        return new ArchivedMessage(
            Guid.NewGuid(),
            originalMessageId,
            messageType,
            version,
            originalTimestamp,
            DateTime.UtcNow);
    }

    public bool IsOlderThan(TimeSpan age)
    {
        return DateTime.UtcNow - ArchivedAt > age;
    }

    public bool IsOfType(string messageType)
    {
        return MessageType.Equals(messageType, StringComparison.OrdinalIgnoreCase);
    }
}