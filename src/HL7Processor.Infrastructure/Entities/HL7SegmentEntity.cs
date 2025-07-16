using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HL7Processor.Infrastructure.Entities;

public class HL7SegmentEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(3)]
    public string Type { get; set; } = string.Empty;

    [Required]
    public int SequenceNumber { get; set; }

    [ForeignKey(nameof(Message))]
    public Guid MessageId { get; set; }
    public HL7MessageEntity Message { get; set; } = null!;

    public ICollection<HL7FieldEntity> Fields { get; set; } = new List<HL7FieldEntity>();
} 