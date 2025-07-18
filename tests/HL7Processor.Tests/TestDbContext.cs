using HL7Processor.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace HL7Processor.Tests;

public class TestDbContext : HL7Processor.Infrastructure.HL7DbContext
{
    public TestDbContext(DbContextOptions<HL7Processor.Infrastructure.HL7DbContext> options)
        : base(options) { }
}