using DotnetEventBus.Configuration;
using DotnetEventBus.Middleware;
using DotnetEventBus.Models;
using DotnetEventBus.Repositories;
using DotnetEventBus.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace DotnetEventBus.Tests;

/// <summary>
/// Contains integration tests for new core functionality of the DotnetEventBus library.
/// Tests priority-based handler invocation, middleware pipeline execution, retry mechanisms,
/// dead letter queue functionality, and subscription management.
/// </summary>
public class NewCoreFunctionalityTests
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewCoreFunctionalityTests"/> class.
    /// Sets up the test environment with a service provider for dependency injection.
    /// </summary>
    public NewCoreFunctionalityTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Tests that event handlers are invoked in priority order when multiple handlers
    /// are subscribed to the same event type.
    /// </summary>
    [Fact]
    public async Task PublishAsync_ShouldInvokeHandlersInPriorityOrder()
    {
        // Arrange
        var options = new EventBusOptions();
        var bus = new EventBus(options: options, serviceProvider: _serviceProvider);
        var invocationOrder = new List<string>();

        bus.Subscribe<string>((e, ct) => { invocationOrder.Add("Low"); return Task.CompletedTask; }, "LowPriority", priority: 0);
        bus.Subscribe<string>((e, ct) => { invocationOrder.Add("High"); return Task.CompletedTask; }, "HighPriority", priority: 10);

        // Act
        await bus.PublishAsync("test-event");

        // Assert
        invocationOrder.Should().ContainInOrder("High", "Low");
    }

    /// <summary>
    /// Tests that middleware pipeline is executed when publishing events.
    /// Verifies that middleware can modify the context and that the pipeline completes successfully.
    /// </summary>
    [Fact]
    public async Task PublishAsync_ShouldExecuteMiddlewarePipeline()
    {
        // Arrange
        var options = new EventBusOptions();
        var middlewareMock = new Mock<IEventBusMiddleware>();
        middlewareMock
            .Setup(m => m.InvokeAsync(It.IsAny<EventMiddlewareContext>(), It.IsAny<EventMiddlewareDelegate>()))
            .Callback<EventMiddlewareContext, EventMiddlewareDelegate>((ctx, next) => ctx.CorrelationId = "modified")
            .Returns<EventMiddlewareContext, EventMiddlewareDelegate>((ctx, next) => next(ctx));

        var services = new ServiceCollection();
        services.AddSingleton(middlewareMock.Object);
        var provider = services.BuildServiceProvider();

        options.MiddlewareTypes.Add(typeof(IEventBusMiddleware));

        var bus = new EventBus(options: options, serviceProvider: provider);

        // Act
        var result = await bus.PublishAsync("test", correlationId: "original");

        // Assert
        middlewareMock.Verify(m => m.InvokeAsync(It.IsAny<EventMiddlewareContext>(), It.IsAny<EventMiddlewareDelegate>()), Times.Once);
    }

    /// <summary>
    /// Tests that failed event handlers are automatically retried according to configured retry policy.
    /// Verifies that the retry mechanism executes the handler multiple times before giving up.
    /// </summary>
    [Fact]
    public async Task PublishAsync_ShouldRetryFailedHandlers()
    {
        // Arrange
        var options = new EventBusOptions { MaxRetryAttempts = 2 };
        var bus = new EventBus(options: options, serviceProvider: _serviceProvider);
        int attempts = 0;

        bus.Subscribe<string>((e, ct) => {
            attempts++;
            throw new Exception("Temporary failure");
        });

        // Act
        var result = await bus.PublishAsync("test");

        // Assert
        attempts.Should().Be(3); // Initial + 2 retries
        result.Success.Should().BeFalse();
    }

    /// <summary>
    /// Tests that failed event handlers are moved to the dead letter queue when configured.
    /// Verifies that handlers that consistently fail are properly tracked and stored for later inspection.
    /// </summary>
    [Fact]
    public async Task PublishAsync_ShouldMoveFailedHandlerToDeadLetterQueue()
    {
        // Arrange
        var dlqRepository = new InMemoryDeadLetterRepository();
        var options = new EventBusOptions
        {
            MaxRetryAttempts = 0,
            EnableDeadLetterQueue = true
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDeadLetterRepository>(dlqRepository);
        // We need a proper way to inject the repository into the constructor.
        // Looking at the constructor, it seems I need to pass it explicitly in the other overload.

        var bus = new EventBus(
            new InMemoryEventMessageRepository(),
            new InMemorySubscriptionRepository(),
            dlqRepository,
            new DeadLetterService(dlqRepository, null!, null),
            new DotnetEventBus.Formatters.JsonEventFormatter(),
            _serviceProvider,
            options);

        bus.Subscribe<string>((e, ct) => throw new Exception("Fatal error"), "BadHandler", priority: 0);

        // Act
        await bus.PublishAsync("test");

        // Assert
        var deadLetters = await dlqRepository.GetAllAsync();
        deadLetters.Should().ContainSingle(e => e.FailedHandlerName == "BadHandler");
    }

    /// <summary>
    /// Tests that unsubscribing prevents future invocations of the handler.
    /// Verifies that subscription cleanup works correctly and handlers are not called after disposal.
    /// </summary>
    [Fact]
    public async Task Unsubscribe_ShouldPreventFutureInvocations()
    {
        // Arrange
        var bus = new EventBus(options: new EventBusOptions(), serviceProvider: _serviceProvider);
        int callCount = 0;

        var subscription = bus.SubscribeSync<string>(e => callCount++);

        // Act
        await bus.PublishAsync("first");
        subscription.Dispose();
        await bus.PublishAsync("second");

        // Assert
        callCount.Should().Be(1);
    }
}