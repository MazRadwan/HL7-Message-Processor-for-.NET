using HL7Processor.Application.UseCases;
using HL7Processor.Web.Models;
using System.Diagnostics;
using System.Text.Json;

namespace HL7Processor.Web.Services;

public class ValidationService : IValidationService
{
    private readonly IGetValidationDataUseCase _getValidationDataUseCase;
    private readonly ILogger<ValidationService> _logger;
    private readonly IParserMetricsService _parserMetricsService;

    public ValidationService(IGetValidationDataUseCase getValidationDataUseCase, ILogger<ValidationService> logger, IParserMetricsService parserMetricsService)
    {
        _getValidationDataUseCase = getValidationDataUseCase;
        _logger = logger;
        _parserMetricsService = parserMetricsService;
    }

    public async Task<Models.ValidationResult> ValidateMessageAsync(string hl7Content, string validationLevel = "Strict")
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Use Application layer Use Case instead of direct Infrastructure access
            var resultDto = await _getValidationDataUseCase.ValidateMessageAsync(hl7Content, validationLevel);
            stopwatch.Stop();
            
            // Extract parsing metrics from HL7 content (keep this local logic)
            var parsingMetrics = ExtractParsingMetrics(hl7Content);
            
            // Record parser metrics
            await _parserMetricsService.RecordParsingMetricAsync(
                parsingMetrics.MessageType,
                parsingMetrics.Delimiter,
                parsingMetrics.SegmentCount,
                parsingMetrics.FieldCount,
                parsingMetrics.ComponentCount,
                resultDto.ProcessingTimeMs
            );

            // Map Application DTO to Web model
            var result = new Models.ValidationResult
            {
                Id = resultDto.Id,
                MessageId = resultDto.MessageId,
                ValidationLevel = resultDto.ValidationLevel,
                IsValid = resultDto.IsValid,
                ErrorCount = resultDto.ErrorCount,
                WarningCount = resultDto.WarningCount,
                ValidationDetails = resultDto.ValidationDetails,
                ProcessingTimeMs = resultDto.ProcessingTimeMs,
                CreatedAt = resultDto.CreatedAt
            };

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

    public async Task<Models.ValidationResult> ValidateMessageAsync(Guid messageId, string validationLevel = "Strict")
    {
        try
        {
            // Use Application layer Use Case to get validation by ID
            var resultDto = await _getValidationDataUseCase.GetValidationResultByIdAsync(messageId);
            if (resultDto == null)
                throw new ArgumentException($"Validation result for message ID {messageId} not found");

            // Map Application DTO to Web model
            return new Models.ValidationResult
            {
                Id = resultDto.Id,
                MessageId = resultDto.MessageId,
                ValidationLevel = resultDto.ValidationLevel,
                IsValid = resultDto.IsValid,
                ErrorCount = resultDto.ErrorCount,
                WarningCount = resultDto.WarningCount,
                ValidationDetails = resultDto.ValidationDetails,
                ProcessingTimeMs = resultDto.ProcessingTimeMs,
                CreatedAt = resultDto.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating message by ID {MessageId}", messageId);
            throw;
        }
    }

    public async Task<List<Models.ValidationResult>> GetValidationHistoryAsync(int limit = 50)
    {
        try
        {
            // Use Application layer Use Case instead of direct Infrastructure access
            var resultDtos = await _getValidationDataUseCase.GetRecentValidationResultsAsync(limit);
            
            // Map Application DTOs to Web models
            return resultDtos.Select(dto => new Models.ValidationResult
            {
                Id = dto.Id,
                MessageId = dto.MessageId,
                ValidationLevel = dto.ValidationLevel,
                IsValid = dto.IsValid,
                ErrorCount = dto.ErrorCount,
                WarningCount = dto.WarningCount,
                ValidationDetails = dto.ValidationDetails,
                ProcessingTimeMs = dto.ProcessingTimeMs,
                CreatedAt = dto.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting validation history");
            return new List<ValidationResult>();
        }
    }

    public async Task<Models.ValidationMetrics> GetValidationMetricsAsync(DateTime? fromDate = null)
    {
        try
        {
            // Use Application layer Use Case instead of direct Infrastructure access
            var validationDtos = await _getValidationDataUseCase.GetRecentValidationResultsAsync(1000); // Get enough for metrics
            
            // Filter by date if provided
            var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var validations = validationDtos.Where(v => v.CreatedAt >= startDate).ToList();

            var metrics = new Models.ValidationMetrics
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting validation metrics");
            return new Models.ValidationMetrics();
        }
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

    private ParsingMetrics ExtractParsingMetrics(string hl7Content)
    {
        var lines = hl7Content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        string messageType = "Unknown";
        string delimiter = "|";
        int segmentCount = lines.Length;
        int fieldCount = 0;
        int componentCount = 0;

        // Extract message type from MSH segment
        if (lines.Length > 0 && lines[0].StartsWith("MSH"))
        {
            var mshFields = lines[0].Split('|');
            if (mshFields.Length > 8)
            {
                messageType = mshFields[8]; // MSH.9 - Message Type
            }
        }

        // Count fields and components across all segments
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            var fields = line.Split('|');
            fieldCount += fields.Length;
            
            // Count components (separated by ^)
            foreach (var field in fields)
            {
                if (field.Contains('^'))
                {
                    componentCount += field.Split('^').Length;
                }
                else
                {
                    componentCount++; // Single component
                }
            }
        }

        return new ParsingMetrics
        {
            MessageType = messageType,
            Delimiter = delimiter,
            SegmentCount = segmentCount,
            FieldCount = fieldCount,
            ComponentCount = componentCount
        };
    }
}

public class ParsingMetrics
{
    public string MessageType { get; set; } = string.Empty;
    public string Delimiter { get; set; } = string.Empty;
    public int SegmentCount { get; set; }
    public int FieldCount { get; set; }
    public int ComponentCount { get; set; }
}