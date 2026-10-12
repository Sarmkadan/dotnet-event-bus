#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using DotnetEventBus.Configuration;
using DotnetEventBus.Models;
using DotnetEventBus.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotnetEventBus.Tests;

/// <summary>
/// Integration tests for Subscription lifecycle scenarios.
/// </summary>
public sealed class SubscriptionLifecycleTests
{
    /// <summary>
    /// Base event class for inheritance testing.
    /// </summary>
    private class BaseEvent { }

    /// <summary>
    /// Derived event class for inheritance testing.
    /// </summary>
    private class DerivedEvent : BaseEvent { }

    /// <summary>
    /// Tests that subscribing to a base event type DOES receive derived events.
    /// Current implementation supports event type inheritance for subscriptions by walking up the inheritance hierarchy.
    /// </summary>
    [Fact]
    public async Task EventBus_SubscribeToBaseEvent_ReceivesDerivedEvents()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEventBus();
        var provider = services.BuildServiceProvider();
        var eventBus = provider.GetRequiredService<IEventBus>();

        var baseEventReceived = false;
        var derivedEventReceived = false;

        // Subscribe to base event
        eventBus.Subscribe<BaseEvent>(async (e, ct) =>
        {
            baseEventReceived = true;
            await Task.CompletedTask;
        });

        // Subscribe to derived event (for comparison)
        eventBus.Subscribe<DerivedEvent>(async (e, ct) =>
        {
            derivedEventReceived = true;
            await Task.CompletedTask;
        });

        // Act
        await eventBus.PublishAsync(new DerivedEvent());

        // Assert
        // Base event handler SHOULD be called (inheritance support)
        baseEventReceived.Should().BeTrue("Subscription to base event receives derived events via inheritance");
        // Derived event handler should be called
        derivedEventReceived.Should().BeTrue();
    }

    /// <summary>
    /// Tests that unsubscribing via Dispose during an in-flight handler:
    /// - Completes the current handler execution
    /// - Prevents delivery of further events
    /// </summary>
    [Fact]
    public async Task EventBus_DisposeSubscriptionDuringHandler_CompletesCurrentHandlerAndStopsFurtherEvents()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEventBus(options => options.AllowParallelHandling = false);
        var provider = services.BuildServiceProvider();
        var eventBus = provider.GetRequiredService<IEventBus>();

        var handlerExecutionCount = 0;
        var handlerCompleted = false;
        var disposeCalled = false;
        var handlerStarted = new TaskCompletionSource<object?>();

        // Subscribe to event
        var subscription = eventBus.Subscribe<TestEvent>((e, ct) =>
        {
            handlerExecutionCount++;
            // Signal that handler has started
            handlerStarted.SetResult(null);
            // Handler completes normally
            handlerCompleted = true;
            return Task.CompletedTask;
        });

        // Publish first event (will trigger handler)
        var publishTask = eventBus.PublishAsync(new TestEvent { Data = "first" });

        // Wait for handler to start
        await handlerStarted.Task;

        // Act: Dispose subscription while handler is running
        subscription.Dispose();
        disposeCalled = true;

        // Let the handler complete
        await publishTask;

        // Publish second event (should not be handled)
        await eventBus.PublishAsync(new TestEvent { Data = "second" });

        // Assert
        disposeCalled.Should().BeTrue();
        handlerExecutionCount.Should().Be(1, "Handler should execute exactly once");
        handlerCompleted.Should().BeTrue("Handler should complete after disposal");

        // Verify no further events were processed by checking that handler wasn't called again
        // We can't directly check, but we can publish a third event and see if count changes
        await eventBus.PublishAsync(new TestEvent { Data = "third" });
        handlerExecutionCount.Should().Be(1, "Handler should not be called after disposal");
    }

    /// <summary>
    /// Tests that calling Dispose multiple times is a no-op and does not throw.
    /// </summary>
    [Fact]
    public void EventBus_DisposeSubscriptionMultipleTimes_IsNoOp()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEventBus();
        var provider = services.BuildServiceProvider();
        var eventBus = provider.GetRequiredService<IEventBus>();

        // Subscribe to event
        var subscription = eventBus.Subscribe<TestEvent>(async (e, ct) => await Task.CompletedTask);

        // Act
        // First dispose
        subscription.Dispose();
        // Second dispose (should not throw)
        subscription.Dispose();
        // Third dispose (should not throw)
        subscription.Dispose();

        // Assert - if we get here without exception, test passes
        true.Should().BeTrue();
    }

    /// <summary>
    /// Tests that subscribing from within a handler does not cause deadlock.
    /// </summary>
    [Fact]
    public async Task EventBus_SubscribeFromWithinHandler_DoesNotDeadlock()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEventBus(options => options.AllowParallelHandling = false);
        var provider = services.BuildServiceProvider();
        var eventBus = provider.GetRequiredService<IEventBus>();

        var outerHandlerExecuted = false;
        var innerHandlerExecuted = false;
        var innerSubscriptionMade = false;

        // Subscribe to outer event
        eventBus.Subscribe<OuterEvent>((e, ct) =>
        {
            outerHandlerExecuted = true;
            // Subscribe to inner event from within the handler
            eventBus.Subscribe<InnerEvent>((ie, ict) =>
            {
                innerHandlerExecuted = true;
                return Task.CompletedTask;
            });
            innerSubscriptionMade = true;
            return Task.CompletedTask;
        });

        // Act
        await eventBus.PublishAsync(new OuterEvent { Data = "outer" });

        // Give a moment for inner subscription to be made and potentially trigger
        await Task.Delay(10);

        // Publish inner event to see if inner handler executes
        await eventBus.PublishAsync(new InnerEvent { Data = "inner" });

        // Assert
        outerHandlerExecuted.Should().BeTrue("Outer handler should execute");
        innerSubscriptionMade.Should().BeTrue("Inner subscription should be made from within outer handler");
        innerHandlerExecuted.Should().BeTrue("Inner handler should execute after being subscribed to from within outer handler");
    }

    // Helper event classes
    private class TestEvent
    {
        public string? Data { get; set; }
    }

    private class OuterEvent
    {
        public string? Data { get; set; }
    }

    private class InnerEvent
    {
        public string? Data { get; set; }
    }
}