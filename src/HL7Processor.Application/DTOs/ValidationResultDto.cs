namespace HL7Processor.Application.DTOs;

public class ValidationResultDto
{
    public Guid Id { get; set; }
    public Guid? MessageId { get; set; }
    public string ValidationLevel { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public string? ValidationDetails { get; set; }
    public int ProcessingTimeMs { get; set; }
    public DateTime CreatedAt { get; set; }
}