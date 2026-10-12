#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetEventBus.Configuration;
using DotnetEventBus.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotnetEventBus.Tests;

public sealed class InMemoryBusTests
{
    private sealed record OrderPlaced(string OrderId);

    private sealed record PaymentReceived(decimal Amount);

    private static InMemoryBus CreateBus() => new(NullLogger<InMemoryBus>.Instance, new EventBusOptions());

    [Fact]
    public void ToString_EmptyBus_ReportsNoEventTypes()
    {
        // Arrange
        using var bus = CreateBus();

        // Act
        var text = bus.ToString();

        // Assert
        Assert.Contains("InMemoryBus", text);
        Assert.Contains("EventTypes = 0", text);
        Assert.Contains("Subscribers = []", text);
    }

    [Fact]
    public void ToString_ContainsSubscriberCountPerEventType()
    {
        // Arrange
        using var bus = CreateBus();
        bus.Subscribe<OrderPlaced>((_, _) => Task.CompletedTask, "First");
        bus.Subscribe<OrderPlaced>((_, _) => Task.CompletedTask, "Second");
        bus.Subscribe<PaymentReceived>((_, _) => Task.CompletedTask, "Third");

        // Act
        var text = bus.ToString();

        // Assert
        Assert.Contains("EventTypes = 2", text);
        Assert.Contains($"{typeof(OrderPlaced).FullName} = 2", text);
        Assert.Contains($"{typeof(PaymentReceived).FullName} = 1", text);
    }

    [Fact]
    public void ToString_AfterUnsubscribe_ReflectsRemainingSubscribers()
    {
        // Arrange
        using var bus = CreateBus();
        using var subscription = bus.Subscribe<OrderPlaced>((_, _) => Task.CompletedTask, "First");
        bus.Subscribe<OrderPlaced>((_, _) => Task.CompletedTask, "Second");

        // Act
        subscription.Dispose();
        var text = bus.ToString();

        // Assert
        Assert.Contains($"{typeof(OrderPlaced).FullName} = 1", text);
    }
}
