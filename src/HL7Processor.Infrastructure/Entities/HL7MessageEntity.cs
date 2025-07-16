using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HL7Processor.Infrastructure.Entities;

public class HL7MessageEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string MessageType { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Version { get; set; } = "2.5";

    public DateTime Timestamp { get; set; }

    public ICollection<HL7SegmentEntity> Segments { get; set; } = new List<HL7SegmentEntity>();
} 