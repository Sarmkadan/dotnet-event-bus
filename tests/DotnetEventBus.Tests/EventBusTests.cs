#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Xunit;
using DotnetEventBus.Services;
using DotnetEventBus.Configuration;
using DotnetEventBus.Models;
using DotnetEventBus.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetEventBus.Tests;

/// <summary>
/// Test event class for testing purposes.
/// </summary>
public sealed class TestEvent
{
    public string Data { get; set; } = string.Empty;
    public int Value { get; set; }
}

/// <summary>
/// Test handler implementation.
/// </summary>
public sealed class TestEventHandler : EventHandlerBase<TestEvent>
{
    public int CallCount { get; set; }

    public override async Task Handle(TestEvent @event, CancellationToken cancellationToken = default)
    {
        CallCount++;
        await Task.Delay(10, cancellationToken);
    }
}

/// <summary>
/// Unit tests for the event bus.
/// </summary>
public sealed class EventBusTests
{
    private readonly ServiceCollection _services;
    private readonly IEventBus _eventBus;

    public EventBusTests()
    {
        _services = new ServiceCollection();
        _services.AddEventBus();
        var provider = _services.BuildServiceProvider();
        _eventBus = provider.GetRequiredService<IEventBus>();
    }

    [Fact]
    public async Task PublishAsync_WithValidEvent_ShouldInvokeSubscribedHandlers()
    {
        // Arrange
        var @event = new TestEvent { Data = "test", Value = 42 };
        var callCount = 0;

        _eventBus.Subscribe<TestEvent>(
            async (e, ct) =>
            {
                callCount++;
                await Task.CompletedTask;
            },
            handlerName: "TestHandler"
        );

        // Act
        var result = await _eventBus.PublishAsync(@event);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.HandlersInvoked);
        Assert.Equal(0, result.FailedHandlers);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task PublishAsync_WithMultipleHandlers_ShouldInvokeAllHandlers()
    {
        // Arrange
        var @event = new TestEvent { Data = "test", Value = 42 };
        var callCount1 = 0;
        var callCount2 = 0;

        _eventBus.Subscribe<TestEvent>(
            async (e, ct) =>
            {
                callCount1++;
                await Task.CompletedTask;
            }
        );

        _eventBus.Subscribe<TestEvent>(
            async (e, ct) =>
            {
                callCount2++;
                await Task.CompletedTask;
            }
        );

        // Act
        var result = await _eventBus.PublishAsync(@event);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.HandlersInvoked);
        Assert.Equal(1, callCount1);
        Assert.Equal(1, callCount2);
    }

    [Fact]
    public async Task Subscribe_WithDelegate_ShouldReturnDisposable()
    {
        // Arrange
        var @event = new TestEvent { Data = "test", Value = 42 };
        var callCount = 0;

        // Act
        var subscription = _eventBus.Subscribe<TestEvent>(
            async (e, ct) =>
            {
                callCount++;
                await Task.CompletedTask;
            },
            handlerName: "DisposableHandler"
        );

        await _eventBus.PublishAsync(@event);
        subscription.Dispose();
        await _eventBus.PublishAsync(@event);

        // Assert
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task SubscribeSync_WithSynchronousHandler_ShouldWork()
    {
        // Arrange
        var @event = new TestEvent { Data = "test", Value = 42 };
        var callCount = 0;

        // Act
        _eventBus.SubscribeSync<TestEvent>(
            e =>
            {
                callCount++;
            },
            handlerName: "SyncHandler"
        );

        var result = await _eventBus.PublishAsync(@event);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task GetSubscriptions_WithRegisteredHandlers_ShouldReturnHandlerNames()
    {
        // Arrange
        _eventBus.Subscribe<TestEvent>(
            async (e, ct) => await Task.CompletedTask,
            handlerName: "Handler1"
        );

        _eventBus.Subscribe<TestEvent>(
            async (e, ct) => await Task.CompletedTask,
            handlerName: "Handler2"
        );

        // Act
        var subscriptions = await _eventBus.GetSubscriptionsAsync(
            typeof(TestEvent).FullName ?? typeof(TestEvent).Name
        );

        // Assert
        var subsList = subscriptions.ToList();
        Assert.Equal(2, subsList.Count);
        Assert.Contains("Handler1", subsList);
        Assert.Contains("Handler2", subsList);
    }

    [Fact]
    public async Task PublishAsync_WithPriority_ShouldExecuteInOrder()
    {
        // Arrange
        var executionOrder = new List<string>();

        _eventBus.Subscribe<TestEvent>(
            async (e, ct) =>
            {
                executionOrder.Add("Low");
                await Task.CompletedTask;
            },
            handlerName: "LowPriority",
            priority: (int)HandlerPriority.Low
        );

        _eventBus.Subscribe<TestEvent>(
            async (e, ct) =>
            {
                executionOrder.Add("High");
                await Task.CompletedTask;
            },
            handlerName: "HighPriority",
            priority: (int)HandlerPriority.High
        );

        // Act
        await _eventBus.PublishAsync(new TestEvent { Data = "test", Value = 1 });

        // Assert
        Assert.Equal("High", executionOrder[0]);
        Assert.Equal("Low", executionOrder[1]);
    }

    [Fact]
    public async Task ClearSubscriptions_ShouldRemoveAllSubscriptions()
    {
        // Arrange
        _eventBus.Subscribe<TestEvent>(async (e, ct) => await Task.CompletedTask);
        _eventBus.Subscribe<TestEvent>(async (e, ct) => await Task.CompletedTask);

        // Act
        await _eventBus.ClearSubscriptionsAsync();
        var result = await _eventBus.PublishAsync(new TestEvent { Data = "test", Value = 1 });

        // Assert
        Assert.Equal(0, result.HandlersInvoked);
    }

    [Fact]
    public void GetOptions_ShouldReturnCurrentOptions()
    {
        // Act
        var options = _eventBus.GetOptions();

        // Assert
        Assert.NotNull(options);
        Assert.True(options.AllowParallelHandling);
        Assert.Equal(3, options.MaxRetryAttempts);
    }
}
