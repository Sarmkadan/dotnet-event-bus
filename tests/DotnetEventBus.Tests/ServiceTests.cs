// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Xunit;
using DotnetEventBus.Services;
using DotnetEventBus.Configuration;
using DotnetEventBus.Repositories;
using DotnetEventBus.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetEventBus.Tests;

/// <summary>
/// Unit tests for DeadLetterService.
/// </summary>
public class DeadLetterServiceTests
{
    private readonly IDeadLetterRepository _repository = new InMemoryDeadLetterRepository();
    private readonly IDeadLetterService _service;
    private readonly IEventBus _eventBus;

    public DeadLetterServiceTests()
    {
        var services = new ServiceCollection();
        services.AddEventBus();
        var provider = services.BuildServiceProvider();
        _eventBus = provider.GetRequiredService<IEventBus>();
        _service = new DeadLetterService(_repository, _eventBus);
    }

    [Fact]
    public async Task GetPendingEntriesAsync_ShouldReturnPendingEntries()
    {
        // Arrange
        var msg = new EventMessage("Event1", "payload");
        var entry = new DeadLetterEntry(msg, "Handler1", new Exception("Test"));
        await _repository.AddAsync(entry);

        // Act
        var pending = await _service.GetPendingEntriesAsync();

        // Assert
        Assert.Single(pending);
    }

    [Fact]
    public async Task MarkAsReviewedAsync_ShouldUpdateStatus()
    {
        // Arrange
        var msg = new EventMessage("Event1", "payload");
        var entry = new DeadLetterEntry(msg, "Handler1", new Exception("Test"));
        await _repository.AddAsync(entry);

        // Act
        await _service.MarkAsReviewedAsync(entry.Id, "Reviewed for processing");
        var updated = await _repository.GetByIdAsync(entry.Id);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(DeadLetterStatus.ReviewedNotProcessed, updated.Status);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnAccurateStats()
    {
        // Arrange
        var msg1 = new EventMessage("Event1", "payload");
        var msg2 = new EventMessage("Event2", "payload");

        var entry1 = new DeadLetterEntry(msg1, "Handler1", new Exception("Test"));
        var entry2 = new DeadLetterEntry(msg2, "Handler2", new Exception("Test"));
        entry2.MarkAsReprocessed();

        await _repository.AddAsync(entry1);
        await _repository.AddAsync(entry2);

        // Act
        var stats = await _service.GetStatisticsAsync();

        // Assert
        Assert.Equal(2, stats.TotalEntries);
        Assert.Equal(1, stats.PendingEntries);
        Assert.Equal(1, stats.ReprocessedEntries);
        Assert.Equal(2, stats.EntriesByEventType.Count);
    }

    [Fact]
    public async Task ArchiveOldEntriesAsync_ShouldArchiveOldEntries()
    {
        // Arrange
        var msg = new EventMessage("Event1", "payload");
        var entry = new DeadLetterEntry(msg, "Handler1", new Exception("Test"));
        entry.CreatedAtUtc = DateTime.UtcNow.AddDays(-30);
        await _repository.AddAsync(entry);

        // Act
        var archivedCount = await _service.ArchiveOldEntriesAsync(TimeSpan.FromDays(7));

        // Assert
        Assert.Equal(1, archivedCount);
    }
}

/// <summary>
/// Unit tests for SubscriptionManager.
/// </summary>
public class SubscriptionManagerTests
{
    private readonly ISubscriptionRepository _repository = new InMemorySubscriptionRepository();
    private readonly ISubscriptionManager _manager;

    public SubscriptionManagerTests()
    {
        _manager = new SubscriptionManager(_repository);
    }

    [Fact]
    public async Task GetSubscriptionsAsync_ShouldReturnSubscriptionInfo()
    {
        // Arrange
        var sub1 = new Subscription("Event1", new Action<object>(o => { }), "Handler1");
        var sub2 = new Subscription("Event1", new Action<object>(o => { }), "Handler2");

        await _repository.AddAsync(sub1);
        await _repository.AddAsync(sub2);

        // Act
        var subs = await _manager.GetSubscriptionsAsync("Event1");

        // Assert
        Assert.Equal(2, subs.Count());
        Assert.All(subs, s => Assert.Equal("Event1", s.EventType));
    }

