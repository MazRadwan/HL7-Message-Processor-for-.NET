using HL7Processor.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using HL7Processor.Infrastructure.Audit;

namespace HL7Processor.Infrastructure;

public class HL7DbContext : DbContext
{
    public HL7DbContext(DbContextOptions<HL7DbContext> options) : base(options) { }

    public DbSet<HL7MessageEntity> Messages => Set<HL7MessageEntity>();
    public DbSet<HL7SegmentEntity> Segments => Set<HL7SegmentEntity>();
    public DbSet<HL7FieldEntity> Fields => Set<HL7FieldEntity>();
    public DbSet<HL7ArchivedMessageEntity> ArchivedMessages => Set<HL7ArchivedMessageEntity>();
    
    // Stage 6b: Parser & Validation tables
    public DbSet<ValidationResult> ValidationResults => Set<ValidationResult>();
    public DbSet<ParserMetric> ParserMetrics => Set<ParserMetric>();

    // Remove OnConfiguring when using DbContext pooling
    // Interceptors should be configured in Program.cs instead

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HL7MessageEntity>()
            .HasMany(m => m.Segments)
            .WithOne(s => s.Message)
            .HasForeignKey(s => s.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HL7SegmentEntity>()
            .HasMany(s => s.Fields)
            .WithOne(f => f.Segment)
            .HasForeignKey(f => f.SegmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Stage 6b: Validation Results relationship
        modelBuilder.Entity<ValidationResult>()
            .HasOne(v => v.Message)
            .WithMany()
            .HasForeignKey(v => v.MessageId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
} 