using DotnetEventBus.Configuration;
using DotnetEventBus.Middleware;
using DotnetEventBus.Models;
using DotnetEventBus.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace DotnetEventBus.Tests;

public class NewCoreFunctionalityTests
{
    private readonly IServiceProvider _serviceProvider;

    public NewCoreFunctionalityTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeHandlersInPriorityOrder()
    {
        // Arrange
        var options = new EventBusOptions();
        var bus = new EventBus(options: options, serviceProvider: _serviceProvider);
        var invocationOrder = new List<string>();

        bus.Subscribe<string>(e => { invocationOrder.Add("Low"); return Task.CompletedTask; }, "LowPriority", priority: 0);
        bus.Subscribe<string>(e => { invocationOrder.Add("High"); return Task.CompletedTask; }, "HighPriority", priority: 10);

        // Act
        await bus.PublishAsync("test-event");

        // Assert
        invocationOrder.Should().ContainInOrder("High", "Low");
    }

    [Fact]
    public async Task PublishAsync_ShouldExecuteMiddlewarePipeline()
    {
        // Arrange
        var options = new EventBusOptions();
        var middlewareMock = new Mock<IEventBusMiddleware>();
        middlewareMock
            .Setup(m => m.InvokeAsync(It.IsAny<EventContext>(), It.IsAny<EventMiddlewareDelegate>()))
            .Callback<EventContext, EventMiddlewareDelegate>((ctx, next) => ctx.CorrelationId = "modified")
            .Returns<EventContext, EventMiddlewareDelegate>((ctx, next) => next(ctx));

        var services = new ServiceCollection();
        services.AddSingleton(middlewareMock.Object);
        var provider = services.BuildServiceProvider();
        
        options.MiddlewareTypes.Add(typeof(IEventBusMiddleware));
        
        var bus = new EventBus(options: options, serviceProvider: provider);

        // Act
        var result = await bus.PublishAsync("test", correlationId: "original");

        // Assert
        middlewareMock.Verify(m => m.InvokeAsync(It.IsAny<EventContext>(), It.IsAny<EventMiddlewareDelegate>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldRetryFailedHandlers()
    {
        // Arrange
        var options = new EventBusOptions { MaxRetryAttempts = 2 };
        var bus = new EventBus(options: options, serviceProvider: _serviceProvider);
        int attempts = 0;

        bus.Subscribe<string>(e => {
            attempts++;
            throw new Exception("Temporary failure");
        });

        // Act
        var result = await bus.PublishAsync("test");

        // Assert
        attempts.Should().Be(3); // Initial + 2 retries
        result.Success.Should().BeFalse();
    }

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

        bus.Subscribe<string>(e => throw new Exception("Fatal error"), "BadHandler", priority: 0);

        // Act
        await bus.PublishAsync("test");

        // Assert
        var deadLetters = await dlqRepository.GetAllAsync();
        deadLetters.Should().ContainSingle(e => e.FailedHandlerName == "BadHandler");
    }

    [Fact]
    public async Task Unsubscribe_ShouldPreventFutureInvocations()
    {
        // Arrange
        var bus = new EventBus(options: new EventBusOptions(), serviceProvider: _serviceProvider);
        int callCount = 0;
        
        var subscription = bus.Subscribe<string>(e => callCount++);

        // Act
        await bus.PublishAsync("first");
        subscription.Dispose();
        await bus.PublishAsync("second");

        // Assert
        callCount.Should().Be(1);
    }
}
