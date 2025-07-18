using System.ComponentModel.DataAnnotations;

namespace HL7Processor.Infrastructure.Entities;

public class ParserMetric
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(50)]
    public string? MessageType { get; set; }

    [MaxLength(10)]
    public string? DelimiterDetected { get; set; }

    public int? SegmentCount { get; set; }

    public int? FieldCount { get; set; }

    public int? ComponentCount { get; set; }

    public int ParseTimeMs { get; set; }

    public long? MemoryUsedBytes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}