namespace HL7Processor.Infrastructure.Retention;

public class RetentionSettings
{
    public const string SectionName = "Retention";
    /// <summary>
    /// Number of days to keep processed HL7 messages in primary database before archiving/deletion.
    /// </summary>
    public int RetainDays { get; init; } = 30;
    /// <summary>
    /// If true, archive to a separate table instead of delete.
    /// </summary>
    public bool ArchiveInsteadOfDelete { get; init; } = false;
} 