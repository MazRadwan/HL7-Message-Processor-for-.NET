using System.ComponentModel.DataAnnotations;

namespace HL7Processor.Infrastructure.Entities;

public class HL7ArchivedMessageEntity
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Original message identifier from the live Messages table.
    /// </summary>
    public Guid OriginalMessageId { get; set; }

    [MaxLength(50)]
    public string MessageType { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Version { get; set; } = "2.5";

    /// <summary>
    /// Timestamp when the HL7 message was originally received/processed.
    /// </summary>
    public DateTime OriginalTimestamp { get; set; }

    /// <summary>
    /// When the message was archived.
    /// </summary>
    public DateTime ArchivedAt { get; set; }
} 