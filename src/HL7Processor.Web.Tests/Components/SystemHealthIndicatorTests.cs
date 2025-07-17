using Bunit;
using HL7Processor.Web.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;

namespace HL7Processor.Web.Tests.Components;

public class SystemHealthIndicatorTests : TestContext
{
    public SystemHealthIndicatorTests()
    {
        // Configure test services
        Services.AddSingleton<IJSRuntime>(new MockJSRuntime());
    }

    [Fact]
    public void SystemHealthIndicator_ShouldRender_WithDefaultHealthyState()
    {
        // Act
        var component = RenderComponent<SystemHealthIndicator>();

        // Assert
        component.Find(".badge").Should().NotBeNull();
        component.Find(".bi-question-circle-fill").Should().NotBeNull();
        component.Markup.Should().Contain("Unknown");
    }

    [Fact]
    public void GetStatusBadgeClass_WithHealthyStatus_ShouldReturnSuccessBadge()
    {
        // Arrange
        var component = RenderComponent<SystemHealthIndicator>();
        
        // Act - Using reflection to test private method behavior through rendered output
        var health = new SystemHealthIndicator.SystemHealth
        {
            OverallStatus = SystemHealthIndicator.SystemStatus.Healthy
        };

        // Update component state through instance method
        var instance = component.Instance;
        var healthField = typeof(SystemHealthIndicator).GetField("systemHealth", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        healthField?.SetValue(instance, health);
        
        component.Render();

        // Assert
        component.Find(".bg-success").Should().NotBeNull();
        component.Find(".bi-check-circle-fill").Should().NotBeNull();
        component.Markup.Should().Contain("Healthy");
    }

    [Fact]
    public void GetStatusBadgeClass_WithWarningStatus_ShouldReturnWarningBadge()
    {
        // Arrange
        var component = RenderComponent<SystemHealthIndicator>();
        
        // Act
        var health = new SystemHealthIndicator.SystemHealth
        {
            OverallStatus = SystemHealthIndicator.SystemStatus.Warning
        };

        var instance = component.Instance;
        var healthField = typeof(SystemHealthIndicator).GetField("systemHealth", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        healthField?.SetValue(instance, health);
        
        component.Render();

        // Assert
        component.Find(".bg-warning").Should().NotBeNull();
        component.Find(".bi-exclamation-triangle-fill").Should().NotBeNull();
        component.Markup.Should().Contain("Warning");
    }

    [Fact]
    public void GetStatusBadgeClass_WithCriticalStatus_ShouldReturnDangerBadge()
    {
        // Arrange
        var component = RenderComponent<SystemHealthIndicator>();
        
        // Act
        var health = new SystemHealthIndicator.SystemHealth
        {
            OverallStatus = SystemHealthIndicator.SystemStatus.Critical
        };

        var instance = component.Instance;
        var healthField = typeof(SystemHealthIndicator).GetField("systemHealth", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        healthField?.SetValue(instance, health);
        
        component.Render();

        // Assert
        component.Find(".bg-danger").Should().NotBeNull();
        component.Find(".bi-x-circle-fill").Should().NotBeNull();
        component.Markup.Should().Contain("Critical");
    }

    [Fact]
    public void SystemHealthIndicator_ShouldDisplayLastUpdatedTime()
    {
        // Arrange
        var component = RenderComponent<SystemHealthIndicator>();
        
        // Act
        var lastUpdated = DateTime.Now;
        var lastUpdatedField = typeof(SystemHealthIndicator).GetField("lastUpdated", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        lastUpdatedField?.SetValue(component.Instance, lastUpdated);
        
        component.Render();

        // Assert
        component.Markup.Should().Contain("Updated");
        component.Markup.Should().Contain(lastUpdated.ToString("HH:mm:ss"));
    }

    [Fact]
    public void SystemHealthIndicator_WithoutLastUpdated_ShouldNotDisplayTime()
    {
        // Arrange & Act
        var component = RenderComponent<SystemHealthIndicator>();

        // Assert
        component.Markup.Should().NotContain("Updated");
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