    [Fact]
    public async Task DisableHandlerAsync_ShouldDisableAllHandlerSubscriptions()
    {
        // Arrange
        var sub1 = new Subscription("Event1", new Action<object>(o => { }), "MyHandler");
        var sub2 = new Subscription("Event2", new Action<object>(o => { }), "MyHandler");

        await _repository.AddAsync(sub1);
        await _repository.AddAsync(sub2);

        // Act
        await _manager.DisableHandlerAsync("MyHandler");

        var allSubs = await _repository.GetAllAsync();

        // Assert
        Assert.All(allSubs, s => Assert.False(s.IsActive));
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnAccurateStats()
    {
        // Arrange
        var sub1 = new Subscription("Event1", new Action<object>(o => { }), "Handler1");
        var sub2 = new Subscription("Event1", new Action<object>(o => { }), "Handler2");
        var sub3 = new Subscription("Event2", new Action<object>(o => { }), "Handler3");

        await _repository.AddAsync(sub1);
        await _repository.AddAsync(sub2);
        await _repository.AddAsync(sub3);

        // Act
        var stats = await _manager.GetStatisticsAsync();

        // Assert
        Assert.Equal(3, stats.TotalSubscriptions);
        Assert.Equal(3, stats.UniqueHandlers);
        Assert.Equal(2, stats.UniqueEventTypes);
    }
}

/// <summary>
/// Unit tests for HandlerInvoker.
/// </summary>
public class HandlerInvokerTests
{
    [Fact]
    public async Task InvokeAsync_WithValidHandler_ShouldInvoke()
    {
        // Arrange
        var invoker = new HandlerInvoker();
        var handler = new TestEventHandler();
        var @event = new TestEvent { Data = "test", Value = 42 };

        // Act
        await invoker.InvokeAsync(handler, @event);

        // Assert
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public void CanHandle_WithValidHandlerAndEventType_ShouldReturnTrue()
    {
        // Arrange
        var invoker = new HandlerInvoker();
        var handler = new TestEventHandler();

        // Act
        var canHandle = invoker.CanHandle(handler, typeof(TestEvent));

        // Assert
        Assert.True(canHandle);
    }

    [Fact]
    public void CanHandle_WithInvalidEventType_ShouldReturnFalse()
    {
        // Arrange
        var invoker = new HandlerInvoker();
        var handler = new TestEventHandler();

        // Act
        var canHandle = invoker.CanHandle(handler, typeof(string));

        // Assert
        Assert.False(canHandle);
    }

    [Fact]
    public void GetSupportedEventTypes_ShouldReturnHandlerEventTypes()
    {
        // Arrange
        var invoker = new HandlerInvoker();
        var handler = new TestEventHandler();

        // Act
        var types = invoker.GetSupportedEventTypes(handler);

        // Assert
        Assert.Single(types);
        Assert.Contains(typeof(TestEvent), types);
    }
}

/// <summary>
/// Unit tests for configuration and options.
/// </summary>
public class ConfigurationTests
{
    [Fact]
    public void EventBusOptions_Validate_ShouldThrowOnInvalidOptions()
    {
        // Arrange
        var options = new DotnetEventBus.Configuration.EventBusOptions
        {
            DefaultHandlerTimeout = TimeSpan.Zero // Invalid
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void EventBusOptions_CalculateRetryDelay_ShouldUseExponentialBackoff()
    {
        // Arrange
        var options = new DotnetEventBus.Configuration.EventBusOptions
        {
            RetryDelay = TimeSpan.FromMilliseconds(100),
            RetryDelayMultiplier = 2.0,
            MaxRetryDelay = TimeSpan.FromSeconds(10)
        };

        // Act
        var delay0 = options.CalculateRetryDelay(0);
        var delay1 = options.CalculateRetryDelay(1);
        var delay2 = options.CalculateRetryDelay(2);

        // Assert
        Assert.Equal(100, delay0.TotalMilliseconds);
        Assert.Equal(200, delay1.TotalMilliseconds);
        Assert.Equal(400, delay2.TotalMilliseconds);
    }

    [Fact]
    public void EventBusOptions_Clone_ShouldCreateIndependentCopy()
    {
        // Arrange
        var original = new DotnetEventBus.Configuration.EventBusOptions
        {
            MaxRetryAttempts = 5
        };

        // Act
        var clone = original.Clone();
        clone.MaxRetryAttempts = 10;

        // Assert
        Assert.Equal(5, original.MaxRetryAttempts);
        Assert.Equal(10, clone.MaxRetryAttempts);
    }
}
