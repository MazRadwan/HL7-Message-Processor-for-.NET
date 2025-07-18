using HL7Processor.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace HL7Processor.Infrastructure;

public class SeedDataService
{
    private readonly HL7DbContext _context;
    private readonly ILogger<SeedDataService> _logger;
    private readonly IHostEnvironment _environment;

    public SeedDataService(HL7DbContext context, ILogger<SeedDataService> logger, IHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _environment = environment;
    }

    public async Task SeedDataAsync()
    {
        try
        {
            // Only seed in development environment
            if (!_environment.IsDevelopment())
            {
                _logger.LogInformation("Not in development environment. Skipping seed data.");
                return;
            }

            // Check if data already exists (idempotent)
            if (await _context.Messages.AnyAsync())
            {
                _logger.LogInformation("Database already contains data. Skipping seed.");
                return;
            }

            _logger.LogInformation("Seeding database with sample data...");

            var messages = new List<HL7MessageEntity>();
            var now = DateTime.UtcNow;

            // Add recent messages for dashboard
            messages.AddRange(new[]
            {
                new HL7MessageEntity { Id = Guid.NewGuid(), MessageType = "ADT^A01", Version = "2.4", Timestamp = now.AddHours(-2), ProcessingStatus = "Processed", PatientId = "P001234" },
                new HL7MessageEntity { Id = Guid.NewGuid(), MessageType = "ADT^A08", Version = "2.4", Timestamp = now.AddHours(-1), ProcessingStatus = "Processed", PatientId = "P001235" },
                new HL7MessageEntity { Id = Guid.NewGuid(), MessageType = "ORU^R01", Version = "2.4", Timestamp = now.AddMinutes(-30), ProcessingStatus = "Processed", PatientId = "P001236" },
                new HL7MessageEntity { Id = Guid.NewGuid(), MessageType = "ADT^A01", Version = "2.4", Timestamp = now.AddMinutes(-25), ProcessingStatus = "Pending", PatientId = "P001237" },
                new HL7MessageEntity { Id = Guid.NewGuid(), MessageType = "ORU^R01", Version = "2.4", Timestamp = now.AddMinutes(-20), ProcessingStatus = "Processing", PatientId = "P001238" },
                new HL7MessageEntity { Id = Guid.NewGuid(), MessageType = "ADT^A08", Version = "2.4", Timestamp = now.AddMinutes(-15), ProcessingStatus = "Error", PatientId = "P001239" },
                new HL7MessageEntity { Id = Guid.NewGuid(), MessageType = "ORU^R01", Version = "2.4", Timestamp = now.AddMinutes(-10), ProcessingStatus = "Processed", PatientId = "P001240" },
                new HL7MessageEntity { Id = Guid.NewGuid(), MessageType = "ADT^A01", Version = "2.4", Timestamp = now.AddMinutes(-5), ProcessingStatus = "Processed", PatientId = "P001241" },
                new HL7MessageEntity { Id = Guid.NewGuid(), MessageType = "ORU^R01", Version = "2.4", Timestamp = now.AddMinutes(-2), ProcessingStatus = "Pending", PatientId = "P001242" },
                new HL7MessageEntity { Id = Guid.NewGuid(), MessageType = "ADT^A08", Version = "2.4", Timestamp = now.AddMinutes(-1), ProcessingStatus = "Processed", PatientId = "P001243" }
            });

            // Add historical data for charts
            var random = new Random(42); // Fixed seed for consistent data
            for (int i = 1; i <= 100; i++)
            {
                var messageTypes = new[] { "ADT^A01", "ORU^R01", "ADT^A08", "ORM^O01", "ACK^A01" };
                var statuses = new[] { "Processed", "Processed", "Processed", "Processed", "Processed", "Processed", "Processed", "Pending", "Processing", "Error" };
                
                messages.Add(new HL7MessageEntity
                {
                    Id = Guid.NewGuid(),
                    MessageType = messageTypes[random.Next(messageTypes.Length)],
                    Version = "2.4",
                    Timestamp = now.AddHours(-random.Next(1, 72)), // Last 3 days
                    ProcessingStatus = statuses[random.Next(statuses.Length)],
                    PatientId = $"P{random.Next(100000, 999999)}"
                });
            }

            _context.Messages.AddRange(messages);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully seeded {Count} messages to the database", messages.Count);

            // Log statistics
            var stats = await _context.Messages
                .GroupBy(m => m.ProcessingStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var stat in stats)
            {
                _logger.LogInformation("Status: {Status}, Count: {Count}", stat.Status, stat.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding database");
            throw;
        }
    }
}