using System;
using System.Threading.Tasks;
using DotnetEventBus.Models;
using Xunit;

namespace DotnetEventBus.Tests;

public sealed class SubscriptionTests
{
    [Fact]
    public void Constructor_InitializesAllProperties()
    {
        // Arrange
        var handler = new Action<string>(s => { });
        var subscription = new Subscription(
            eventType: "TestEvent",
            handler: handler,
            handlerName: "TestHandler",
            priority: 5);

        // Act & Assert
        Assert.False(string.IsNullOrWhiteSpace(subscription.Id));
        Assert.Equal("TestEvent", subscription.EventType);
        Assert.Same(handler, subscription.Handler);
        Assert.Equal("TestHandler", subscription.HandlerName);
        Assert.True(subscription.IsActive);
        Assert.Equal(5, subscription.Priority);
        Assert.False(subscription.IsAsync);
        Assert.True(subscription.AllowConcurrent);
        Assert.True(subscription.SendToDeadLetterOnFailure);
        Assert.Null(subscription.Timeout);
        Assert.True((DateTime.UtcNow - subscription.CreatedAtUtc).TotalSeconds < 5);
    }

    [Fact]
    public void Constructor_DetectsAsyncDelegate()
    {
        // Arrange
        Func<Task> asyncHandler = async () => await Task.CompletedTask;

        // Act
        var subscription = new Subscription(
            eventType: "AsyncEvent",
            handler: asyncHandler,
            handlerName: "AsyncHandler");

        // Assert
        Assert.True(subscription.IsAsync);
    }

    [Fact]
    public void DisableAndEnable_WorkCorrectly()
    {
        // Arrange
        var subscription = new Subscription(
            eventType: "Event",
            handler: new Action(() => { }),
            handlerName: "Handler");

        // Act
        subscription.Disable();

        // Assert
        Assert.False(subscription.IsActive);

        // Act
        subscription.Enable();

        // Assert
        Assert.True(subscription.IsActive);
    }

    [Fact]
    public void SetTimeout_ValidValue_SetsTimeout()
    {
        // Arrange
        var subscription = new Subscription(
            eventType: "Event",
            handler: new Action(() => { }),
            handlerName: "Handler");

        var timeout = TimeSpan.FromSeconds(10);

        // Act
        subscription.SetTimeout(timeout);

        // Assert
        Assert.Equal(timeout, subscription.Timeout);
    }

    [Fact]
    public void SetTimeout_InvalidValue_ThrowsArgumentException()
    {
        // Arrange
        var subscription = new Subscription(
            eventType: "Event",
            handler: new Action(() => { }),
            handlerName: "Handler");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => subscription.SetTimeout(TimeSpan.Zero));
        Assert.Throws<ArgumentException>(() => subscription.SetTimeout(TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        // Arrange
        var handler = new Action(() => { });

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Subscription(
            eventType: null!,
            handler: handler,
            handlerName: "Handler"));

        Assert.Throws<ArgumentNullException>(() => new Subscription(
            eventType: "Event",
            handler: null!,
            handlerName: "Handler"));

        Assert.Throws<ArgumentNullException>(() => new Subscription(
            eventType: "Event",
            handler: handler,
            handlerName: null!));
    }

    [Fact]
    public void Constructor_ZeroPriority_IsAccepted()
    {
        // Arrange
        var subscription = new Subscription(
            eventType: "Event",
            handler: new Action(() => { }),
            handlerName: "Handler",
            priority: 0);

        // Assert
        Assert.Equal(0, subscription.Priority);
    }

    [Fact]
    public void Constructor_NegativePriority_IsAccepted()
    {
        // Arrange
        var subscription = new Subscription(
            eventType: "Event",
            handler: new Action(() => { }),
            handlerName: "Handler",
            priority: -10);

        // Assert
        Assert.Equal(-10, subscription.Priority);
    }
}
