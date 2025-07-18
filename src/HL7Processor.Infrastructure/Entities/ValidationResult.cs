using System.ComponentModel.DataAnnotations;

namespace HL7Processor.Infrastructure.Entities;

public class ValidationResult
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? MessageId { get; set; }
    public virtual HL7MessageEntity? Message { get; set; }

    [Required]
    [MaxLength(20)]
    public string ValidationLevel { get; set; } = string.Empty; // 'Strict', 'Lenient', 'Custom'

    public bool IsValid { get; set; }

    public int ErrorCount { get; set; } = 0;

    public int WarningCount { get; set; } = 0;

    public string? ValidationDetails { get; set; } // JSON array of issues

    public int ProcessingTimeMs { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}