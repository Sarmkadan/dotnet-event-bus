#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    protected EventBusApiController(IEventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
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
            // TODO: Use actual event bus when available
            // var result = await _eventBus.PublishAsync(envelope);

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
            // TODO: Use actual batch publisher when available
            // await _batchPublisher.AddEventsAsync(events);

            return ApiResponse<BatchPublishResult>.Success(new BatchPublishResult
            {
                BatchId = batch.BatchId,
                EventCount = batch.EventCount,
                PublishedAt = DateTime.UtcNow,
                Success = true
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
            // TODO: Get actual stats from event bus
            var stats = new EventBusStats
            {
                Status = "Healthy",
                TotalEventsPublished = 0,
                TotalEventsFailed = 0,
                ActiveSubscriptions = 0,
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
            // TODO: Check actual health
            return ApiResponse<HealthStatus>.Success(HealthStatus.Healthy);
        }
        catch (Exception ex)
        {
            return ApiResponse<HealthStatus>.Error(ex.Message);
        }
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
/// Enum for API response health status.
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}
