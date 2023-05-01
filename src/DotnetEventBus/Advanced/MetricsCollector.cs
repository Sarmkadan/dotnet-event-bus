#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DotnetEventBus.Advanced;

/// <summary>
/// Collects and aggregates metrics about event processing.
/// Tracks timing, counts, errors, and throughput for observability.
/// Why: Provides visibility into system health and performance bottlenecks.
/// </summary>
public sealed class MetricsCollector
{
    private readonly ConcurrentDictionary<string, EventMetrics> _metrics = [];
    private readonly ConcurrentDictionary<string, HandlerMetrics> _handlerMetrics = [];
    // Stores recent duration samples per event type for percentile calculations.
    private readonly ConcurrentDictionary<string, ConcurrentQueue<long>> _durationSamples = [];
    private const int MaxSamplesPerEventType = 1000;
    private long _totalEventsPublished = 0;
    private long _totalEventsFailed = 0;
    private DateTime _startTime = DateTime.UtcNow;

    /// <summary>
    /// Records an event publication.
    /// </summary>
    public void RecordEventPublished(string eventType, long durationMs)
    {
        Interlocked.Increment(ref _totalEventsPublished);

        var metrics = _metrics.GetOrAdd(eventType, _ => new EventMetrics { EventType = eventType });
        metrics.PublishCount++;
        metrics.TotalDurationMs += durationMs;
        metrics.LastPublishedAt = DateTime.UtcNow;
        metrics.AverageDurationMs = (double)metrics.TotalDurationMs / metrics.PublishCount;

        if (durationMs > metrics.MaxDurationMs)
            metrics.MaxDurationMs = durationMs;

        if (metrics.MinDurationMs == 0 || durationMs < metrics.MinDurationMs)
            metrics.MinDurationMs = durationMs;

        // Keep a bounded sample window for percentile calculations.
        var samples = _durationSamples.GetOrAdd(eventType, _ => new ConcurrentQueue<long>());
        samples.Enqueue(durationMs);
        while (samples.Count > MaxSamplesPerEventType)
            samples.TryDequeue(out _);
    }

    /// <summary>
    /// Records a failed event.
    /// </summary>
    public void RecordEventFailed(string eventType, string handlerName, Exception exception)
    {
        Interlocked.Increment(ref _totalEventsFailed);

        var metrics = _metrics.GetOrAdd(eventType, _ => new EventMetrics { EventType = eventType });
        metrics.FailureCount++;
        metrics.LastFailureAt = DateTime.UtcNow;
        metrics.LastError = exception.Message;

        RecordHandlerFailure(handlerName, eventType);
    }

    /// <summary>
    /// Records handler execution.
    /// </summary>
    public void RecordHandlerExecution(string handlerName, string eventType, long durationMs, bool success)
    {
        var key = $"{handlerName}:{eventType}";
        var metrics = _handlerMetrics.GetOrAdd(key, _ => new HandlerMetrics
        {
            HandlerName = handlerName,
            EventType = eventType
        });

        metrics.ExecutionCount++;
        metrics.TotalDurationMs += durationMs;
        metrics.AverageDurationMs = (double)metrics.TotalDurationMs / metrics.ExecutionCount;

        if (!success)
        {
            metrics.FailureCount++;
        }
    }

    /// <summary>
    /// Gets metrics for a specific event type.
    /// </summary>
    public EventMetrics? GetEventMetrics(string eventType)
    {
        _metrics.TryGetValue(eventType, out var metrics);
        return metrics;
    }

    /// <summary>
    /// Gets all event metrics.
    /// </summary>
    public IEnumerable<EventMetrics> GetAllEventMetrics()
    {
        return _metrics.Values.OrderByDescending(m => m.PublishCount);
    }

    /// <summary>
    /// Gets metrics for a specific handler.
    /// </summary>
    public IEnumerable<HandlerMetrics> GetHandlerMetrics(string handlerName)
    {
        return _handlerMetrics.Values
            .Where(m => m.HandlerName == handlerName)
            .OrderByDescending(m => m.ExecutionCount);
    }

    /// <summary>
    /// Gets overall system metrics.
    /// </summary>
    public SystemMetrics GetSystemMetrics()
    {
        var uptime = DateTime.UtcNow - _startTime;
        var totalEvents = _totalEventsPublished;
        var successRate = totalEvents > 0 ? ((totalEvents - _totalEventsFailed) / (double)totalEvents) * 100 : 100;

        return new SystemMetrics
        {
            StartTime = _startTime,
            UpTime = uptime,
            TotalEventsPublished = totalEvents,
            TotalEventsFailed = _totalEventsFailed,
            SuccessRate = successRate,
            EventTypesCount = _metrics.Count,
            HandlersCount = _handlerMetrics.Values.Select(m => m.HandlerName).Distinct().Count(),
            ThroughputPerSecond = totalEvents / Math.Max(uptime.TotalSeconds, 1)
        };
    }

