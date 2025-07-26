namespace HL7Processor.Web.Models;

public class ValidationResult
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

public class ValidationIssue
{
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Rule { get; set; } = string.Empty;
}

public class ValidationMetrics
{
    public int TotalValidations { get; set; }
    public int ValidCount { get; set; }
    public int InvalidCount { get; set; }
    public double AverageProcessingTimeMs { get; set; }
    public Dictionary<string, int> ErrorsByLevel { get; set; } = new();
    public Dictionary<string, int> CommonErrors { get; set; } = new();
}