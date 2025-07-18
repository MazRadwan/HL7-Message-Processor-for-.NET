using HL7Processor.Core.Data;
using HL7Processor.Web.Services;
using HL7Processor.Web.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace HL7Processor.Web.Tests.Services;

public class SystemHealthServiceTests : IDisposable
{
    private readonly HL7ProcessorContext _context;
    private readonly Mock<ILogger<SystemHealthService>> _loggerMock;
    private readonly SystemHealthService _service;

    public SystemHealthServiceTests()
    {
        var options = new DbContextOptionsBuilder<HL7ProcessorContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new HL7ProcessorContext(options);
        _loggerMock = new Mock<ILogger<SystemHealthService>>();
        _service = new SystemHealthService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task GetSystemHealthAsync_WithHealthySystem_ShouldReturnHealthyStatus()
    {
        // Arrange - Add some processed messages (healthy state)
        _context.HL7Messages.AddRange(new[]
        {
            new HL7Message { Id = Guid.NewGuid(), ProcessingStatus = "Processed", Timestamp = DateTime.Now },
            new HL7Message { Id = Guid.NewGuid(), ProcessingStatus = "Processed", Timestamp = DateTime.Now }
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSystemHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.DatabaseConnected.Should().BeTrue();
        result.SignalRConnected.Should().BeTrue();
        result.QueueLength.Should().Be(0); // No pending/processing messages
        result.OverallStatus.Should().Be(SystemHealthIndicator.SystemStatus.Healthy);
    }

    [Fact]
    public async Task GetSystemHealthAsync_WithPendingMessages_ShouldReturnCorrectQueueLength()
    {
        // Arrange
        _context.HL7Messages.AddRange(new[]
        {
            new HL7Message { Id = Guid.NewGuid(), ProcessingStatus = "Pending", Timestamp = DateTime.Now },
            new HL7Message { Id = Guid.NewGuid(), ProcessingStatus = "Processing", Timestamp = DateTime.Now },
            new HL7Message { Id = Guid.NewGuid(), ProcessingStatus = "Processed", Timestamp = DateTime.Now }
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSystemHealthAsync();

        // Assert
        result.QueueLength.Should().Be(2); // Only pending and processing messages
        result.DatabaseConnected.Should().BeTrue();
    }

    [Fact]
    public async Task GetSystemHealthAsync_WithHighQueueLength_ShouldReturnWarningStatus()
    {
        // Arrange - Add many pending messages to trigger warning
        var pendingMessages = new List<HL7Message>();
        for (int i = 0; i < 1001; i++) // More than 1000 to trigger warning
        {
            pendingMessages.Add(new HL7Message 
            { 
                Id = Guid.NewGuid(), 
                ProcessingStatus = "Pending", 
                Timestamp = DateTime.Now 
            });
        }
        _context.HL7Messages.AddRange(pendingMessages);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSystemHealthAsync();

        // Assert
        result.QueueLength.Should().BeGreaterThan(1000);
        result.OverallStatus.Should().Be(SystemHealthIndicator.SystemStatus.Warning);
    }

    [Fact]
    public async Task GetSystemHealthAsync_WithDatabaseError_ShouldReturnCriticalStatus()
    {
        // Arrange
        _context.Dispose(); // Force database connectivity issues

        // Act
        var result = await _service.GetSystemHealthAsync();

        // Assert
        result.OverallStatus.Should().Be(SystemHealthIndicator.SystemStatus.Critical);
        
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting system health")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSystemHealthAsync_ShouldSetSignalRConnectedToTrue()
    {
        // Act
        var result = await _service.GetSystemHealthAsync();

        // Assert
        result.SignalRConnected.Should().BeTrue();
    }

    [Fact]
    public async Task GetSystemHealthAsync_ShouldIncludeSystemMetrics()
    {
        // Act
        var result = await _service.GetSystemHealthAsync();

        // Assert
        result.CpuUsage.Should().BeGreaterOrEqualTo(-1); // -1 indicates error, >= 0 indicates valid measurement
        result.MemoryUsage.Should().BeGreaterOrEqualTo(-1); // -1 indicates error, >= 0 indicates valid measurement
    }

    [Theory]
    [InlineData(SystemHealthIndicator.SystemStatus.Healthy, true, 100, 10, 100)]
    [InlineData(SystemHealthIndicator.SystemStatus.Warning, true, 1001, 10, 100)]
    [InlineData(SystemHealthIndicator.SystemStatus.Warning, true, 100, 85, 100)]
    [InlineData(SystemHealthIndicator.SystemStatus.Warning, true, 100, 10, 600)]
    [InlineData(SystemHealthIndicator.SystemStatus.Critical, false, 100, 10, 100)]
    public void DetermineOverallStatus_ShouldReturnCorrectStatus(
        SystemHealthIndicator.SystemStatus expectedStatus,
        bool databaseConnected,
        int queueLength,
        double cpuUsage,
        double memoryUsage)
    {
        // Arrange
        var health = new SystemHealthIndicator.SystemHealth
        {
            DatabaseConnected = databaseConnected,
            QueueLength = queueLength,
            CpuUsage = cpuUsage,
            MemoryUsage = memoryUsage
        };

        // Act - Use reflection to test private method
        var method = typeof(SystemHealthService).GetMethod("DetermineOverallStatus", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (SystemHealthIndicator.SystemStatus)method!.Invoke(_service, new object[] { health })!;

        // Assert
        result.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task GetSystemHealthAsync_WithEmptyDatabase_ShouldHandleGracefully()
    {
        // Act
        var result = await _service.GetSystemHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.DatabaseConnected.Should().BeTrue();
        result.QueueLength.Should().Be(0);
        result.OverallStatus.Should().Be(SystemHealthIndicator.SystemStatus.Healthy);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}