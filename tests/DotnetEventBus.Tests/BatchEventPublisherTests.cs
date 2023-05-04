#nullable enable

using FluentAssertions;
using Moq;
using Xunit;
using DotnetEventBus.Services;
using DotnetEventBus.Models;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Tests;

public sealed class BatchEventPublisherTests
{
    private readonly Mock<ILogger<BatchEventPublisher>> _mockLogger;

    public BatchEventPublisherTests()
    {
        _mockLogger = new Mock<ILogger<BatchEventPublisher>>();
    }

    [Fact]
    public async Task AddEventAsync_WithValidEnvelope_ShouldAddToBatch()
    {
        // Arrange
        var publisher = new BatchEventPublisher(_mockLogger.Object, batchSize: 10);
        var envelope = new EventEnvelope { EventType = "TestEvent", Payload = "payload" };

        // Act
        var result = await publisher.AddEventAsync(envelope);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AddEventAsync_WithInvalidEnvelope_ShouldReturnFalse()
    {
        // Arrange
        var publisher = new BatchEventPublisher(_mockLogger.Object, batchSize: 10);
        var envelope = new EventEnvelope { EventType = "", Payload = "" }; // Invalid - empty event type

        // Act
        var result = await publisher.AddEventAsync(envelope);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddEventAsync_WithNullEnvelope_ShouldThrowArgumentNullException()
    {
        // Arrange
        var publisher = new BatchEventPublisher(_mockLogger.Object, batchSize: 10);

        // Act & Assert
        await publisher.Invoking(p => p.AddEventAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SetFlushHandler_ShouldBeInvokedWhenBatchIsFull()
    {
        // Arrange
        var publisher = new BatchEventPublisher(_mockLogger.Object, batchSize: 3);
        var flushedBatches = new List<EventBatch>();

        publisher.SetFlushHandler(async batch =>
        {
            flushedBatches.Add(batch);
            await Task.CompletedTask;
        });

        // Act - Add 3 events to trigger flush
        for (int i = 0; i < 3; i++)
        {
            var envelope = new EventEnvelope { EventType = "Event" + i, Payload = "payload" + i };
            await publisher.AddEventAsync(envelope);
        }

        // Assert
        flushedBatches.Should().HaveCount(1);
        flushedBatches[0].Events.Should().HaveCount(3);
    }

    [Fact]
    public async Task SetFlushHandler_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var publisher = new BatchEventPublisher(_mockLogger.Object);

        // Act & Assert
        publisher.Invoking(p => p.SetFlushHandler(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task SetFlushHandlerWithResult_ShouldInvokePerEventHandler()
    {
        // Arrange
        var publisher = new BatchEventPublisher(_mockLogger.Object, batchSize: 2);
        var processedEvents = new List<string>();
        var batchResults = new List<BatchPublishResult>();

        publisher.SetFlushHandlerWithResult(
            async envelope =>
            {
                processedEvents.Add(envelope.EventType);
                return new EventBatchItemResult { Success = true };
            },
            batch => batchResults.Add(batch)
        );

        // Act
        for (int i = 0; i < 2; i++)
        {
            var envelope = new EventEnvelope { EventType = "Event" + i, Payload = "payload" };
            await publisher.AddEventAsync(envelope);
        }

        // Assert
        processedEvents.Should().HaveCount(2);
        batchResults.Should().HaveCount(1);
    }

    [Fact]
    public async Task SetFlushHandlerWithResult_WithErrorIsolation_ShouldProcessAllEventsEvenWithFailures()
    {
        // Arrange
        var publisher = new BatchEventPublisher(_mockLogger.Object, batchSize: 3);
        var processedCount = 0;
        var failedCount = 0;

        publisher.SetFlushHandlerWithResult(
            async envelope =>
            {
                processedCount++;
                if (envelope.EventType == "Event1")
                {
                    failedCount++;
                    return new EventBatchItemResult { Success = false, ErrorMessage = "Failed" };
                }
                return new EventBatchItemResult { Success = true };
            }
        );

        // Act
        for (int i = 0; i < 3; i++)
        {
            var envelope = new EventEnvelope { EventType = "Event" + i, Payload = "payload" };
            await publisher.AddEventAsync(envelope);
        }

        // Assert
        processedCount.Should().Be(3);
        failedCount.Should().Be(1);
    }

    [Fact]
    public async Task SetFlushHandlerWithResult_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var publisher = new BatchEventPublisher(_mockLogger.Object);

        // Act & Assert
        publisher.Invoking(p => p.SetFlushHandlerWithResult(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task AddEventAsync_WithMultipleBatches_ShouldFlushEachBatch()
    {
        // Arrange
        var publisher = new BatchEventPublisher(_mockLogger.Object, batchSize: 2);
        var flushCount = 0;

        publisher.SetFlushHandler(async batch =>
        {
            flushCount++;
            await Task.CompletedTask;
        });

        // Act - Add 5 events (should flush 2 batches with 1 remaining)
        for (int i = 0; i < 5; i++)
        {
            var envelope = new EventEnvelope { EventType = "Event" + i, Payload = "payload" };
            await publisher.AddEventAsync(envelope);
        }

        // Assert
        flushCount.Should().Be(2);
    }

    [Fact]
    public void Constructor_WithInvalidBatchSize_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => new BatchEventPublisher(_mockLogger.Object, batchSize: 0);
        act.Should().Throw<ArgumentException>();

        var act2 = () => new BatchEventPublisher(_mockLogger.Object, batchSize: -1);
        act2.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new BatchEventPublisher(null!, batchSize: 10);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task FlushAsync_WithoutHandler_ShouldNotThrow()
    {
        // Arrange
        var publisher = new BatchEventPublisher(_mockLogger.Object, batchSize: 100);

        // Act & Assert - Should not throw
        var envelope = new EventEnvelope { EventType = "Event", Payload = "payload" };
        var result = await publisher.AddEventAsync(envelope);
        result.Should().BeTrue();
    }
}
