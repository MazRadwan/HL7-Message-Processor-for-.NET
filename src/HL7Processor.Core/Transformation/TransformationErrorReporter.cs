using HL7Processor.Core.Models;
using HL7Processor.Core.Exceptions;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;

namespace HL7Processor.Core.Transformation;

public class TransformationErrorReporter
{
    private readonly ILogger<TransformationErrorReporter> _logger;
    private readonly List<TransformationError> _errors = new();

    public TransformationErrorReporter(ILogger<TransformationErrorReporter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void ReportError(TransformationError error)
    {
        if (error == null) throw new ArgumentNullException(nameof(error));

        error.Id = Guid.NewGuid().ToString();
        error.Timestamp = DateTime.UtcNow;
        
        _errors.Add(error);
        
        LogError(error);
    }

    public void ReportFieldMappingError(string sourceField, string targetField, string value, string errorMessage, 
        string? messageId = null, Exception? exception = null)
    {
        var error = new TransformationError
        {
            ErrorType = TransformationErrorType.FieldMapping,
            SourceField = sourceField,
            TargetField = targetField,
            Value = value,
            ErrorMessage = errorMessage,
            MessageId = messageId,
            Exception = exception
        };

        ReportError(error);
    }

    public void ReportValidationError(string fieldName, string value, string validationRule, string errorMessage,
        string? messageId = null)
    {
        var error = new TransformationError
        {
            ErrorType = TransformationErrorType.Validation,
            SourceField = fieldName,
            Value = value,
            ErrorMessage = errorMessage,
            MessageId = messageId,
            ValidationRule = validationRule
        };

        ReportError(error);
    }

    public void ReportDataTypeError(string fieldName, string value, string expectedType, string actualType,
        string? messageId = null)
    {
        var error = new TransformationError
        {
            ErrorType = TransformationErrorType.DataType,
            SourceField = fieldName,
            Value = value,
            ErrorMessage = $"Expected {expectedType}, got {actualType}",
            MessageId = messageId,
            ExpectedDataType = expectedType,
            ActualDataType = actualType
        };

        ReportError(error);
    }

    public void ReportFormatError(string fieldName, string value, string expectedFormat, string? messageId = null)
    {
        var error = new TransformationError
        {
            ErrorType = TransformationErrorType.Format,
            SourceField = fieldName,
            Value = value,
            ErrorMessage = $"Value does not match expected format: {expectedFormat}",
            MessageId = messageId,
            ExpectedFormat = expectedFormat
        };

        ReportError(error);
    }

    public void ReportBusinessRuleError(string ruleName, string ruleDescription, string fieldName, string value,
        string? messageId = null)
    {
        var error = new TransformationError
        {
            ErrorType = TransformationErrorType.BusinessRule,
            SourceField = fieldName,
            Value = value,
            ErrorMessage = $"Business rule violation: {ruleDescription}",
            MessageId = messageId,
            BusinessRule = ruleName
        };

        ReportError(error);
    }

    public void ReportSystemError(string operation, string errorMessage, Exception? exception = null,
        string? messageId = null)
    {
        var error = new TransformationError
        {
            ErrorType = TransformationErrorType.System,
            ErrorMessage = errorMessage,
            MessageId = messageId,
            Exception = exception,
            Operation = operation
        };

        ReportError(error);
    }

    public void ReportConfigurationError(string configurationItem, string errorMessage, string? messageId = null)
    {
        var error = new TransformationError
        {
            ErrorType = TransformationErrorType.Configuration,
            ErrorMessage = errorMessage,
            MessageId = messageId,
            ConfigurationItem = configurationItem
        };

        ReportError(error);
    }

    public void ReportPerformanceWarning(string operation, TimeSpan duration, string? messageId = null)
    {
        var error = new TransformationError
        {
            ErrorType = TransformationErrorType.Performance,
            Severity = TransformationErrorSeverity.Warning,
            ErrorMessage = $"Operation {operation} took {duration.TotalMilliseconds}ms",
            MessageId = messageId,
            Operation = operation,
            Duration = duration
        };

        ReportError(error);
    }

    public List<TransformationError> GetErrors(string? messageId = null, TransformationErrorType? errorType = null,
        TransformationErrorSeverity? severity = null)
    {
        var filteredErrors = _errors.AsEnumerable();

        if (!string.IsNullOrEmpty(messageId))
        {
            filteredErrors = filteredErrors.Where(e => e.MessageId == messageId);
        }

        if (errorType.HasValue)
        {
            filteredErrors = filteredErrors.Where(e => e.ErrorType == errorType.Value);
        }

        if (severity.HasValue)
        {
            filteredErrors = filteredErrors.Where(e => e.Severity == severity.Value);
        }

        return filteredErrors.OrderByDescending(e => e.Timestamp).ToList();
    }

    public List<TransformationError> GetErrorsByTimeRange(DateTime startTime, DateTime endTime)
    {
        return _errors.Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime)
                     .OrderByDescending(e => e.Timestamp)
                     .ToList();
    }

