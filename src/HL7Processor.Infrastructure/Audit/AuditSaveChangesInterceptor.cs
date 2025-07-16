using HL7Processor.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

namespace HL7Processor.Infrastructure.Audit;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AddAuditEntries(eventData.Context!);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        AddAuditEntries(eventData.Context!);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void AddAuditEntries(DbContext context)
    {
        if (context == null) return;
        var auditEntries = new List<AuditLogEntry>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLogEntry || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var audit = new AuditLogEntry
            {
                TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                Action = entry.State.ToString(),
                KeyValues = JsonSerializer.Serialize(GetPrimaryKey(entry)),
                UserName = "system" // placeholder; inject user provider if needed
            };

            switch (entry.State)
            {
                case EntityState.Added:
                    audit.NewValues = JsonSerializer.Serialize(entry.CurrentValues.ToObject());
                    break;
                case EntityState.Modified:
                    audit.OldValues = JsonSerializer.Serialize(entry.OriginalValues.ToObject());
                    audit.NewValues = JsonSerializer.Serialize(entry.CurrentValues.ToObject());
                    break;
                case EntityState.Deleted:
                    audit.OldValues = JsonSerializer.Serialize(entry.OriginalValues.ToObject());
                    break;
            }
            auditEntries.Add(audit);
        }

        if (auditEntries.Count > 0)
        {
            context.Set<AuditLogEntry>().AddRange(auditEntries);
        }
    }

    private static Dictionary<string, object?> GetPrimaryKey(EntityEntry entry)
    {
        var key = new Dictionary<string, object?>();
        foreach (var prop in entry.Properties)
        {
            if (prop.Metadata.IsPrimaryKey())
            {
                key[prop.Metadata.Name] = prop.CurrentValue;
            }
        }
        return key;
    }
} 