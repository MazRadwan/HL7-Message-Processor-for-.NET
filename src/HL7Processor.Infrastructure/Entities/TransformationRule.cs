using System.ComponentModel.DataAnnotations;

namespace HL7Processor.Infrastructure.Entities;

public class TransformationRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(20)]
    public string SourceFormat { get; set; } = string.Empty; // 'HL7', 'JSON', 'XML', 'FHIR'

    [Required]
    [MaxLength(20)]
    public string TargetFormat { get; set; } = string.Empty;

    [Required]
    public string RuleDefinition { get; set; } = string.Empty; // JSON DSL

    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<TransformationHistory> TransformationHistories { get; set; } = new List<TransformationHistory>();
}