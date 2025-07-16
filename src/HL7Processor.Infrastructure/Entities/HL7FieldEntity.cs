using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HL7Processor.Infrastructure.Entities;

public class HL7FieldEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int Position { get; set; }

    [MaxLength(2000)]
    public string? Value { get; set; }

    [ForeignKey(nameof(Segment))]
    public int SegmentId { get; set; }
    public HL7SegmentEntity Segment { get; set; } = null!;
} 