using HL7Processor.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HL7Processor.Infrastructure;

public class TransformationSeedDataService
{
    private readonly IDbContextFactory<HL7DbContext> _contextFactory;
    private readonly ILogger<TransformationSeedDataService> _logger;

    public TransformationSeedDataService(IDbContextFactory<HL7DbContext> contextFactory, ILogger<TransformationSeedDataService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task SeedTransformationDataAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // Check if transformation data already exists
        var existingRules = await context.TransformationRules.AnyAsync();
        if (existingRules)
        {
            _logger.LogInformation("Transformation seed data already exists. Skipping seed.");
            return;
        }

        _logger.LogInformation("Seeding transformation data...");

        // Create transformation rules
        var rules = CreateTransformationRules();
        await context.TransformationRules.AddRangeAsync(rules);
        await context.SaveChangesAsync();

        // Create transformation history with demo data spanning past and future dates
        var history = CreateTransformationHistory(rules);
        await context.TransformationHistories.AddRangeAsync(history);
        await context.SaveChangesAsync();

        _logger.LogInformation("Transformation seed data completed successfully.");
    }

    private List<TransformationRule> CreateTransformationRules()
    {
        var rules = new List<TransformationRule>();

        // HL7 to JSON rules
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid(),
            Name = "ADT to JSON Patient",
            Description = "Convert ADT messages to JSON patient format",
            SourceFormat = "HL7",
            TargetFormat = "JSON",
            RuleDefinition = JsonSerializer.Serialize(new
            {
                mappings = new[]
                {
                    new { source = "MSH.3", target = "sendingApplication" },
                    new { source = "MSH.4", target = "sendingFacility" },
                    new { source = "PID.3", target = "patientId" },
                    new { source = "PID.5", target = "patientName" },
                    new { source = "PID.7", target = "birthDate" },
                    new { source = "PID.8", target = "gender" }
                }
            }),
            IsActive = true,
            CreatedBy = "System",
            CreatedAt = DateTime.UtcNow.AddMonths(-6),
            ModifiedAt = DateTime.UtcNow.AddMonths(-6)
        });

        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid(),
            Name = "HL7 to FHIR Patient",
            Description = "Transform HL7 messages to FHIR Patient resources",
            SourceFormat = "HL7",
            TargetFormat = "FHIR",
            RuleDefinition = JsonSerializer.Serialize(new
            {
                resourceType = "Patient",
                mappings = new[]
                {
                    new { source = "PID.3", target = "identifier.value" },
                    new { source = "PID.5", target = "name.family" },
                    new { source = "PID.7", target = "birthDate" },
                    new { source = "PID.8", target = "gender" }
                }
            }),
            IsActive = true,
            CreatedBy = "Admin",
            CreatedAt = DateTime.UtcNow.AddMonths(-4),
            ModifiedAt = DateTime.UtcNow.AddMonths(-2)
        });

        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid(),
            Name = "ORM to XML Order",
            Description = "Convert ORM messages to XML order format",
            SourceFormat = "HL7",
            TargetFormat = "XML",
            RuleDefinition = JsonSerializer.Serialize(new
            {
                rootElement = "Order",
                mappings = new[]
                {
                    new { source = "MSH.10", target = "controlId" },
                    new { source = "PID.3", target = "patient.id" },
                    new { source = "ORC.1", target = "orderControl" },
                    new { source = "OBR.1", target = "orderNumber" },
                    new { source = "OBR.4", target = "orderCode" }
                }
            }),
            IsActive = true,
            CreatedBy = "System",
            CreatedAt = DateTime.UtcNow.AddMonths(-3),
            ModifiedAt = DateTime.UtcNow.AddMonths(-1)
        });

        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid(),
            Name = "JSON to HL7 Converter",
            Description = "Convert JSON patient data back to HL7 format",
            SourceFormat = "JSON",
            TargetFormat = "HL7",
            RuleDefinition = JsonSerializer.Serialize(new
            {
                template = "MSH|^~\\&|{sendingApplication}|{sendingFacility}|DEST|DEST|{timestamp}||ADT^A01|{controlId}|P|2.5\r\nPID|1||{patientId}|||{patientName}||{birthDate}|{gender}",
                mappings = new[]
                {
                    new { source = "sendingApplication", target = "MSH.3" },
                    new { source = "patientId", target = "PID.3" },
                    new { source = "patientName", target = "PID.5" }
                }
            }),
            IsActive = true,
            CreatedBy = "Developer",
            CreatedAt = DateTime.UtcNow.AddMonths(-2),
            ModifiedAt = DateTime.UtcNow.AddDays(-15)
        });

        // Future rules for demo
        rules.Add(new TransformationRule
        {
            Id = Guid.NewGuid(),
            Name = "Advanced FHIR Bundle",
            Description = "Future enhancement for FHIR Bundle transformations",
            SourceFormat = "HL7",
            TargetFormat = "FHIR",
            RuleDefinition = JsonSerializer.Serialize(new
            {
                resourceType = "Bundle",
                type = "transaction",
                mappings = new[]
                {
                    new { source = "MSH", target = "entry[0].resource.Patient" },
                    new { source = "OBR", target = "entry[1].resource.ServiceRequest" }
                }
            }),
            IsActive = false,
            CreatedBy = "Future User",
            CreatedAt = DateTime.UtcNow.AddMonths(3),
            ModifiedAt = DateTime.UtcNow.AddMonths(3)
        });

        return rules;
    }

    private List<TransformationHistory> CreateTransformationHistory(List<TransformationRule> rules)
    {
        var history = new List<TransformationHistory>();
        var random = new Random();

        // Generate history data spanning from 6 months ago to 6 months in the future
        var startDate = DateTime.UtcNow.AddMonths(-6);
        var endDate = DateTime.UtcNow.AddMonths(6);

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            // Skip some days for realistic data
            if (random.NextDouble() < 0.3) continue;

            var dailyTransformations = random.Next(5, 25);
            
            for (int i = 0; i < dailyTransformations; i++)
            {
                var rule = rules[random.Next(rules.Count)];
                var isSuccess = random.NextDouble() > 0.15; // 85% success rate
                
                var transformationTime = date.AddHours(random.Next(0, 24))
                                             .AddMinutes(random.Next(0, 60))
                                             .AddSeconds(random.Next(0, 60));

                var historyItem = new TransformationHistory
                {
                    Id = Guid.NewGuid(),
                    RuleId = rule.Id,
                    TransformationTimeMs = random.Next(10, 500),
                    Success = isSuccess,
                    CreatedAt = transformationTime
                };

                if (isSuccess)
                {
                    historyItem.OutputData = GenerateOutputData(rule.SourceFormat, rule.TargetFormat);
                }
                else
                {
                    historyItem.ErrorMessage = GenerateErrorMessage();
                }

                history.Add(historyItem);
            }
        }

        return history;
    }

    private string GenerateOutputData(string sourceFormat, string targetFormat)
    {
        return (sourceFormat, targetFormat) switch
        {
            ("HL7", "JSON") => JsonSerializer.Serialize(new
            {
                sendingApplication = "EPIC",
                sendingFacility = "HOSPITAL",
                patientId = "123456",
                patientName = "DOE^JOHN^M",
                birthDate = "19800101",
                gender = "M"
            }, new JsonSerializerOptions { WriteIndented = true }),
            
            ("HL7", "FHIR") => JsonSerializer.Serialize(new
            {
                resourceType = "Patient",
                id = "example-patient",
                identifier = new[] { new { value = "123456" } },
                name = new[] { new { family = "DOE", given = new[] { "JOHN" } } },
                gender = "male",
                birthDate = "1980-01-01"
            }, new JsonSerializerOptions { WriteIndented = true }),
            
            ("HL7", "XML") => "<Order><controlId>12345</controlId><patient><id>123456</id></patient><orderControl>NW</orderControl></Order>",
            
            ("JSON", "HL7") => "MSH|^~\\&|EPIC|HOSPITAL|DEST|DEST|20240101120000||ADT^A01|12345|P|2.5\r\nPID|1||123456^^^MRN||DOE^JOHN^M||19800101|M",
            
            _ => "Transformation completed successfully"
        };
    }

    private string GenerateErrorMessage()
    {
        var errors = new[]
        {
            "Invalid HL7 message format: Missing MSH segment",
            "Mapping error: Source field PID.5 not found",
            "Validation failed: Invalid date format in PID.7",
            "Transformation timeout: Processing exceeded 30 seconds",
            "Parse error: Malformed JSON input data",
            "Schema validation failed: Required field missing",
            "Connection timeout: Unable to reach external service",
            "Memory limit exceeded during large message processing"
        };

        var random = new Random();
        return errors[random.Next(errors.Length)];
    }
}