    public TransformationErrorSummary GetErrorSummary(string? messageId = null)
    {
        var errors = GetErrors(messageId);
        
        var summary = new TransformationErrorSummary
        {
            MessageId = messageId,
            GeneratedAt = DateTime.UtcNow,
            TotalErrors = errors.Count,
            ErrorsBySeverity = errors.GroupBy(e => e.Severity).ToDictionary(g => g.Key, g => g.Count()),
            ErrorsByType = errors.GroupBy(e => e.ErrorType).ToDictionary(g => g.Key, g => g.Count()),
            MostFrequentErrors = errors.GroupBy(e => e.ErrorMessage)
                                     .OrderByDescending(g => g.Count())
                                     .Take(10)
                                     .ToDictionary(g => g.Key, g => g.Count()),
            ErrorsByField = errors.Where(e => !string.IsNullOrEmpty(e.SourceField))
                                 .GroupBy(e => e.SourceField!)
                                 .ToDictionary(g => g.Key, g => g.Count())
        };

        return summary;
    }

    public string GenerateErrorReport(string? messageId = null, ReportFormat format = ReportFormat.Text)
    {
        var errors = GetErrors(messageId);
        var summary = GetErrorSummary(messageId);

        return format switch
        {
            ReportFormat.Text => GenerateTextReport(errors, summary),
            ReportFormat.Html => GenerateHtmlReport(errors, summary),
            ReportFormat.Json => GenerateJsonReport(errors, summary),
            ReportFormat.Csv => GenerateCsvReport(errors),
            _ => GenerateTextReport(errors, summary)
        };
    }

    public void ClearErrors(string? messageId = null)
    {
        if (string.IsNullOrEmpty(messageId))
        {
            _errors.Clear();
            _logger.LogInformation("All transformation errors cleared");
        }
        else
        {
            var removedCount = _errors.RemoveAll(e => e.MessageId == messageId);
            _logger.LogInformation("Cleared {Count} transformation errors for message {MessageId}", removedCount, messageId);
        }
    }

    public void ExportErrors(string filePath, string? messageId = null, ReportFormat format = ReportFormat.Json)
    {
        var report = GenerateErrorReport(messageId, format);
        File.WriteAllText(filePath, report);
        _logger.LogInformation("Exported transformation errors to {FilePath}", filePath);
    }

    public bool HasErrors(string? messageId = null, TransformationErrorSeverity? minSeverity = null)
    {
        var errors = GetErrors(messageId);
        
        if (minSeverity.HasValue)
        {
            errors = errors.Where(e => e.Severity >= minSeverity.Value).ToList();
        }

        return errors.Any();
    }

    public bool HasCriticalErrors(string? messageId = null)
    {
        return HasErrors(messageId, TransformationErrorSeverity.Critical);
    }

    public int GetErrorCount(string? messageId = null, TransformationErrorSeverity? severity = null)
    {
        return GetErrors(messageId, severity: severity).Count;
    }

    public List<string> GetAffectedFields(string? messageId = null)
    {
        return GetErrors(messageId)
            .Where(e => !string.IsNullOrEmpty(e.SourceField))
            .Select(e => e.SourceField!)
            .Distinct()
            .OrderBy(f => f)
            .ToList();
    }

