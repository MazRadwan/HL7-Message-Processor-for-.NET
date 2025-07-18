using HL7Processor.Infrastructure.Entities;

namespace HL7Processor.Web.Services;

public interface IValidationService
{
    Task<ValidationResult> ValidateMessageAsync(string hl7Content, string validationLevel = "Strict");
    Task<ValidationResult> ValidateMessageAsync(Guid messageId, string validationLevel = "Strict");
    Task<List<ValidationResult>> GetValidationHistoryAsync(int limit = 50);
    Task<ValidationMetrics> GetValidationMetricsAsync(DateTime? fromDate = null);
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

public class ValidationIssue
{
    public string Type { get; set; } = string.Empty; // Error, Warning
    public string Severity { get; set; } = string.Empty; // High, Medium, Low
    public string Location { get; set; } = string.Empty; // Segment.Field.Component
    public string Message { get; set; } = string.Empty;
    public string Rule { get; set; } = string.Empty;
}