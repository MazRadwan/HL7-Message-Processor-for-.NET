using HL7Processor.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace HL7Processor.Infrastructure.Retention;

public interface IDataRetentionService
{
    Task ApplyRetentionPolicyAsync(CancellationToken token = default);
}

public class DataRetentionService : IDataRetentionService
{
    private readonly HL7DbContext _db;
    private readonly RetentionSettings _settings;
    private readonly ILogger<DataRetentionService> _logger;

    public DataRetentionService(HL7DbContext db, RetentionSettings settings, ILogger<DataRetentionService> logger)
    {
        _db = db;
        _settings = settings;
        _logger = logger;
    }

    public async Task ApplyRetentionPolicyAsync(CancellationToken token = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-_settings.RetainDays);
        _logger.LogDebug("Applying data retention policy. RetainDays={Days}, Cutoff={Cutoff}", _settings.RetainDays, cutoff);

        var obsoleteMessages = await _db.Messages.AsNoTracking()
            .Where(m => m.Timestamp < cutoff)
            .Select(m => m.Id)
            .ToListAsync(token);

        if (obsoleteMessages.Count == 0)
        {
            _logger.LogDebug("No messages eligible for retention cleanup");
            return;
        }

        if (_settings.ArchiveInsteadOfDelete)
        {
            _logger.LogInformation("Archiving {Count} HL7 messages older than {Cutoff}", obsoleteMessages.Count, cutoff);

            foreach (var batch in obsoleteMessages.Chunk(100))
            {
                var msgs = await _db.Messages
                    .Where(m => batch.Contains(m.Id))
                    .ToListAsync(token);

                // Map messages to archive entities
                var archived = msgs.Select(m => new Entities.HL7ArchivedMessageEntity
                {
                    Id = Guid.NewGuid(),
                    OriginalMessageId = m.Id,
                    MessageType = m.MessageType,
                    Version = m.Version,
                    OriginalTimestamp = m.Timestamp,
                    ArchivedAt = DateTime.UtcNow
                }).ToList();

                await _db.ArchivedMessages.AddRangeAsync(archived, token);
                _db.Messages.RemoveRange(msgs);
                await _db.SaveChangesAsync(token);
            }
        }
        else
        {
            _logger.LogInformation("Deleting {Count} HL7 messages older than {Cutoff}", obsoleteMessages.Count, cutoff);
            // Load and delete in batches to avoid FK constraint issues (cascade configured).
            foreach (var batch in obsoleteMessages.Chunk(100))
            {
                var msgs = await _db.Messages.Where(m => batch.Contains(m.Id)).ToListAsync(token);
                _db.Messages.RemoveRange(msgs);
                await _db.SaveChangesAsync(token);
            }
        }
    }
} 