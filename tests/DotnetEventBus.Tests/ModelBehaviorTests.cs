// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using Xunit;
using DotnetEventBus.Models;

namespace DotnetEventBus.Tests;

public class EventMessageModelTests
{
    [Fact]
    public void CreateRetry_ShouldIncrementProcessingAttemptsAndPreserveHeaders()
    {
        // Arrange
        var original = new EventMessage("MyApp.Events.OrderPlaced", "{\"orderId\":99}");
        original.CorrelationId = "corr-abc-123";
        original.Source = "order-service";
        original.ProcessingAttempts = 1;
        original.AddHeader("x-region", "eu-west");

        // Act
        var retry = original.CreateRetry();

        // Assert
        retry.MessageId.Should().NotBe(original.MessageId);
        retry.EventType.Should().Be(original.EventType);
        retry.CorrelationId.Should().Be("corr-abc-123");
        retry.Source.Should().Be("order-service");
        retry.ProcessingAttempts.Should().Be(2);
        retry.GetHeader("x-region").Should().Be("eu-west");
    }

    [Fact]
    public void AddHeader_ThenGetHeader_ShouldReturnStoredValue()
    {
        // Arrange
        var msg = new EventMessage("SomeEvent", "{}");

        // Act
        msg.AddHeader("trace-id", "t-001");

        // Assert
        msg.GetHeader("trace-id").Should().Be("t-001");
    }

    [Fact]
    public void GetHeader_WithUnknownKey_ShouldReturnNull()
    {
        // Arrange
        var msg = new EventMessage("SomeEvent", "{}");

        // Act & Assert
        msg.GetHeader("does-not-exist").Should().BeNull();
    }
}

public class SubscriptionModelTests
{
    [Fact]
    public void Disable_ThenEnable_ShouldToggleIsActiveCorrectly()
    {
        // Arrange
        var sub = new Subscription("OrderPlaced", new Action<object>(_ => { }), "OrderHandler");

        // Assert - starts active by default
        sub.IsActive.Should().BeTrue();

        // Act & Assert - disable
        sub.Disable();
        sub.IsActive.Should().BeFalse();

        // Act & Assert - re-enable
        sub.Enable();
        sub.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SetTimeout_WithZeroOrNegativeDuration_ShouldThrowArgumentException()
    {
        // Arrange
        var sub = new Subscription("OrderPlaced", new Action<object>(_ => { }), "OrderHandler");

        // Act
        var setZero = () => sub.SetTimeout(TimeSpan.Zero);
        var setNegative = () => sub.SetTimeout(TimeSpan.FromSeconds(-1));

        // Assert
        setZero.Should().Throw<ArgumentException>();
        setNegative.Should().Throw<ArgumentException>();
    }
}
