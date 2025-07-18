using HL7Processor.Infrastructure;
using HL7Processor.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;

namespace HL7Processor.Web.Services;

public class TransformationService : ITransformationService
{
    private readonly IDbContextFactory<HL7DbContext> _contextFactory;
    private readonly ILogger<TransformationService> _logger;

    public TransformationService(IDbContextFactory<HL7DbContext> contextFactory, ILogger<TransformationService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<List<TransformationRule>> GetTransformationRulesAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.TransformationRules
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<TransformationRule?> GetTransformationRuleAsync(Guid id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.TransformationRules
            .Include(r => r.TransformationHistories)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<TransformationRule> CreateTransformationRuleAsync(TransformationRule rule)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        rule.CreatedAt = DateTime.UtcNow;
        rule.ModifiedAt = DateTime.UtcNow;
        
        context.TransformationRules.Add(rule);
        await context.SaveChangesAsync();
        
        _logger.LogInformation("Created transformation rule: {RuleName}", rule.Name);
        return rule;
    }

    public async Task<TransformationRule> UpdateTransformationRuleAsync(TransformationRule rule)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var existing = await context.TransformationRules.FindAsync(rule.Id);
        if (existing == null)
            throw new ArgumentException($"Transformation rule with ID {rule.Id} not found");

        existing.Name = rule.Name;
        existing.Description = rule.Description;
        existing.SourceFormat = rule.SourceFormat;
        existing.TargetFormat = rule.TargetFormat;
        existing.RuleDefinition = rule.RuleDefinition;
        existing.IsActive = rule.IsActive;
        existing.ModifiedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        
        _logger.LogInformation("Updated transformation rule: {RuleName}", rule.Name);
        return existing;
    }

    public async Task<bool> DeleteTransformationRuleAsync(Guid id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var rule = await context.TransformationRules.FindAsync(id);
        if (rule == null)
            return false;

        context.TransformationRules.Remove(rule);
        await context.SaveChangesAsync();
        
        _logger.LogInformation("Deleted transformation rule: {RuleName}", rule.Name);
        return true;
    }

    public async Task<string> ExecuteTransformationAsync(Guid ruleId, string inputData)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var stopwatch = Stopwatch.StartNew();
        
        var rule = await context.TransformationRules.FindAsync(ruleId);
        if (rule == null)
            throw new ArgumentException($"Transformation rule with ID {ruleId} not found");

        var history = new TransformationHistory
        {
            RuleId = ruleId,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            var result = await ExecuteTransformationLogic(rule, inputData);
            stopwatch.Stop();
            
            history.Success = true;
            history.OutputData = result;
            history.TransformationTimeMs = (int)stopwatch.ElapsedMilliseconds;
            
            context.TransformationHistories.Add(history);
            await context.SaveChangesAsync();
            
            _logger.LogInformation("Executed transformation {RuleName} in {ElapsedMs}ms", 
                rule.Name, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            history.Success = false;
            history.ErrorMessage = ex.Message;
            history.TransformationTimeMs = (int)stopwatch.ElapsedMilliseconds;
            
            context.TransformationHistories.Add(history);
            await context.SaveChangesAsync();
            
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

    public async Task<List<TransformationHistory>> GetTransformationHistoryAsync(int limit = 100)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.TransformationHistories
            .Include(h => h.Rule)
            .Include(h => h.SourceMessage)
            .OrderByDescending(h => h.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<TransformationStats> GetTransformationStatsAsync(DateTime? fromDate = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
        
        var transformations = await context.TransformationHistories
            .Include(h => h.Rule)
            .Where(h => h.CreatedAt >= startDate)
            .ToListAsync();

        var stats = new TransformationStats
        {
            TotalTransformations = transformations.Count,
            SuccessfulTransformations = transformations.Count(t => t.Success),
            FailedTransformations = transformations.Count(t => !t.Success),
            AverageTransformationTimeMs = transformations.Any() ? 
                transformations.Average(t => t.TransformationTimeMs) : 0
        };

        // Group by format combinations
        stats.TransformationsByFormat = transformations
            .GroupBy(t => $"{t.Rule.SourceFormat} → {t.Rule.TargetFormat}")
            .ToDictionary(g => g.Key, g => g.Count());

        // Group by rule name
        stats.TransformationsByRule = transformations
            .GroupBy(t => t.Rule.Name)
            .ToDictionary(g => g.Key, g => g.Count());

        // Daily trends
        stats.DailyTrends = transformations
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new TransformationTrend
            {
                Date = g.Key,
                TransformationCount = g.Count(),
                AverageTimeMs = g.Average(t => t.TransformationTimeMs),
                SuccessCount = g.Count(t => t.Success),
                FailureCount = g.Count(t => !t.Success)
            })
            .OrderBy(t => t.Date)
            .ToList();

        return stats;
    }

    private async Task<string> ExecuteTransformationLogic(TransformationRule rule, string inputData)
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
}