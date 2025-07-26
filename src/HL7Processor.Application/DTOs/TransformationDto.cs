namespace HL7Processor.Application.DTOs;

public class TransformationRuleDto
{
    public Guid Id { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string TransformationType { get; set; } = string.Empty;
    public string? TransformationExpression { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TransformationHistoryDto
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string SourceFormat { get; set; } = string.Empty;
    public string TargetFormat { get; set; } = string.Empty;
    public string? SourceData { get; set; }
    public string? TransformedData { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public int ProcessingTimeMs { get; set; }
    public DateTime CreatedAt { get; set; }
}