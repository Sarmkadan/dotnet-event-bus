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
public class MetricsCollector
{
    private readonly ConcurrentDictionary<string, EventMetrics> _metrics = [];
    private readonly ConcurrentDictionary<string, HandlerMetrics> _handlerMetrics = [];
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
    /// Resets all metrics.
    /// </summary>
    public void Reset()
    {
        _metrics.Clear();
        _handlerMetrics.Clear();
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

public class EventMetrics
{
    public string? EventType { get; set; }
    public long PublishCount { get; set; }
    public long FailureCount { get; set; }
    public long TotalDurationMs { get; set; }
    public double AverageDurationMs { get; set; }
    public long MaxDurationMs { get; set; }
    public DateTime LastPublishedAt { get; set; }
    public DateTime? LastFailureAt { get; set; }
    public string? LastError { get; set; }

    public double SuccessRate => PublishCount > 0
        ? ((PublishCount - FailureCount) / (double)PublishCount) * 100
        : 100;
}

public class HandlerMetrics
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

public class SystemMetrics
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
