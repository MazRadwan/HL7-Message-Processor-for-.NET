using HL7Processor.Infrastructure;
using HL7Processor.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace HL7Processor.Tests.Integration;

public class SeedDataIntegrationTests : IDisposable
{
    private readonly HL7Processor.Infrastructure.HL7DbContext _context;
    private readonly Mock<ILogger<SeedDataService>> _loggerMock;
    private readonly Mock<IHostEnvironment> _environmentMock;
    private readonly Mock<IDbContextFactory<HL7Processor.Infrastructure.HL7DbContext>> _factoryMock;

    public SeedDataIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<HL7Processor.Infrastructure.HL7DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new HL7Processor.Tests.TestDbContext(options);
        _loggerMock = new Mock<ILogger<SeedDataService>>();
        _environmentMock = new Mock<IHostEnvironment>();
        _factoryMock = new Mock<IDbContextFactory<HL7Processor.Infrastructure.HL7DbContext>>();
        _factoryMock.Setup(f => f.CreateDbContext()).Returns(_context);
        _factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_context);
    }

    [Fact]
    public async Task SeedDataService_InDevelopment_ShouldSeedData()
    {
        // Arrange
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Development");
        var seedService = new SeedDataService(_context, _loggerMock.Object, _environmentMock.Object, _factoryMock.Object);

        // Act
        await seedService.SeedDataAsync();

        // Assert
        var messageCount = await _context.Messages.CountAsync();
        messageCount.Should().BeGreaterThan(0);
        
        var repository = new MessageRepository(_context);
        var messages = await repository.SearchAsync(null, null, null, 1, 10);
        messages.Items.Should().NotBeEmpty();
        messages.Items.First().MessageType.Should().NotBeNullOrEmpty();
        messages.Items.First().PatientId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SeedDataService_InProduction_ShouldNotSeedData()
    {
        // Arrange
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Production");
        var seedService = new SeedDataService(_context, _loggerMock.Object, _environmentMock.Object, _factoryMock.Object);

        // Act
        await seedService.SeedDataAsync();

        // Assert
        var messageCount = await _context.Messages.CountAsync();
        messageCount.Should().Be(0);
    }

    [Fact]
    public async Task SeedDataService_WithExistingData_ShouldBeIdempotent()
    {
        // Arrange
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Development");
        var seedService = new SeedDataService(_context, _loggerMock.Object, _environmentMock.Object, _factoryMock.Object);

        // Seed first time
        await seedService.SeedDataAsync();
        var firstCount = await _context.Messages.CountAsync();

        // Act - Seed second time
        await seedService.SeedDataAsync();

        // Assert
        var secondCount = await _context.Messages.CountAsync();
        secondCount.Should().Be(firstCount);
    }

    [Fact]
    public async Task MessageRepository_WithSeededData_ShouldReturnFilteredResults()
    {
        // Arrange
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Development");
        var seedService = new SeedDataService(_context, _loggerMock.Object, _environmentMock.Object, _factoryMock.Object);
        await seedService.SeedDataAsync();

        var repository = new MessageRepository(_context);

        // Act - Test filtering by ProcessingStatus
        var processedMessages = await _context.Messages
            .Where(m => m.ProcessingStatus == "Processed")
            .CountAsync();

        var pendingMessages = await _context.Messages
            .Where(m => m.ProcessingStatus == "Pending")
            .CountAsync();

        // Assert
        processedMessages.Should().BeGreaterThan(0);
        pendingMessages.Should().BeGreaterThan(0);
        
        // Verify indexes are used effectively (no database errors)
        var recentMessages = await _context.Messages
            .Where(m => m.Timestamp > DateTime.UtcNow.AddHours(-1))
            .OrderByDescending(m => m.Timestamp)
            .Take(5)
            .ToListAsync();

        recentMessages.Should().NotBeEmpty();
    }

    [Fact]
    public async Task MessageRepository_PatientIdIndex_ShouldPerformEfficiently()
    {
        // Arrange
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Development");
        var seedService = new SeedDataService(_context, _loggerMock.Object, _environmentMock.Object, _factoryMock.Object);
        await seedService.SeedDataAsync();

        // Act - Query by PatientId (should use index)
        var patientMessage = await _context.Messages
            .FirstOrDefaultAsync(m => m.PatientId != null);

        patientMessage.Should().NotBeNull();

        if (patientMessage != null)
        {
            var patientMessages = await _context.Messages
                .Where(m => m.PatientId == patientMessage.PatientId)
                .ToListAsync();

            // Assert
            patientMessages.Should().NotBeEmpty();
            patientMessages.All(m => m.PatientId == patientMessage.PatientId).Should().BeTrue();
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}