using HL7Processor.Core.Data;
using HL7Processor.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace HL7Processor.Web.Tests.Services;

public class DashboardServiceTests : IDisposable
{
    private readonly HL7ProcessorContext _context;
    private readonly Mock<ILogger<DashboardService>> _loggerMock;
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<HL7ProcessorContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new HL7ProcessorContext(options);
        _loggerMock = new Mock<ILogger<DashboardService>>();
        _service = new DashboardService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task GetDashboardDataAsync_WithNoMessages_ShouldReturnEmptyData()
    {
        // Act
        var result = await _service.GetDashboardDataAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalMessages.Should().Be(0);
        result.ProcessedToday.Should().Be(0);
        result.PendingMessages.Should().Be(0);
        result.ErrorsToday.Should().Be(0);
        result.RecentMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDashboardDataAsync_WithMessages_ShouldReturnCorrectCounts()
    {
        // Arrange
        var today = DateTime.Today;
        await SeedTestData(today);

        // Act
        var result = await _service.GetDashboardDataAsync();

        // Assert
        result.TotalMessages.Should().Be(5);
        result.ProcessedToday.Should().Be(2);
        result.PendingMessages.Should().Be(2);
        result.ErrorsToday.Should().Be(1);
        result.RecentMessages.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetDashboardDataAsync_ShouldOrderRecentMessagesByTimestampDescending()
    {
        // Arrange
        var today = DateTime.Today;
        await SeedTestData(today);

        // Act
        var result = await _service.GetDashboardDataAsync();

        // Assert
        result.RecentMessages.Should().HaveCount(5);
        var timestamps = result.RecentMessages.Select(m => m.Timestamp).ToList();
        timestamps.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetDashboardDataAsync_ShouldLimitRecentMessagesTo10()
    {
        // Arrange
        var today = DateTime.Today;
        
        // Add 15 messages
        for (int i = 0; i < 15; i++)
        {
            _context.HL7Messages.Add(new HL7Message
            {
                Id = Guid.NewGuid(),
                MessageType = "ADT^A01",
                PatientId = $"P{i:000}",
                ProcessingStatus = "Processed",
                Timestamp = today.AddMinutes(i)
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDashboardDataAsync();

        // Assert
        result.TotalMessages.Should().Be(15);
        result.RecentMessages.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetDashboardDataAsync_WithDatabaseError_ShouldReturnEmptyDataAndLogError()
    {
        // Arrange
        _context.Dispose(); // Force database error

        // Act
        var result = await _service.GetDashboardDataAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalMessages.Should().Be(0);
        result.ProcessedToday.Should().Be(0);
        result.PendingMessages.Should().Be(0);
        result.ErrorsToday.Should().Be(0);
        result.RecentMessages.Should().BeEmpty();

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting dashboard data")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetDashboardDataAsync_ShouldMapRecentMessagesCorrectly()
    {
        // Arrange
        var today = DateTime.Today;
        var messageId = Guid.NewGuid();
        var timestamp = today.AddHours(10);

        _context.HL7Messages.Add(new HL7Message
        {
            Id = messageId,
            MessageType = "ORU^R01",
            PatientId = "P12345",
            ProcessingStatus = "Processed",
            Timestamp = timestamp
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDashboardDataAsync();

        // Assert
        var recentMessage = result.RecentMessages.First();
        recentMessage.MessageType.Should().Be("ORU^R01");
        recentMessage.PatientId.Should().Be("P12345");
        recentMessage.Status.Should().Be("Processed");
        recentMessage.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public async Task GetDashboardDataAsync_WithNullFields_ShouldHandleGracefully()
    {
        // Arrange
        _context.HL7Messages.Add(new HL7Message
        {
            Id = Guid.NewGuid(),
            MessageType = null,
            PatientId = null,
            ProcessingStatus = null,
            Timestamp = DateTime.Today
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDashboardDataAsync();

        // Assert
        var recentMessage = result.RecentMessages.First();
        recentMessage.MessageType.Should().Be("Unknown");
        recentMessage.PatientId.Should().Be("N/A");
        recentMessage.Status.Should().Be("Unknown");
    }

    private async Task SeedTestData(DateTime today)
    {
        var messages = new[]
        {
            new HL7Message { Id = Guid.NewGuid(), MessageType = "ADT^A01", PatientId = "P001", ProcessingStatus = "Processed", Timestamp = today.AddHours(1) },
            new HL7Message { Id = Guid.NewGuid(), MessageType = "ORU^R01", PatientId = "P002", ProcessingStatus = "Processed", Timestamp = today.AddHours(2) },
            new HL7Message { Id = Guid.NewGuid(), MessageType = "ADT^A08", PatientId = "P003", ProcessingStatus = "Pending", Timestamp = today.AddHours(3) },
            new HL7Message { Id = Guid.NewGuid(), MessageType = "ORU^R01", PatientId = "P004", ProcessingStatus = "Processing", Timestamp = today.AddHours(4) },
            new HL7Message { Id = Guid.NewGuid(), MessageType = "ADT^A01", PatientId = "P005", ProcessingStatus = "Error", Timestamp = today.AddHours(5) }
        };

        _context.HL7Messages.AddRange(messages);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}