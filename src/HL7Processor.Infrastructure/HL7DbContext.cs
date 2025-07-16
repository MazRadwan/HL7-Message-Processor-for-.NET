using HL7Processor.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace HL7Processor.Infrastructure;

public class HL7DbContext : DbContext
{
    public HL7DbContext(DbContextOptions<HL7DbContext> options) : base(options) { }

    public DbSet<HL7MessageEntity> Messages => Set<HL7MessageEntity>();
    public DbSet<HL7SegmentEntity> Segments => Set<HL7SegmentEntity>();
    public DbSet<HL7FieldEntity> Fields => Set<HL7FieldEntity>();

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
    }
} 