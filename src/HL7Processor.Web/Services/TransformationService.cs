using HL7Processor.Application.UseCases;
using HL7Processor.Web.Models;
using System.Diagnostics;
using System.Text.Json;
using HL7Processor.Application.DTOs;
using System.Linq;

namespace HL7Processor.Web.Services;

public class TransformationService : ITransformationService
{
    private readonly IGetTransformationDataUseCase _getTransformationDataUseCase;
    private readonly ICreateTransformationRuleUseCase _createRuleUseCase;
    private readonly IUpdateTransformationRuleUseCase _updateRuleUseCase;
    private readonly IDeleteTransformationRuleUseCase _deleteRuleUseCase;
    private readonly IGetTransformationStatsUseCase _getStatsUseCase;
    private readonly ILogger<TransformationService> _logger;

    public TransformationService(IGetTransformationDataUseCase getTransformationDataUseCase,
        ICreateTransformationRuleUseCase createRuleUseCase,
        IUpdateTransformationRuleUseCase updateRuleUseCase,
        IDeleteTransformationRuleUseCase deleteRuleUseCase,
        IGetTransformationStatsUseCase getStatsUseCase,
        ILogger<TransformationService> logger)
    {
        _getTransformationDataUseCase = getTransformationDataUseCase;
        _createRuleUseCase = createRuleUseCase;
        _updateRuleUseCase = updateRuleUseCase;
        _deleteRuleUseCase = deleteRuleUseCase;
        _getStatsUseCase = getStatsUseCase;
        _logger = logger;
    }