    public Dictionary<string, int> GetErrorTrends(int days = 7)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        var errors = GetErrorsByTimeRange(startDate, DateTime.UtcNow);
        
        return errors.GroupBy(e => e.Timestamp.Date)
                    .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count());
    }

    private void LogError(TransformationError error)
    {
        var logLevel = error.Severity switch
        {
            TransformationErrorSeverity.Critical => LogLevel.Critical,
            TransformationErrorSeverity.Error => LogLevel.Error,
            TransformationErrorSeverity.Warning => LogLevel.Warning,
            TransformationErrorSeverity.Information => LogLevel.Information,
            _ => LogLevel.Error
        };

        _logger.Log(logLevel, error.Exception, 
            "Transformation {ErrorType} error in {Operation}: {ErrorMessage} (Field: {SourceField}, Value: {Value})",
            error.ErrorType, error.Operation, error.ErrorMessage, error.SourceField, error.Value);
    }

    private string GenerateTextReport(List<TransformationError> errors, TransformationErrorSummary summary)
    {
        var report = new StringBuilder();
        
        report.AppendLine("TRANSFORMATION ERROR REPORT");
        report.AppendLine("=" + new string('=', 50));
        report.AppendLine($"Generated: {summary.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"Message ID: {summary.MessageId ?? "All Messages"}");
        report.AppendLine($"Total Errors: {summary.TotalErrors}");
        report.AppendLine();

        // Summary by severity
        report.AppendLine("ERRORS BY SEVERITY:");
        foreach (var kvp in summary.ErrorsBySeverity)
        {
            report.AppendLine($"  {kvp.Key}: {kvp.Value}");
        }
        report.AppendLine();

        // Summary by type
        report.AppendLine("ERRORS BY TYPE:");
        foreach (var kvp in summary.ErrorsByType)
        {
            report.AppendLine($"  {kvp.Key}: {kvp.Value}");
        }
        report.AppendLine();

        // Most frequent errors
        report.AppendLine("MOST FREQUENT ERRORS:");
        foreach (var kvp in summary.MostFrequentErrors)
        {
            report.AppendLine($"  {kvp.Value}x: {kvp.Key}");
        }
        report.AppendLine();

        // Detailed errors
        report.AppendLine("DETAILED ERRORS:");
        foreach (var error in errors.Take(50)) // Limit to 50 most recent
        {
            report.AppendLine($"[{error.Timestamp:yyyy-MM-dd HH:mm:ss}] {error.Severity} - {error.ErrorType}");
            report.AppendLine($"  Message: {error.ErrorMessage}");
            if (!string.IsNullOrEmpty(error.SourceField))
                report.AppendLine($"  Field: {error.SourceField}");
            if (!string.IsNullOrEmpty(error.Value))
                report.AppendLine($"  Value: {error.Value}");
            if (!string.IsNullOrEmpty(error.Operation))
                report.AppendLine($"  Operation: {error.Operation}");
            report.AppendLine();
        }

        return report.ToString();
    }

    private string GenerateHtmlReport(List<TransformationError> errors, TransformationErrorSummary summary)
    {
        var html = new StringBuilder();
        
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head><title>Transformation Error Report</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        html.AppendLine("table { border-collapse: collapse; width: 100%; }");
        html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        html.AppendLine("th { background-color: #f2f2f2; }");
        html.AppendLine(".critical { color: #d32f2f; }");
        html.AppendLine(".error { color: #f57c00; }");
        html.AppendLine(".warning { color: #fbc02d; }");
        html.AppendLine(".info { color: #1976d2; }");
        html.AppendLine("</style></head><body>");
        
        html.AppendLine("<h1>Transformation Error Report</h1>");
        html.AppendLine($"<p><strong>Generated:</strong> {summary.GeneratedAt:yyyy-MM-dd HH:mm:ss}</p>");
        html.AppendLine($"<p><strong>Message ID:</strong> {summary.MessageId ?? "All Messages"}</p>");
        html.AppendLine($"<p><strong>Total Errors:</strong> {summary.TotalErrors}</p>");
        
        html.AppendLine("<h2>Error Summary</h2>");
        html.AppendLine("<table><tr><th>Type</th><th>Count</th></tr>");
        foreach (var kvp in summary.ErrorsByType)
        {
            html.AppendLine($"<tr><td>{kvp.Key}</td><td>{kvp.Value}</td></tr>");
        }
        html.AppendLine("</table>");
        
        html.AppendLine("<h2>Recent Errors</h2>");
        html.AppendLine("<table><tr><th>Timestamp</th><th>Severity</th><th>Type</th><th>Message</th><th>Field</th></tr>");
        
        foreach (var error in errors.Take(100))
        {
            var severityClass = error.Severity.ToString().ToLower();
            html.AppendLine($"<tr>");
            html.AppendLine($"<td>{error.Timestamp:yyyy-MM-dd HH:mm:ss}</td>");
            html.AppendLine($"<td class=\"{severityClass}\">{error.Severity}</td>");
            html.AppendLine($"<td>{error.ErrorType}</td>");
            html.AppendLine($"<td>{error.ErrorMessage}</td>");
            html.AppendLine($"<td>{error.SourceField ?? ""}</td>");
            html.AppendLine($"</tr>");
        }
        
        html.AppendLine("</table></body></html>");
        
        return html.ToString();
    }

    private string GenerateJsonReport(List<TransformationError> errors, TransformationErrorSummary summary)
    {
        var report = new
        {
            Summary = summary,
            Errors = errors.Take(1000).ToList() // Limit to 1000 most recent
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(report, options);
    }

    private string GenerateCsvReport(List<TransformationError> errors)
    {
        var csv = new StringBuilder();
        
        // Header
        csv.AppendLine("Timestamp,Severity,Type,Message,SourceField,TargetField,Value,Operation,MessageId");
        
        // Data rows
        foreach (var error in errors)
        {
            csv.AppendLine($"{error.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                          $"{error.Severity}," +
                          $"{error.ErrorType}," +
                          $"\"{error.ErrorMessage?.Replace("\"", "\"\"")}\"," +
                          $"\"{error.SourceField ?? ""}\"," +
                          $"\"{error.TargetField ?? ""}\"," +
                          $"\"{error.Value ?? ""}\"," +
                          $"\"{error.Operation ?? ""}\"," +
                          $"\"{error.MessageId ?? ""}\"");
        }

        return csv.ToString();
    }
}

