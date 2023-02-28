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
/// </summary>
public class BatchEventPublisher
{
    private readonly ILogger<BatchEventPublisher> _logger;
    private readonly List<EventEnvelope> _buffer = [];
    private readonly int _batchSize;
    private readonly TimeSpan _flushInterval;
    private DateTime _lastFlushTime = DateTime.UtcNow;
    private readonly object _lock = new();
    private Func<EventBatch, Task>? _flushHandler;

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
    /// Sets the handler that processes batches.
    /// </summary>
    public void SetFlushHandler(Func<EventBatch, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _flushHandler = handler;
    }

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

        if (batch != null)
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
        if (_flushHandler == null)
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
}

/// <summary>
/// Statistics about batch publisher state.
/// </summary>
public class BatchPublisherStats
{
    public int BufferedEventCount { get; set; }
    public int BufferedEventSize { get; set; }
    public DateTime LastFlushTime { get; set; }
}
