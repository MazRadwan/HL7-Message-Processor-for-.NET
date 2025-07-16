using System.ComponentModel.DataAnnotations;

namespace HL7Processor.Infrastructure.Entities;

public class AuditLogEntry
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    public string TableName { get; set; } = string.Empty;
    [Required]
    public string Action { get; set; } = string.Empty; // Added/Modified/Deleted
    [Required]
    public string KeyValues { get; set; } = string.Empty; // JSON of key
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? UserName { get; set; }
} 