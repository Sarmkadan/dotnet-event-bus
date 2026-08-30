// SPDX-License-Identifier: MIT
// Tests for the BatchEventPublisher implementation.
// Uses the same namespace style as the existing test files in the repository.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotnetEventBus.Models;
using DotnetEventBus.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotnetEventBus.Tests;

public class BatchEventPublisherBehaviorTests
{
    [Fact]
    public async Task AddEventAsync_BuffersBelowThresholdAndFlushesAtThreshold()
    {
        // Arrange
        var publisher = CreatePublisher(batchSize: 3);
        var flushedBatches = new List<EventBatch>();
        publisher.SetFlushHandler(batch =>
        {
            flushedBatches.Add(batch);
            return Task.CompletedTask;
        });

        // Act
        await publisher.AddEventAsync(CreateEnvelope("event-1"));
        await publisher.AddEventAsync(CreateEnvelope("event-2"));

        // Assert
        Assert.Equal(2, publisher.GetBufferSize());
        Assert.Empty(flushedBatches);

        // Act
        await publisher.AddEventAsync(CreateEnvelope("event-3"));

        // Assert
        var batch = Assert.Single(flushedBatches);
        Assert.Equal(new[] { "event-1", "event-2", "event-3" },
            batch.Events.Select(envelope => envelope.EventType));
        Assert.Equal(0, publisher.GetBufferSize());
    }

    [Fact]
    public async Task AddEventsAsync_WithMultipleEnvelopes_AddsAndFlushesAllEnvelopes()
    {
        // Arrange
        var publisher = CreatePublisher(batchSize: 2);
        var flushedEventTypes = new List<string>();
        publisher.SetFlushHandler(batch =>
        {
            flushedEventTypes.AddRange(batch.Events.Select(envelope => envelope.EventType));
            return Task.CompletedTask;
        });
        var envelopes = new[]
        {
            CreateEnvelope("event-1"),
            CreateEnvelope("event-2"),
            CreateEnvelope("event-3")
        };

        // Act
        await publisher.AddEventsAsync(envelopes);

        // Assert
        Assert.Equal(new[] { "event-1", "event-2" }, flushedEventTypes);
        Assert.Equal(1, publisher.GetBufferSize());

        // Act
        await publisher.FlushAsync();

        // Assert
        Assert.Equal(new[] { "event-1", "event-2", "event-3" }, flushedEventTypes);
        Assert.Equal(0, publisher.GetBufferSize());
    }

    [Fact]
    public async Task FlushAsync_WithBufferedEvents_EmptiesBufferAndInvokesFlushHandler()
    {
        // Arrange
        var publisher = CreatePublisher(batchSize: 10);
        EventBatch? flushedBatch = null;
        publisher.SetFlushHandler(batch =>
        {
            flushedBatch = batch;
            return Task.CompletedTask;
        });
        await publisher.AddEventsAsync(new[]
        {
            CreateEnvelope("event-1"),
            CreateEnvelope("event-2")
        });

        // Act
        await publisher.FlushAsync();

        // Assert
        Assert.NotNull(flushedBatch);
        Assert.Equal(2, flushedBatch.Events.Count);
        Assert.Equal(0, publisher.GetBufferSize());
    }

    [Fact]
    public async Task GetBufferSizeAndGetStats_ReflectBufferingAndFlushActivity()
    {
        // Arrange
        var publisher = CreatePublisher(batchSize: 10);
        publisher.SetFlushHandler(_ => Task.CompletedTask);
        var initialStats = publisher.GetStats();
        var beforeFlush = DateTime.UtcNow;

        // Act
        await publisher.AddEventsAsync(new[]
        {
            CreateEnvelope("event-1"),
            CreateEnvelope("event-2")
        });
        var bufferedStats = publisher.GetStats();

        // Assert
        Assert.Equal(2, publisher.GetBufferSize());
        Assert.Equal(2, bufferedStats.BufferedEventCount);
        Assert.Equal(2, bufferedStats.BufferedEventSize);
        Assert.Equal(initialStats.LastFlushTime, bufferedStats.LastFlushTime);

        // Act
        await publisher.FlushAsync();
        var flushedStats = publisher.GetStats();

        // Assert
        Assert.Equal(0, publisher.GetBufferSize());
        Assert.Equal(0, flushedStats.BufferedEventCount);
        Assert.Equal(0, flushedStats.BufferedEventSize);
        Assert.InRange(flushedStats.LastFlushTime, beforeFlush, DateTime.UtcNow);
    }

    [Fact]
    public async Task FlushAsync_WithoutFlushHandler_EmptiesBufferWithoutThrowing()
    {
        // Arrange
        var publisher = CreatePublisher(batchSize: 10);
        await publisher.AddEventAsync(CreateEnvelope("event-1"));

        // Act
        var exception = await Record.ExceptionAsync(() => publisher.FlushAsync());

        // Assert
        Assert.Null(exception);
        Assert.Equal(0, publisher.GetBufferSize());
        Assert.Equal(0, publisher.GetStats().BufferedEventCount);
    }

    private static BatchEventPublisher CreatePublisher(int batchSize)
    {
        return new BatchEventPublisher(
            NullLogger<BatchEventPublisher>.Instance,
            batchSize,
            TimeSpan.FromHours(1));
    }

    private static EventEnvelope CreateEnvelope(string eventType)
    {
        return EventEnvelope.Create(eventType, new { Value = eventType });
    }
}
