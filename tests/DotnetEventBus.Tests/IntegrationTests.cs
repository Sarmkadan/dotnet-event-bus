#nullable enable

using FluentAssertions;
using Moq;
using Xunit;
using DotnetEventBus.Services;
using DotnetEventBus.Configuration;
using DotnetEventBus.Models;
using DotnetEventBus.Repositories;
using DotnetEventBus.Integration;
using DotnetEventBus.Advanced;
using DotnetEventBus.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Tests;

/// <summary>
/// Integration tests for end-to-end event bus workflows.
/// </summary>
public sealed class EventBusIntegrationTests
{
    [Fact]
    public async Task EventBus_PublishAndSubscribe_FullWorkflow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEventBus(options =>
        {
            options.EnableDeadLetterQueue = true;
            options.AllowParallelHandling = false;
            options.MaxRetryAttempts = 1;
        });
        var provider = services.BuildServiceProvider();
        var eventBus = provider.GetRequiredService<IEventBus>();

        var handlerCallCount = 0;
        var capturedEvent = (TestEvent?)null;

        // Act
        eventBus.Subscribe<TestEvent>(
            async (e, ct) =>
            {
                handlerCallCount++;
                capturedEvent = e;
                await Task.CompletedTask;
            },
            handlerName: "IntegrationTestHandler"
        );

        var publishEvent = new TestEvent { Data = "integration-test", Value = 42 };
        var result = await eventBus.PublishAsync(publishEvent);

