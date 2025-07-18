using System.ComponentModel.DataAnnotations;

namespace HL7Processor.Infrastructure.Entities;

public class TransformationHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RuleId { get; set; }

    public Guid? SourceMessageId { get; set; }

    public int TransformationTimeMs { get; set; }

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public string? OutputData { get; set; } // Transformed result

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual TransformationRule Rule { get; set; } = null!;
    public virtual HL7MessageEntity? SourceMessage { get; set; }
}