#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotnetEventBus.Advanced;
using DotnetEventBus.Models;
using DotnetEventBus.Services;

namespace DotnetEventBus.Api;

/// <summary>
/// Base API controller for exposing event bus operations via HTTP.
/// Provides REST endpoints for publishing, querying, and managing events.
/// Why: Allows external systems to interact with the event bus via standard HTTP.
/// </summary>
public abstract class EventBusApiController
{
    protected readonly IEventBus _eventBus;
    protected readonly MetricsCollector? _metrics;
    private readonly DeadLetterStatistics? _deadLetterStatistics;
    private readonly BatchPublisherStats? _batchPublisherStats;

    protected EventBusApiController(IEventBus eventBus)
        : this(eventBus, null, null, null)
    {
    }

    /// <summary>
    /// Creates a controller that also reports statistics from the supplied metrics collector.
    /// </summary>
    protected EventBusApiController(
        IEventBus eventBus,
        MetricsCollector? metrics,
        DeadLetterStatistics? deadLetterStatistics = null,
        BatchPublisherStats? batchPublisherStats = null)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _metrics = metrics;
        _deadLetterStatistics = deadLetterStatistics;
        _batchPublisherStats = batchPublisherStats;
    }

    /// <summary>
    /// Publishes an event.
    /// </summary>
    public virtual async Task<ApiResponse<EventPublishResult>> PublishEventAsync(string eventType, object payload)
    {
        if (string.IsNullOrEmpty(eventType))
        {
            return ApiResponse<EventPublishResult>.Error("Event type is required");
        }

        if (payload is null)
        {
            return ApiResponse<EventPublishResult>.Error("Payload is required");
        }

        try
        {
            var envelope = EventEnvelope.Create(eventType, payload);
            var result = await _eventBus.PublishAsync(envelope);

            if (!result.Success)
            {
                return ApiResponse<EventPublishResult>.Error(result.ErrorMessage ?? "Publish failed");
            }

            return ApiResponse<EventPublishResult>.Success(new EventPublishResult
            {
                EventId = envelope.EventId,
                EventType = eventType,
                PublishedAt = DateTime.UtcNow,
                Success = true
            });
        }
        catch (Exception ex)
        {
            return ApiResponse<EventPublishResult>.Error(ex.Message);
        }
    }

    /// <summary>
    /// Publishes multiple events in a batch.
    /// </summary>
    public virtual async Task<ApiResponse<BatchPublishResult>> PublishBatchAsync(List<EventEnvelope> events)
    {
        if (events is null || events.Count == 0)
        {
            return ApiResponse<BatchPublishResult>.Error("At least one event is required");
        }

        try
        {
            var batch = EventBatch.Create(events.ToArray());

            var publishedCount = 0;
            var failures = new List<string>();

            foreach (var envelope in events)
            {
                var result = await _eventBus.PublishAsync(envelope);

                if (result.Success)
                {
                    publishedCount++;
                }
                else
                {
                    failures.Add($"{envelope.EventId}: {result.ErrorMessage ?? "publish failed"}");
                }
            }

            if (failures.Count == events.Count)
            {
                return ApiResponse<BatchPublishResult>.Error(string.Join("; ", failures));
            }

            return ApiResponse<BatchPublishResult>.Success(new BatchPublishResult
            {
                BatchId = batch.BatchId,
                EventCount = publishedCount,
                PublishedAt = DateTime.UtcNow,
                Success = failures.Count == 0
            });
        }
        catch (Exception ex)
        {
            return ApiResponse<BatchPublishResult>.Error(ex.Message);
        }
    }

    /// <summary>
    /// Gets statistics about the event bus.
    /// </summary>
    public virtual ApiResponse<EventBusStats> GetStats()
    {
        try
        {
            var systemMetrics = _metrics?.GetSystemMetrics();

            var stats = new EventBusStats
            {
                Status = DetermineStatus(systemMetrics).ToString(),
                TotalEventsPublished = systemMetrics?.TotalEventsPublished ?? 0,
                TotalEventsFailed = systemMetrics?.TotalEventsFailed ?? 0,
                ActiveSubscriptions = systemMetrics?.HandlersCount ?? 0,
                LastCheckTime = DateTime.UtcNow
            };

            return ApiResponse<EventBusStats>.Success(stats);
        }
        catch (Exception ex)
        {
            return ApiResponse<EventBusStats>.Error(ex.Message);
        }
    }

    /// <summary>
    /// Gets health status of the event bus.
    /// </summary>
    public virtual ApiResponse<HealthStatus> GetHealthAsync()
    {
        try
        {
            return ApiResponse<HealthStatus>.Success(DetermineStatus(_metrics?.GetSystemMetrics()));
        }
        catch (Exception ex)
        {
            return ApiResponse<HealthStatus>.Error(ex.Message);
        }
    }

    /// <summary>
    /// Returns a consolidated snapshot that includes bus statistics, dead‑letter statistics,
    /// and batch‑publisher statistics. The response follows the same <see cref="ApiResponse{T}"/>
    /// envelope used by the other actions.
    /// </summary>
    public virtual ApiResponse<ConsolidatedStats> GetConsolidatedStats()
    {
        try
        {
            // Re‑use the existing GetStats method to obtain the bus section.
            var busStats = GetStats().Data;

            var result = new ConsolidatedStats
            {
                Bus = busStats,
                DeadLetter = _deadLetterStatistics,
                Batch = _batchPublisherStats
            };

            return ApiResponse<ConsolidatedStats>.Success(result);
        }
        catch (Exception ex)
        {
            return ApiResponse<ConsolidatedStats>.Error(ex.Message);
        }
    }

    /// <summary>
    /// Async version of <c>GetConsolidatedStats</c> suitable for typical HTTP GET endpoints.
    /// </summary>
    public virtual async Task<ApiResponse<ConsolidatedStats>> GetConsolidatedStatsAsync()
    {
        // The method body is deliberately synchronous but wrapped in a Task to keep the async signature.
        return await Task.FromResult(GetConsolidatedStats());
    }

    private static HealthStatus DetermineStatus(SystemMetrics? metrics)
    {
        if (metrics is null || metrics.TotalEventsPublished == 0)
        {
            return HealthStatus.Healthy;
        }

        return metrics.SuccessRate switch
        {
            < 50 => HealthStatus.Unhealthy,
            < 95 => HealthStatus.Degraded,
            _ => HealthStatus.Healthy
        };
    }
}

