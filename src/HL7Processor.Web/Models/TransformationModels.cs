namespace HL7Processor.Web.Models;

public class TransformationRule
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SourceFormat { get; set; } = string.Empty;
    public string TargetFormat { get; set; } = string.Empty;
    public string RuleDefinition { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    
    // Navigation properties for display purposes
    public List<TransformationHistory> TransformationHistories { get; set; } = new();
}

public class TransformationHistory
{
    public Guid Id { get; set; }
    public Guid RuleId { get; set; }
    public TransformationRule Rule { get; set; } = new();
    public bool Success { get; set; }
    public string? OutputData { get; set; }
    public string? ErrorMessage { get; set; }
    public int TransformationTimeMs { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TransformationStats
{
    public int TotalTransformations { get; set; }
    public int SuccessfulTransformations { get; set; }
    public int FailedTransformations { get; set; }
    public double AverageTransformationTimeMs { get; set; }
    public Dictionary<string, int> TransformationsByFormat { get; set; } = new();
    public Dictionary<string, int> TransformationsByRule { get; set; } = new();
    public List<TransformationTrend> DailyTrends { get; set; } = new();
}

public class TransformationTrend
{
    public DateTime Date { get; set; }
    public int TransformationCount { get; set; }
    public double AverageTimeMs { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}