    public async Task<List<Models.TransformationRule>> GetTransformationRulesAsync()
    {
        try
        {
            // Use Application layer Use Case instead of direct Infrastructure access
            var ruleDtos = await _getTransformationDataUseCase.GetTransformationRulesAsync();
            
            // Map Application DTOs to Web models
            return ruleDtos.Select(dto => new Models.TransformationRule
            {
                Id = dto.Id,
                Name = dto.RuleName,
                SourceFormat = ExtractSourceFormat(dto.TransformationType),
                TargetFormat = ExtractTargetFormat(dto.TransformationType),
                RuleDefinition = dto.TransformationExpression ?? string.Empty,
                IsActive = dto.IsActive,
                CreatedAt = dto.CreatedAt,
                ModifiedAt = dto.UpdatedAt ?? dto.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transformation rules");
            return new List<Models.TransformationRule>();
        }
    }

    public async Task<Models.TransformationRule?> GetTransformationRuleAsync(Guid id)
    {
        try
        {
            // Use Application layer Use Case instead of direct Infrastructure access
            var ruleDto = await _getTransformationDataUseCase.GetTransformationRuleByIdAsync(id);
            if (ruleDto == null) return null;
            
            // Map Application DTO to Web model
            return new Models.TransformationRule
            {
                Id = ruleDto.Id,
                Name = ruleDto.RuleName,
                SourceFormat = ExtractSourceFormat(ruleDto.TransformationType),
                TargetFormat = ExtractTargetFormat(ruleDto.TransformationType),
                RuleDefinition = ruleDto.TransformationExpression ?? string.Empty,
                IsActive = ruleDto.IsActive,
                CreatedAt = ruleDto.CreatedAt,
                ModifiedAt = ruleDto.UpdatedAt ?? ruleDto.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transformation rule {RuleId}", id);
            return null;
        }
    }

    public async Task<Models.TransformationRule> CreateTransformationRuleAsync(Models.TransformationRule rule)
    {
        var dto = new TransformationRuleDto
        {
            RuleName = rule.Name,
            SourcePath = rule.SourceFormat,
            TargetPath = rule.TargetFormat,
            TransformationType = rule.SourceFormat + "->" + rule.TargetFormat,
            TransformationExpression = rule.RuleDefinition,
            IsActive = rule.IsActive
        };
        var created = await _createRuleUseCase.ExecuteAsync(dto);
        rule.Id = created.Id;
        rule.CreatedAt = created.CreatedAt;
        rule.ModifiedAt = created.UpdatedAt ?? created.CreatedAt;
        return rule;
    }

    public async Task<Models.TransformationRule> UpdateTransformationRuleAsync(Models.TransformationRule rule)
    {
        var dto = new TransformationRuleDto
        {
            Id = rule.Id,
            RuleName = rule.Name,
            SourcePath = rule.SourceFormat,
            TargetPath = rule.TargetFormat,
            TransformationType = rule.SourceFormat + "->" + rule.TargetFormat,
            TransformationExpression = rule.RuleDefinition,
            IsActive = rule.IsActive
        };
        var updated = await _updateRuleUseCase.ExecuteAsync(dto);
        rule.ModifiedAt = updated.UpdatedAt ?? DateTime.UtcNow;
        return rule;
    }

    public async Task<bool> DeleteTransformationRuleAsync(Guid id)
    {
        return await _deleteRuleUseCase.ExecuteAsync(id);
    }

    public async Task<string> ExecuteTransformationAsync(Guid ruleId, string inputData)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Use Application layer Use Case to get transformation rule
        var ruleDto = await _getTransformationDataUseCase.GetTransformationRuleByIdAsync(ruleId);
        if (ruleDto == null)
            throw new ArgumentException($"Transformation rule with ID {ruleId} not found");

        // Map to Web model for transformation logic
        var rule = new Models.TransformationRule
        {
            Id = ruleDto.Id,
            Name = ruleDto.RuleName,
            SourceFormat = ExtractSourceFormat(ruleDto.TransformationType),
            TargetFormat = ExtractTargetFormat(ruleDto.TransformationType),
            RuleDefinition = ruleDto.TransformationExpression ?? string.Empty
        };

        try
        {
            var result = await ExecuteTransformationLogic(rule, inputData);
            stopwatch.Stop();
            
            // TODO: Record transformation history via Use Case
            _logger.LogInformation("Executed transformation {RuleName} in {ElapsedMs}ms", 
                rule.Name, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // TODO: Record transformation failure via Use Case
            _logger.LogError(ex, "Transformation {RuleName} failed", rule.Name);
            throw;
        }
    }

    public async Task<string> PreviewTransformationAsync(string ruleDefinition, string inputData)
    {
        var tempRule = new TransformationRule
        {
            RuleDefinition = ruleDefinition,
            SourceFormat = "HL7",
            TargetFormat = "JSON"
        };
        
        return await ExecuteTransformationLogic(tempRule, inputData);
    }

    public async Task<List<Models.TransformationHistory>> GetTransformationHistoryAsync(int limit = 100)
    {
        var historyDtos = await _getTransformationDataUseCase.GetTransformationHistoryAsync(limit);
        return historyDtos.Select(dto => new Models.TransformationHistory
        {
            Id = dto.Id,
            RuleId = dto.RuleId,
            Success = dto.IsSuccessful,
            OutputData = dto.TransformedData,
            ErrorMessage = dto.ErrorMessage,
            TransformationTimeMs = dto.ProcessingTimeMs,
            CreatedAt = dto.CreatedAt,
            Rule = new Models.TransformationRule { Id = dto.RuleId, Name = dto.RuleName }
        }).ToList();
    }

    public async Task<Models.TransformationStats> GetTransformationStatsAsync(DateTime? fromDate = null)
    {
        var statsDto = await _getStatsUseCase.ExecuteAsync(fromDate);
        return new Models.TransformationStats
        {
            TotalTransformations = statsDto.TotalTransformations,
            SuccessfulTransformations = statsDto.SuccessfulTransformations,
            FailedTransformations = statsDto.FailedTransformations,
            AverageTransformationTimeMs = statsDto.AverageTransformationTimeMs,
            TransformationsByFormat = statsDto.TransformationsByFormat,
            TransformationsByRule = statsDto.TransformationsByRule,
            DailyTrends = statsDto.DailyTrends.Select(t => new Models.TransformationTrend
            {
                Date = t.Date,
                TransformationCount = t.TransformationCount,
                AverageTimeMs = t.AverageTimeMs,
                SuccessCount = t.SuccessCount,
                FailureCount = t.FailureCount
            }).ToList()
        };
    }

    private async Task<string> ExecuteTransformationLogic(Models.TransformationRule rule, string inputData)
    {
        await Task.Delay(1); // Simulate async operation
        
        // Basic transformation logic based on format
        return (rule.SourceFormat, rule.TargetFormat) switch
        {
            ("HL7", "JSON") => TransformHl7ToJson(inputData),
            ("JSON", "HL7") => TransformJsonToHl7(inputData),
            ("HL7", "XML") => TransformHl7ToXml(inputData),
            ("XML", "HL7") => TransformXmlToHl7(inputData),
            ("HL7", "FHIR") => TransformHl7ToFhir(inputData),
            ("FHIR", "HL7") => TransformFhirToHl7(inputData),
            _ => throw new NotSupportedException($"Transformation from {rule.SourceFormat} to {rule.TargetFormat} is not supported")
        };
    }

    private string TransformHl7ToJson(string hl7Data)
    {
        var lines = hl7Data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var segments = lines.Select(line => 
        {
            var fields = line.Split('|');
            return new
            {
                SegmentType = fields[0],
                Fields = fields.Skip(1).Select((field, index) => new { Index = index + 1, Value = field }).ToArray()
            };
        }).ToArray();

        return JsonSerializer.Serialize(new { Segments = segments }, new JsonSerializerOptions { WriteIndented = true });
    }

    private string TransformJsonToHl7(string jsonData)
    {
        // Simplified reverse transformation
        return "MSH|^~\\&|SYSTEM|FACILITY|DEST|DEST|20240101120000||ADT^A01|12345|P|2.5\r\n" +
               "PID|1||123456^^^MRN||DOE^JOHN^M||19800101|M";
    }

    private string TransformHl7ToXml(string hl7Data)
    {
        var lines = hl7Data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var xml = "<HL7Message>\n";
        
        foreach (var line in lines)
        {
            var fields = line.Split('|');
            xml += $"  <{fields[0]}>\n";
            for (int i = 1; i < fields.Length; i++)
            {
                xml += $"    <Field{i}>{fields[i]}</Field{i}>\n";
            }
            xml += $"  </{fields[0]}>\n";
        }
        
        xml += "</HL7Message>";
        return xml;
    }

    private string TransformXmlToHl7(string xmlData)
    {
        // Simplified XML to HL7 conversion
        return "MSH|^~\\&|SYSTEM|FACILITY|DEST|DEST|20240101120000||ADT^A01|12345|P|2.5\r\n" +
               "PID|1||123456^^^MRN||DOE^JOHN^M||19800101|M";
    }

    private string TransformHl7ToFhir(string hl7Data)
    {
        // Simplified HL7 to FHIR conversion
        return JsonSerializer.Serialize(new
        {
            resourceType = "Patient",
            id = "example-patient",
            identifier = new[]
            {
                new { system = "urn:oid:2.16.840.1.113883.2.1.4.1", value = "123456" }
            },
            name = new[]
            {
                new { family = "DOE", given = new[] { "JOHN" } }
            },
            gender = "male",
            birthDate = "1980-01-01"
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    private string TransformFhirToHl7(string fhirData)
    {
        // Simplified FHIR to HL7 conversion
        return "MSH|^~\\&|SYSTEM|FACILITY|DEST|DEST|20240101120000||ADT^A01|12345|P|2.5\r\n" +
               "PID|1||123456^^^MRN||DOE^JOHN^M||19800101|M";
    }

    // Helper methods for TransformationDto mapping
    private string ExtractSourceFormat(string transformationType)
    {
        if (string.IsNullOrEmpty(transformationType)) return "HL7";
        var parts = transformationType.Split("->", StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0].Trim() : "HL7";
    }

    private string ExtractTargetFormat(string transformationType)
    {
        if (string.IsNullOrEmpty(transformationType)) return "JSON";
        var parts = transformationType.Split("->", StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[1].Trim() : "JSON";
    }
}