        // Assert
        result.Success.Should().BeTrue();
        result.HandlersInvoked.Should().Be(1);
        handlerCallCount.Should().Be(1);
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Data.Should().Be("integration-test");
        capturedEvent.Value.Should().Be(42);
    }

    [Fact]
    public async Task EventBus_WithMultipleHandlers_AllHandlersAreInvoked()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEventBus();
        var provider = services.BuildServiceProvider();
        var eventBus = provider.GetRequiredService<IEventBus>();

        var handler1Called = false;
        var handler2Called = false;
        var handler3Called = false;

        // Act
        eventBus.Subscribe<TestEvent>(async (e, ct) => { handler1Called = true; await Task.CompletedTask; });
        eventBus.Subscribe<TestEvent>(async (e, ct) => { handler2Called = true; await Task.CompletedTask; });
        eventBus.Subscribe<TestEvent>(async (e, ct) => { handler3Called = true; await Task.CompletedTask; });

        var result = await eventBus.PublishAsync(new TestEvent { Data = "multi-handler", Value = 1 });

        // Assert
        result.HandlersInvoked.Should().Be(3);
        handler1Called.Should().BeTrue();
        handler2Called.Should().BeTrue();
        handler3Called.Should().BeTrue();
    }

    [Fact]
    public async Task EventBus_WithFailingHandler_DeadLetterQueueCaptures()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEventBus(options =>
        {
            options.EnableDeadLetterQueue = true;
            options.MaxRetryAttempts = 1;
        });
        var provider = services.BuildServiceProvider();
        var eventBus = provider.GetRequiredService<IEventBus>();
        var deadLetterService = provider.GetRequiredService<IDeadLetterService>();

        // Act
        eventBus.Subscribe<TestEvent>(
            async (e, ct) =>
            {
                await Task.CompletedTask;
                throw new InvalidOperationException("Handler failure");
            },
            handlerName: "FailingHandler"
        );

        var result = await eventBus.PublishAsync(new TestEvent { Data = "will-fail", Value = 1 });
        var pending = await deadLetterService.GetPendingEntriesAsync();

        // Assert
        result.Success.Should().BeFalse();
        pending.Should().NotBeEmpty();
    }

    [Fact]
    public async Task EventBus_WithRetryPolicy_RetriesOnTransientFailure()
    {
        // Arrange
        var policy = new RetryPolicy()
            .WithMaxRetries(2)
            .WithInitialDelay(TimeSpan.FromMilliseconds(10));

        var attemptCount = 0;

        // Act
        var act = () => policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 3)
                throw new TimeoutException("Transient");
            return "success";
        });

        var result = await policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 3)
                throw new TimeoutException("Transient");
            return "success";
        });

        // Assert
        result.Should().Be("success");
    }

    [Fact]
    public async Task CircuitBreaker_WithEventBus_ProtectsFromCascadingFailures()
    {
        // Arrange
        var breaker = new CircuitBreaker(failureThreshold: 2, timeout: TimeSpan.FromSeconds(5));
        var callCount = 0;

        // Act - Trigger failures to open circuit
        for (int i = 0; i < 2; i++)
        {
            var act = () => breaker.ExecuteAsync(async () =>
            {
                callCount++;
                throw new TimeoutException("Service down");
            });
            await act.Should().ThrowAsync<TimeoutException>();
        }

        // Try to call when circuit is open
        var openAct = () => breaker.ExecuteAsync(async () =>
        {
            callCount++;
            return "should not execute";
        });

        // Assert
        await openAct.Should().ThrowAsync<CircuitBreakerOpenException>();
        callCount.Should().Be(2); // Operation never executed after circuit opened
    }

    [Fact]
    public async Task MetricsCollector_TrackingEndToEnd()
    {
        // Arrange
        var collector = new MetricsCollector();

        // Act
        for (int i = 0; i < 5; i++)
        {
            collector.RecordEventPublished("OrderPlaced", 100 + i * 10);
            collector.RecordHandlerExecution("OrderHandler", "OrderPlaced", 80, success: i < 4);
        }

        var eventMetrics = collector.GetEventMetrics("OrderPlaced");
        var handlerMetrics = collector.GetHandlerMetrics("OrderHandler", "OrderPlaced");

        // Assert
        eventMetrics.Should().NotBeNull();
        eventMetrics!.PublishCount.Should().Be(5);
        eventMetrics.FailureCount.Should().Be(0);

        handlerMetrics.Should().NotBeNull();
        handlerMetrics!.ExecutionCount.Should().Be(5);
        handlerMetrics.SuccessCount.Should().Be(4);
        handlerMetrics.FailureCount.Should().Be(1);
    }

    [Fact]
    public async Task EventFilter_IntegratedWithEventBus()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEventBus();
        var provider = services.BuildServiceProvider();
        var eventBus = provider.GetRequiredService<IEventBus>();

        var largeOrdersProcessed = 0;

        var filter = new EventFilter<TestEvent>()
            .Where(e => e.Value > 50);

        // Act
        eventBus.Subscribe<TestEvent>(
            async (e, ct) =>
            {
                if (filter.Matches(e))
                {
                    largeOrdersProcessed++;
                }
                await Task.CompletedTask;
            },
            handlerName: "FilteredHandler"
        );

        await eventBus.PublishAsync(new TestEvent { Data = "small", Value = 10 });
        await eventBus.PublishAsync(new TestEvent { Data = "large", Value = 100 });
        await eventBus.PublishAsync(new TestEvent { Data = "large2", Value = 75 });

        // Assert
        largeOrdersProcessed.Should().Be(2);
    }

    [Fact]
    public async Task EventBus_WithPriorities_ExecutesInOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEventBus(options => options.AllowParallelHandling = false);
        var provider = services.BuildServiceProvider();
        var eventBus = provider.GetRequiredService<IEventBus>();

        var executionOrder = new List<string>();

        // Act
        eventBus.Subscribe<TestEvent>(
            async (e, ct) =>
            {
                executionOrder.Add("Low");
                await Task.CompletedTask;
            },
            handlerName: "LowPriority",
            priority: (int)HandlerPriority.Low
        );

        eventBus.Subscribe<TestEvent>(
            async (e, ct) =>
            {
                executionOrder.Add("High");
                await Task.CompletedTask;
            },
            handlerName: "HighPriority",
            priority: (int)HandlerPriority.High
        );

        eventBus.Subscribe<TestEvent>(
            async (e, ct) =>
            {
                executionOrder.Add("Medium");
                await Task.CompletedTask;
            },
            handlerName: "MediumPriority",
            priority: (int)HandlerPriority.Medium
        );

        await eventBus.PublishAsync(new TestEvent { Data = "priority-test", Value = 1 });

        // Assert
        executionOrder.Should().ContainInOrder("High", "Medium", "Low");
    }
}

/// <summary>
/// Integration tests for batch event publishing.
/// </summary>
public sealed class BatchEventPublisherIntegrationTests
{
    [Fact]
    public async Task BatchEventPublisher_AccumulatesAndFlushes()
    {
        // Arrange
        var logger = new Mock<ILogger<BatchEventPublisher>>();
        var publisher = new BatchEventPublisher(logger.Object, batchSize: 5);

        var flushedEventCount = 0;

        publisher.SetFlushHandler(async batch =>
        {
            flushedEventCount += batch.Events.Count;
            await Task.CompletedTask;
        });

        // Act
        for (int i = 0; i < 12; i++)
        {
            var envelope = new EventEnvelope(
                new EventMessage($"Event{i}", $"payload{i}"),
                System.Reflection.typeof(object).Assembly
            );
            await publisher.AddEventAsync(envelope);
        }

        // Assert
        flushedEventCount.Should().Be(10); // Two full batches (5 + 5)
    }