/// <summary>
/// Generic API response wrapper.
/// </summary>
public sealed class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Success(T data)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Data = data
        };
    }

    public static ApiResponse<T> Error(string error)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            ErrorMessage = error
        };
    }
}

/// <summary>
/// Result of publishing an event.
/// </summary>
public sealed class EventPublishResult
{
    public string? EventId { get; set; }
    public string? EventType { get; set; }
    public DateTime PublishedAt { get; set; }
    public bool Success { get; set; }
}

/// <summary>
/// Result of publishing a batch.
/// </summary>
public sealed class BatchPublishResult
{
    public string? BatchId { get; set; }
    public int EventCount { get; set; }
    public DateTime PublishedAt { get; set; }
    public bool Success { get; set; }
}

/// <summary>
/// Event bus statistics for API responses.
/// </summary>
public sealed class EventBusStats
{
    public string? Status { get; set; }
    public long TotalEventsPublished { get; set; }
    public long TotalEventsFailed { get; set; }
    public int ActiveSubscriptions { get; set; }
    public DateTime LastCheckTime { get; set; }
}

/// <summary>
/// Consolidated statistics snapshot returned by <c>GetConsolidatedStats</c>.
/// </summary>
public sealed class ConsolidatedStats
{
    /// <summary>
    /// Statistics about the event bus itself.
    /// </summary>
    public EventBusStats? Bus { get; set; }

    /// <summary>
    /// Dead‑letter queue statistics (if available).
    /// </summary>
    public DeadLetterStatistics? DeadLetter { get; set; }

    /// <summary>
    /// Batch publisher statistics (if available).
    /// </summary>
    public BatchPublisherStats? Batch { get; set; }
}

/// <summary>
/// Enum for API response health status.
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}
