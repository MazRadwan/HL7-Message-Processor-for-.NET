namespace HL7Processor.Application.DTOs;

public class ArchivedMessageDto
{
    public Guid Id { get; set; }
    public Guid OriginalMessageId { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime OriginalTimestamp { get; set; }
    public DateTime ArchivedAt { get; set; }
}