    [Fact]
    public async Task BatchEventPublisher_WithErrorIsolation_ProcessesAllEvents()
    {
        // Arrange
        var logger = new Mock<ILogger<BatchEventPublisher>>();
        var publisher = new BatchEventPublisher(logger.Object, batchSize: 3);

        var processedCount = 0;
        var failedCount = 0;

        publisher.SetFlushHandlerWithResult(
            async envelope =>
            {
                processedCount++;
                if (envelope.EventMessage.EventType.Contains("error"))
                {
                    failedCount++;
                    return new EventBatchItemResult { Success = false, ErrorMessage = "Processing failed" };
                }
                return new EventBatchItemResult { Success = true };
            }
        );

        // Act
        for (int i = 0; i < 3; i++)
        {
            var eventType = i == 1 ? "error-event" : $"event{i}";
            var envelope = new EventEnvelope(
                new EventMessage(eventType, "payload"),
                System.Reflection.typeof(object).Assembly
            );
            await publisher.AddEventAsync(envelope);
        }

        // Assert
        processedCount.Should().Be(3);
        failedCount.Should().Be(1);
    }
}

/// <summary>
/// Integration tests for middleware pipeline with event bus.
/// </summary>
public sealed class PipelineIntegrationTests
{
    [Fact]
    public async Task EventBus_WithMiddlewarePipeline_ExecutesInOrder()
    {
        // Arrange
        var executionLog = new List<string>();

        var builder = new PipelineBuilder();

        builder.Use(next => async context =>
        {
            executionLog.Add("Middleware1-Start");
            context.Metadata["start_time"] = DateTime.UtcNow;
            await next(context);
            executionLog.Add("Middleware1-End");
        });

        builder.Use(next => async context =>
        {
            executionLog.Add("Middleware2-Start");
            await next(context);
            executionLog.Add("Middleware2-End");
        });

        var pipeline = builder.Build();

        // Act
        var context = new EventContext
        {
            EventType = "TestEvent",
            EventData = new TestEvent { Data = "test", Value = 1 }
        };

        await pipeline(context);

        // Assert
        executionLog.Should().ContainInOrder(
            "Middleware1-Start",
            "Middleware2-Start",
            "Middleware2-End",
            "Middleware1-End"
        );
    }
}

/// <summary>
/// Concurrent/stress integration tests.
/// </summary>
public sealed class ConcurrencyIntegrationTests
{
    [Fact]
    public async Task EventBus_WithParallelPublishing_HandlesMultipleThreads()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEventBus(options => options.AllowParallelHandling = true);
        var provider = services.BuildServiceProvider();
        var eventBus = provider.GetRequiredService<IEventBus>();

        var processedCount = 0;
        var lockObj = new object();

        eventBus.Subscribe<TestEvent>(
            async (e, ct) =>
            {
                lock (lockObj)
                {
                    processedCount++;
                }
                await Task.CompletedTask;
            },
            handlerName: "ConcurrentHandler"
        );

        // Act
        var tasks = Enumerable.Range(0, 100)
            .Select(i => eventBus.PublishAsync(new TestEvent { Data = $"event-{i}", Value = i }))
            .ToList();

        await Task.WhenAll(tasks);

        // Assert
        processedCount.Should().Be(100);
    }

    [Fact]
    public async Task MetricsCollector_ThreadSafe_WithConcurrentRecording()
    {
        // Arrange
        var collector = new MetricsCollector();

        // Act
        var tasks = Enumerable.Range(0, 10)
            .SelectMany(t => Enumerable.Range(0, 10)
                .Select(i => Task.Run(() =>
                {
                    collector.RecordEventPublished("Event", 100 + i);
                    collector.RecordHandlerExecution("Handler", "Event", 50, true);
                })))
            .ToList();

        await Task.WhenAll(tasks);

        // Assert
        var metrics = collector.GetEventMetrics("Event");
        metrics.Should().NotBeNull();
        metrics!.PublishCount.Should().Be(100);
    }
}
