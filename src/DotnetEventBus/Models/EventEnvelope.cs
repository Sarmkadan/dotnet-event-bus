// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace DotnetEventBus.Models;

/// <summary>
/// Wraps an event with metadata and context information.
/// Used for serialization, transmission, and audit trail.
/// Why: Decouples event payload from infrastructure concerns.
/// </summary>
public class EventEnvelope
{
    /// <summary>
    /// Unique identifier for this event.
    /// </summary>
    public string? EventId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Type of event (e.g., "user.created", "order.placed").
    /// </summary>
    public required string EventType { get; set; }

    /// <summary>
    /// Version of the event schema.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// The actual event payload.
    /// </summary>
    public required object Payload { get; set; }

    /// <summary>
    /// When the event was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for tracing across systems.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// ID of the cause event (for linked events).
    /// </summary>
    public string? CausationId { get; set; }

    /// <summary>
    /// Source system or service that generated the event.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// User or actor that caused the event.
    /// </summary>
    public string? Actor { get; set; }

    /// <summary>
    /// Additional metadata key-value pairs.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = [];

    /// <summary>
    /// Whether the event is a test event.
    /// </summary>
    public bool IsTestEvent { get; set; } = false;

    /// <summary>
    /// How many times this event has been processed.
    /// </summary>
    public int ProcessingAttempts { get; set; } = 0;

    /// <summary>
    /// Timeout for processing this event.
    /// </summary>
    public TimeSpan ProcessingTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Priority level (0-100, higher = more important).
    /// </summary>
    public int Priority { get; set; } = 50;

    /// <summary>
    /// Whether this is a critical event that must be processed.
    /// </summary>
    public bool IsCritical { get; set; } = false;

    /// <summary>
    /// Creates a new event envelope with default values.
    /// </summary>
    public static EventEnvelope Create(string eventType, object payload)
    {
        return new EventEnvelope
        {
            EventType = eventType,
            Payload = payload,
            EventId = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a linked event envelope (causally related).
    /// </summary>
    public static EventEnvelope CreateLinked(
        string eventType,
        object payload,
        string causationId,
        string? correlationId = null)
    {
        return new EventEnvelope
        {
            EventType = eventType,
            Payload = payload,
            EventId = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            CausationId = causationId,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString()
        };
    }

    /// <summary>
    /// Gets all header information for transmission.
    /// </summary>
    public Dictionary<string, string> GetHeaders()
    {
        var headers = new Dictionary<string, string>
        {
            { "X-Event-ID", EventId ?? "" },
            { "X-Event-Type", EventType },
            { "X-Event-Version", Version.ToString() },
            { "X-Created-At", CreatedAt.ToString("o") }
        };

        if (!string.IsNullOrEmpty(CorrelationId))
            headers["X-Correlation-ID"] = CorrelationId;

        if (!string.IsNullOrEmpty(Source))
            headers["X-Source"] = Source;

        if (!string.IsNullOrEmpty(Actor))
            headers["X-Actor"] = Actor;

        return headers;
    }

    /// <summary>
    /// Validates that the envelope has required fields.
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(EventType) && Payload != null;
    }
}

/// <summary>
/// Represents the result of event processing.
/// </summary>
public class EventProcessingResult
{
    public bool Success { get; set; }
    public string? EventId { get; set; }
    public string? Message { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public long ProcessingTimeMs { get; set; }
    public int RetryCount { get; set; }
    public Exception? Exception { get; set; }
}

/// <summary>
/// Batch of events to be processed together.
/// </summary>
public class EventBatch
{
    public string? BatchId { get; set; } = Guid.NewGuid().ToString();
    public List<EventEnvelope> Events { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int Priority { get; set; } = 50;

    public int EventCount => Events.Count;

    public static EventBatch Create(params EventEnvelope[] events)
    {
        return new EventBatch { Events = new List<EventEnvelope>(events) };
    }
}
