using FluentAssertions;
using HL7Processor.Infrastructure;
using HL7Processor.Infrastructure.Entities;
using HL7Processor.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HL7Processor.Tests.Infrastructure;

public class MessageRepositoryTests
{
    private static HL7DbContext CreateInMemoryDbContext()
    {
        var opts = new DbContextOptionsBuilder<HL7DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new HL7DbContext(opts);
    }

    [Fact]
    public async Task Add_And_Get_Message_Should_Persist()
    {
        await using var db = CreateInMemoryDbContext();
        var repo = new MessageRepository(db);

        var msg = new HL7MessageEntity
        {
            Id = Guid.NewGuid(),
            MessageType = "ADT^A01",
            Timestamp = DateTime.UtcNow
        };
        msg.Segments.Add(new HL7SegmentEntity
        {
            Type = "MSH",
            SequenceNumber = 1,
            Fields = new List<HL7FieldEntity>
            {
                new() { Position = 1, Value = "|" }
            }
        });

        await repo.AddAsync(msg);
        await repo.SaveChangesAsync();

        var fetched = await repo.GetAsync(msg.Id);
        fetched.Should().NotBeNull();
        fetched!.Segments.Should().HaveCount(1);
    }
} 