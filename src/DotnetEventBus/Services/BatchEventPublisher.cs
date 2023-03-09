#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotnetEventBus.Models;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Services;

/// <summary>
/// Publishes events in batches for improved throughput.
/// Collects events and flushes them according to size or time triggers.
/// Why: Reduces per-event overhead and improves system throughput significantly.
///
/// Error handling semantics:
/// - Each event in a batch is processed independently; a handler throwing for one
///   event does not prevent the remaining events from being processed.
/// - Use <see cref="SetFlushHandlerWithResult"/> to receive a per-event
///   <see cref="BatchPublishResult"/> that lists which events succeeded and which failed.
/// - Failed events are NOT automatically moved to a dead letter queue by this class;
///   inspect <see cref="BatchPublishResult.FailedEvents"/> and handle them explicitly.
/// </summary>
public sealed class BatchEventPublisher
{
    private readonly ILogger<BatchEventPublisher> _logger;
    private readonly List<EventEnvelope> _buffer = [];
    private readonly int _batchSize;
    private readonly TimeSpan _flushInterval;
    private DateTime _lastFlushTime = DateTime.UtcNow;
    private readonly object _lock = new();
    private Func<EventBatch, Task>? _flushHandler;
    private Func<EventEnvelope, Task<EventBatchItemResult>>? _perEventHandler;

    public BatchEventPublisher(
        ILogger<BatchEventPublisher> logger,
        int batchSize = 100,
        TimeSpan? flushInterval = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be positive", nameof(batchSize));

        _batchSize = batchSize;
        _flushInterval = flushInterval ?? TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// Sets the handler that processes batches as a whole.
    /// </summary>
    public void SetFlushHandler(Func<EventBatch, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _flushHandler = handler;
    }

    /// <summary>
    /// Sets a per-event handler used to process each event individually with error isolation.
    /// The returned <see cref="EventBatchItemResult"/> is aggregated into a
    /// <see cref="BatchPublishResult"/> that is emitted via <paramref name="onBatchComplete"/>.
    /// Unlike <see cref="SetFlushHandler"/>, a failure in one event does not stop the rest.
    /// </summary>
    public void SetFlushHandlerWithResult(
        Func<EventEnvelope, Task<EventBatchItemResult>> handler,
        Action<BatchPublishResult>? onBatchComplete = null)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _perEventHandler = handler;
        _onBatchComplete = onBatchComplete;
    }

    private Action<BatchPublishResult>? _onBatchComplete;

    /// <summary>
    /// Adds an event to the batch buffer.
    /// Automatically flushes if batch is full or interval has passed.
    /// </summary>
    public async Task<bool> AddEventAsync(EventEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (!envelope.IsValid())
        {
            _logger.LogWarning("Invalid event envelope provided");
            return false;
        }

        lock (_lock)
        {
            _buffer.Add(envelope);

            // Check if we should flush
            bool shouldFlush = _buffer.Count >= _batchSize ||
                              (DateTime.UtcNow - _lastFlushTime) >= _flushInterval;

            if (shouldFlush && _buffer.Count > 0)
            {
                var batch = new EventBatch { Events = new List<EventEnvelope>(_buffer) };
                _buffer.Clear();
                _lastFlushTime = DateTime.UtcNow;

                // Flush outside lock
                _ = Task.Run(async () => await FlushBatchAsync(batch));
            }
        }

        return true;
    }

    /// <summary>
    /// Adds multiple events to the batch.
    /// </summary>
    public async Task AddEventsAsync(IEnumerable<EventEnvelope> envelopes)
    {
        ArgumentNullException.ThrowIfNull(envelopes);

        foreach (var envelope in envelopes)
        {
            await AddEventAsync(envelope);
        }
    }

    /// <summary>
    /// Flushes all buffered events immediately.
    /// </summary>
    public async Task FlushAsync()
    {
        EventBatch? batch = null;

        lock (_lock)
        {
            if (_buffer.Count > 0)
            {
                batch = new EventBatch { Events = new List<EventEnvelope>(_buffer) };
                _buffer.Clear();
                _lastFlushTime = DateTime.UtcNow;
            }
        }

        if (batch is not null)
        {
            await FlushBatchAsync(batch);
        }
    }

    /// <summary>
    /// Gets the current number of buffered events.
    /// </summary>
    public int GetBufferSize()
    {
        lock (_lock)
        {
            return _buffer.Count;
        }
    }

    /// <summary>
    /// Gets statistics about the publisher.
    /// </summary>
    public BatchPublisherStats GetStats()
    {
        lock (_lock)
        {
            return new BatchPublisherStats
            {
                BufferedEventCount = _buffer.Count,
                BufferedEventSize = _buffer.Count,
                LastFlushTime = _lastFlushTime
            };
        }
    }

    private async Task FlushBatchAsync(EventBatch batch)
    {
        if (_perEventHandler is not null)
        {
            await FlushBatchWithResultAsync(batch);
            return;
        }

        if (_flushHandler is null)
        {
            _logger.LogWarning("No flush handler configured for batch publisher");
            return;
        }

        try
        {
            _logger.LogInformation("Flushing batch with {Count} events", batch.Events.Count);
            await _flushHandler(batch);
            _logger.LogInformation("Batch flush completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch flush failed for {Count} events", batch.Events.Count);
            throw;
        }
    }

    private async Task FlushBatchWithResultAsync(EventBatch batch)
    {
        _logger.LogInformation("Flushing batch with {Count} events (per-event mode)", batch.Events.Count);

        var result = new BatchPublishResult { BatchId = batch.BatchId };

        foreach (var envelope in batch.Events)
        {
            try
            {
                var itemResult = await _perEventHandler!(envelope);
                result.EventResults.Add(itemResult);
                if (itemResult.Success)
                    result.SucceededCount++;
                else
                    result.FailedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handler threw while processing event {EventId} ({EventType})",
                    envelope.EventId, envelope.EventType);
                result.EventResults.Add(new EventBatchItemResult
                {
                    EventId = envelope.EventId,
                    EventType = envelope.EventType,
                    Success = false,
                    Exception = ex,
                    ErrorMessage = ex.Message
                });
                result.FailedCount++;
            }
        }

        _logger.LogInformation(
            "Batch {BatchId} flush complete: {Succeeded} succeeded, {Failed} failed",
            batch.BatchId, result.SucceededCount, result.FailedCount);

        _onBatchComplete?.Invoke(result);
    }
}

/// <summary>
/// Statistics about batch publisher state.
/// </summary>
public sealed class BatchPublisherStats
{
    public int BufferedEventCount { get; set; }
    public int BufferedEventSize { get; set; }
    public DateTime LastFlushTime { get; set; }
}

/// <summary>
/// Result of publishing a single event inside a batch.
/// </summary>
public sealed class EventBatchItemResult
{
    public string? EventId { get; set; }
    public string? EventType { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
}

/// <summary>
/// Aggregated result of processing one flushed batch.
/// Includes per-event results so callers can inspect exactly which events failed
/// and decide whether to retry or send them to a dead letter store.
/// </summary>
public sealed class BatchPublishResult
{
    public string? BatchId { get; set; }
    public int SucceededCount { get; set; }
    public int FailedCount { get; set; }
    public bool AllSucceeded => FailedCount == 0;
    public List<EventBatchItemResult> EventResults { get; set; } = [];

    /// <summary>
    /// Returns only the results for events that failed.
    /// </summary>
    public IEnumerable<EventBatchItemResult> FailedEvents =>
        EventResults.Where(r => !r.Success);
}
