using Bunit;
using HL7Processor.Web.Pages;
using HL7Processor.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using Xunit;
using FluentAssertions;

namespace HL7Processor.Web.Tests.Pages;

public class IndexTests : TestContext
{
    private readonly Mock<IDashboardService> _dashboardServiceMock;

    public IndexTests()
    {
        _dashboardServiceMock = new Mock<IDashboardService>();
        
        Services.AddSingleton(_dashboardServiceMock.Object);
        Services.AddSingleton<IJSRuntime>(new MockJSRuntime());
    }

    [Fact]
    public void Index_ShouldRender_WithCorrectTitle()
    {
        // Arrange
        _dashboardServiceMock.Setup(x => x.GetDashboardDataAsync())
            .ReturnsAsync(new DashboardData());

        // Act
        var component = RenderComponent<Index>();

        // Assert
        component.Find("title").TextContent.Should().Contain("HL7 Processor Dashboard");
    }

    [Fact]
    public void Index_ShouldDisplay_MetricCards()
    {
        // Arrange
        var dashboardData = new DashboardData
        {
            TotalMessages = 1000,
            ProcessedToday = 150,
            PendingMessages = 25,
            ErrorsToday = 5
        };

        _dashboardServiceMock.Setup(x => x.GetDashboardDataAsync())
            .ReturnsAsync(dashboardData);

        // Act
        var component = RenderComponent<Index>();

        // Assert
        var cards = component.FindAll(".card");
        cards.Should().HaveCountGreaterOrEqualTo(4);

        component.Markup.Should().Contain("1000"); // Total Messages
        component.Markup.Should().Contain("150");  // Processed Today
        component.Markup.Should().Contain("25");   // Pending Messages
        component.Markup.Should().Contain("5");    // Errors Today
    }

    [Fact]
    public void Index_ShouldDisplay_RecentMessages()
    {
        // Arrange
        var dashboardData = new DashboardData
        {
            RecentMessages = new List<RecentMessage>
            {
                new RecentMessage
                {
                    MessageType = "ADT^A01",
                    PatientId = "P12345",
                    Status = "Processed",
                    Timestamp = DateTime.Now.AddMinutes(-10)
                },
                new RecentMessage
                {
                    MessageType = "ORU^R01",
                    PatientId = "P67890",
                    Status = "Pending",
                    Timestamp = DateTime.Now.AddMinutes(-5)
                }
            }
        };

        _dashboardServiceMock.Setup(x => x.GetDashboardDataAsync())
            .ReturnsAsync(dashboardData);

        // Act
        var component = RenderComponent<Index>();

        // Assert
        component.Markup.Should().Contain("ADT^A01");
        component.Markup.Should().Contain("P12345");
        component.Markup.Should().Contain("ORU^R01");
        component.Markup.Should().Contain("P67890");
    }

    [Fact]
    public void Index_WithNoRecentMessages_ShouldDisplay_NoMessagesText()
    {
        // Arrange
        var dashboardData = new DashboardData
        {
            RecentMessages = new List<RecentMessage>()
        };

        _dashboardServiceMock.Setup(x => x.GetDashboardDataAsync())
            .ReturnsAsync(dashboardData);

        // Act
        var component = RenderComponent<Index>();

        // Assert
        component.Markup.Should().Contain("No recent messages");
    }

    [Fact]
    public void Index_ShouldInclude_ThroughputChart()
    {
        // Arrange
        _dashboardServiceMock.Setup(x => x.GetDashboardDataAsync())
            .ReturnsAsync(new DashboardData());

        // Act
        var component = RenderComponent<Index>();

        // Assert
        // Check that ThroughputChart component is rendered
        component.Markup.Should().Contain("Message Throughput");
    }

    [Theory]
    [InlineData("processed", "bg-success")]
    [InlineData("pending", "bg-warning")]
    [InlineData("error", "bg-danger")]
    [InlineData("unknown", "bg-secondary")]
    public void GetStatusBadge_ShouldReturn_CorrectBadgeClass(string status, string expectedClass)
    {
        // Arrange
        var dashboardData = new DashboardData
        {
            RecentMessages = new List<RecentMessage>
            {
                new RecentMessage
                {
                    MessageType = "ADT^A01",
                    PatientId = "P12345",
                    Status = status,
                    Timestamp = DateTime.Now
                }
            }
        };

        _dashboardServiceMock.Setup(x => x.GetDashboardDataAsync())
            .ReturnsAsync(dashboardData);

        // Act
        var component = RenderComponent<Index>();

        // Assert
        component.Markup.Should().Contain(expectedClass);
    }

    [Fact]
    public void Index_ShouldDisplay_CorrectIcons()
    {
        // Arrange
        _dashboardServiceMock.Setup(x => x.GetDashboardDataAsync())
            .ReturnsAsync(new DashboardData());

        // Act
        var component = RenderComponent<Index>();

        // Assert
        component.Find(".bi-envelope-fill").Should().NotBeNull(); // Total Messages icon
        component.Find(".bi-check-circle-fill").Should().NotBeNull(); // Processed Today icon
        component.Find(".bi-clock-fill").Should().NotBeNull(); // Pending icon
        component.Find(".bi-exclamation-triangle-fill").Should().NotBeNull(); // Errors icon
    }

    [Fact]
    public void Index_ShouldLimit_RecentMessagesToFive()
    {
        // Arrange
        var messages = new List<RecentMessage>();
        for (int i = 0; i < 10; i++)
        {
            messages.Add(new RecentMessage
            {
                MessageType = $"ADT^A0{i}",
                PatientId = $"P{i:000}",
                Status = "Processed",
                Timestamp = DateTime.Now.AddMinutes(-i)
            });
        }

        var dashboardData = new DashboardData
        {
            RecentMessages = messages
        };

        _dashboardServiceMock.Setup(x => x.GetDashboardDataAsync())
            .ReturnsAsync(dashboardData);

        // Act
        var component = RenderComponent<Index>();

        // Assert
        var listItems = component.FindAll(".list-group-item");
        listItems.Should().HaveCount(5); // Should only show first 5 messages
    }
}

// Mock JSRuntime for testing
public class MockJSRuntime : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        return ValueTask.FromResult(default(TValue)!);
    }
}