    /// <summary>
    /// Gets detailed latency statistics for a specific event type, including p95.
    /// Returns null when no data has been recorded for the event type.
    /// </summary>
    public LatencyStats? GetLatencyStats(string eventType)
    {
        if (!_metrics.TryGetValue(eventType, out var metrics))
            return null;

        long p95Ms = 0;
        if (_durationSamples.TryGetValue(eventType, out var samples))
        {
            var sorted = samples.ToArray();
            Array.Sort(sorted);
            if (sorted.Length > 0)
            {
                var p95Index = (int)Math.Ceiling(sorted.Length * 0.95) - 1;
                p95Ms = sorted[Math.Max(0, p95Index)];
            }
        }

        return new LatencyStats
        {
            EventType = eventType,
            MinMs = metrics.MinDurationMs,
            MaxMs = metrics.MaxDurationMs,
            AvgMs = metrics.AverageDurationMs,
            P95Ms = p95Ms,
            SampleCount = metrics.PublishCount
        };
    }

    /// <summary>
    /// Gets latency statistics for all recorded event types.
    /// </summary>
    public IEnumerable<LatencyStats> GetAllLatencyStats()
    {
        return _metrics.Keys
            .Select(GetLatencyStats)
            .Where(s => s is not null)
            .Cast<LatencyStats>();
    }

    /// <summary>
    /// Resets all metrics.
    /// </summary>
    public void Reset()
    {
        _metrics.Clear();
        _handlerMetrics.Clear();
        _durationSamples.Clear();
        _totalEventsPublished = 0;
        _totalEventsFailed = 0;
        _startTime = DateTime.UtcNow;
    }

    private void RecordHandlerFailure(string handlerName, string eventType)
    {
        var key = $"{handlerName}:{eventType}";
        var metrics = _handlerMetrics.GetOrAdd(key, _ => new HandlerMetrics
        {
            HandlerName = handlerName,
            EventType = eventType
        });

        metrics.FailureCount++;
    }
}

public sealed class EventMetrics
{
    public string? EventType { get; set; }
    public long PublishCount { get; set; }
    public long FailureCount { get; set; }
    public long TotalDurationMs { get; set; }
    public double AverageDurationMs { get; set; }
    public long MinDurationMs { get; set; }
    public long MaxDurationMs { get; set; }
    public DateTime LastPublishedAt { get; set; }
    public DateTime? LastFailureAt { get; set; }
    public string? LastError { get; set; }

    public double SuccessRate => PublishCount > 0
        ? ((PublishCount - FailureCount) / (double)PublishCount) * 100
        : 100;
}

public sealed class HandlerMetrics
{
    public string? HandlerName { get; set; }
    public string? EventType { get; set; }
    public long ExecutionCount { get; set; }
    public long FailureCount { get; set; }
    public long TotalDurationMs { get; set; }
    public double AverageDurationMs { get; set; }

    public double SuccessRate => ExecutionCount > 0
        ? ((ExecutionCount - FailureCount) / (double)ExecutionCount) * 100
        : 100;
}

public sealed class SystemMetrics
{
    public DateTime StartTime { get; set; }
    public TimeSpan UpTime { get; set; }
    public long TotalEventsPublished { get; set; }
    public long TotalEventsFailed { get; set; }
    public double SuccessRate { get; set; }
    public int EventTypesCount { get; set; }
    public int HandlersCount { get; set; }
    public double ThroughputPerSecond { get; set; }
}

/// <summary>
/// Latency statistics for a specific event type.
/// </summary>
public sealed class LatencyStats
{
    public string? EventType { get; set; }
    /// <summary>Minimum observed publish latency in milliseconds.</summary>
    public long MinMs { get; set; }
    /// <summary>Maximum observed publish latency in milliseconds.</summary>
    public long MaxMs { get; set; }
    /// <summary>Average publish latency in milliseconds.</summary>
    public double AvgMs { get; set; }
    /// <summary>95th-percentile publish latency in milliseconds (based on recent samples).</summary>
    public long P95Ms { get; set; }
    /// <summary>Total number of recorded samples.</summary>
    public long SampleCount { get; set; }
}
