using HL7Processor.Infrastructure;
using HL7Processor.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;

namespace HL7Processor.Web.Services;

public class ValidationService : IValidationService
{
    private readonly IDbContextFactory<HL7DbContext> _contextFactory;
    private readonly ILogger<ValidationService> _logger;

    public ValidationService(IDbContextFactory<HL7DbContext> contextFactory, ILogger<ValidationService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<ValidationResult> ValidateMessageAsync(string hl7Content, string validationLevel = "Strict")
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var issues = await PerformValidation(hl7Content, validationLevel);
            stopwatch.Stop();

            var result = new ValidationResult
            {
                ValidationLevel = validationLevel,
                IsValid = !issues.Any(i => i.Type == "Error"),
                ErrorCount = issues.Count(i => i.Type == "Error"),
                WarningCount = issues.Count(i => i.Type == "Warning"),
                ValidationDetails = JsonSerializer.Serialize(issues),
                ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds
            };

            context.ValidationResults.Add(result);
            await context.SaveChangesAsync();

            _logger.LogInformation("Validated HL7 message: {IsValid}, {ErrorCount} errors, {WarningCount} warnings in {ProcessingTime}ms", 
                result.IsValid, result.ErrorCount, result.WarningCount, result.ProcessingTimeMs);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating HL7 message");
            throw;
        }
    }

    public async Task<ValidationResult> ValidateMessageAsync(Guid messageId, string validationLevel = "Strict")
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var message = await context.Messages.FindAsync(messageId);
        if (message == null)
            throw new ArgumentException($"Message with ID {messageId} not found");

        // For now, we'll generate a basic HL7 structure from the stored message data
        // In a real implementation, you'd store the original content or reconstruct it
        var reconstructedContent = $"MSH|^~\\&|SYSTEM|FACILITY|DEST|DEST|{message.Timestamp:yyyyMMddHHmmss}||{message.MessageType}|{messageId}|P|{message.Version}";
        
        var result = await ValidateMessageAsync(reconstructedContent, validationLevel);
        result.MessageId = messageId;
        
        await context.SaveChangesAsync();
        return result;
    }

    public async Task<List<ValidationResult>> GetValidationHistoryAsync(int limit = 50)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.ValidationResults
            .Include(v => v.Message)
            .OrderByDescending(v => v.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<ValidationMetrics> GetValidationMetricsAsync(DateTime? fromDate = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
        
        var validations = await context.ValidationResults
            .Where(v => v.CreatedAt >= startDate)
            .ToListAsync();

        var metrics = new ValidationMetrics
        {
            TotalValidations = validations.Count,
            ValidCount = validations.Count(v => v.IsValid),
            InvalidCount = validations.Count(v => !v.IsValid),
            AverageProcessingTimeMs = validations.Any() ? validations.Average(v => v.ProcessingTimeMs) : 0,
            ErrorsByLevel = validations
                .GroupBy(v => v.ValidationLevel)
                .ToDictionary(g => g.Key, g => g.Sum(v => v.ErrorCount))
        };

        // Extract common errors from validation details
        var allIssues = validations
            .Where(v => !string.IsNullOrEmpty(v.ValidationDetails))
            .SelectMany(v => 
            {
                try
                {
                    return JsonSerializer.Deserialize<List<ValidationIssue>>(v.ValidationDetails!) ?? new List<ValidationIssue>();
                }
                catch
                {
                    return new List<ValidationIssue>();
                }
            })
            .Where(i => i.Type == "Error")
            .ToList();

        metrics.CommonErrors = allIssues
            .GroupBy(i => i.Rule)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToDictionary(g => g.Key, g => g.Count());

        return metrics;
    }

    private async Task<List<ValidationIssue>> PerformValidation(string hl7Content, string validationLevel)
    {
        await Task.Delay(1); // Simulate async operation
        
        var issues = new List<ValidationIssue>();

        if (string.IsNullOrWhiteSpace(hl7Content))
        {
            issues.Add(new ValidationIssue
            {
                Type = "Error",
                Severity = "High",
                Location = "Message",
                Message = "Message content is empty",
                Rule = "REQUIRED_CONTENT"
            });
            return issues;
        }

        // Basic HL7 structure validation
        var lines = hl7Content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (!lines.Any())
        {
            issues.Add(new ValidationIssue
            {
                Type = "Error",
                Severity = "High", 
                Location = "Message",
                Message = "No segments found in message",
                Rule = "REQUIRED_SEGMENTS"
            });
            return issues;
        }

        // MSH segment validation
        var mshSegment = lines.FirstOrDefault();
        if (mshSegment == null || !mshSegment.StartsWith("MSH"))
        {
            issues.Add(new ValidationIssue
            {
                Type = "Error",
                Severity = "High",
                Location = "MSH",
                Message = "MSH segment must be first segment",
                Rule = "MSH_FIRST_SEGMENT"
            });
        }
        else
        {
            // Validate MSH field structure
            if (mshSegment.Length < 8)
            {
                issues.Add(new ValidationIssue
                {
                    Type = "Error",
                    Severity = "High",
                    Location = "MSH",
                    Message = "MSH segment too short",
                    Rule = "MSH_MIN_LENGTH"
                });
            }

            // Check for field separator
            if (mshSegment.Length > 3 && mshSegment[3] != '|')
            {
                issues.Add(new ValidationIssue
                {
                    Type = "Error", 
                    Severity = "Medium",
                    Location = "MSH.1",
                    Message = "Invalid field separator, expected '|'",
                    Rule = "MSH_FIELD_SEPARATOR"
                });
            }
        }

        // Segment validation based on level
        if (validationLevel == "Strict")
        {
            for (int i = 0; i < lines.Length; i++)
            {
                var segment = lines[i];
                if (segment.Length < 3)
                {
                    issues.Add(new ValidationIssue
                    {
                        Type = "Error",
                        Severity = "Medium",
                        Location = $"Segment[{i}]",
                        Message = "Segment identifier too short",
                        Rule = "SEGMENT_MIN_LENGTH"
                    });
                }

                // Check for proper field separators
                if (!segment.Contains('|') && segment != "MSH")
                {
                    issues.Add(new ValidationIssue
                    {
                        Type = "Warning",
                        Severity = "Low",
                        Location = $"Segment[{i}]",
                        Message = "No field separators found in segment",
                        Rule = "FIELD_SEPARATORS"
                    });
                }
            }
        }

        return issues;
    }
}