public class TransformationError
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TransformationErrorType ErrorType { get; set; }
    public TransformationErrorSeverity Severity { get; set; } = TransformationErrorSeverity.Error;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? MessageId { get; set; }
    public string? SourceField { get; set; }
    public string? TargetField { get; set; }
    public string? Value { get; set; }
    public string? Operation { get; set; }
    public Exception? Exception { get; set; }
    public string? ValidationRule { get; set; }
    public string? BusinessRule { get; set; }
    public string? ConfigurationItem { get; set; }
    public string? ExpectedDataType { get; set; }
    public string? ActualDataType { get; set; }
    public string? ExpectedFormat { get; set; }
    public TimeSpan? Duration { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    public override string ToString()
    {
        return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Severity} - {ErrorType}: {ErrorMessage}";
    }
}

public class TransformationErrorSummary
{
    public string? MessageId { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public int TotalErrors { get; set; }
    public Dictionary<TransformationErrorSeverity, int> ErrorsBySeverity { get; set; } = new();
    public Dictionary<TransformationErrorType, int> ErrorsByType { get; set; } = new();
    public Dictionary<string, int> MostFrequentErrors { get; set; } = new();
    public Dictionary<string, int> ErrorsByField { get; set; } = new();
}

public enum TransformationErrorType
{
    FieldMapping,
    DataType,
    Validation,
    Format,
    BusinessRule,
    System,
    Configuration,
    Performance
}

public enum TransformationErrorSeverity
{
    Information = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}

public enum ReportFormat
{
    Text,
    Html,
    Json,
    Csv
}