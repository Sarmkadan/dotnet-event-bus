#nullable enable

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
public sealed class EventEnvelope
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
    /// <param name="eventType">Type of event. Must not be null or empty.</param>
    /// <param name="payload">The event payload. Must not be null.</param>
    /// <returns>A new envelope with a fresh <see cref="EventId"/> and <see cref="CreatedAt"/> set to UTC now.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="eventType"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="payload"/> is null.</exception>
    public static EventEnvelope Create(string eventType, object payload)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventType);
        ArgumentNullException.ThrowIfNull(payload);
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
    /// <param name="eventType">Type of event. Must not be null or empty.</param>
    /// <param name="payload">The event payload. Must not be null.</param>
    /// <param name="causationId">ID of the event that caused this one. Must not be null or empty.</param>
    /// <param name="correlationId">Correlation ID to propagate. A new GUID is generated when null.</param>
    /// <returns>A new envelope with <see cref="CausationId"/> and <see cref="CorrelationId"/> set.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="eventType"/> or <paramref name="causationId"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="payload"/> is null.</exception>
    public static EventEnvelope CreateLinked(
        string eventType,
        object payload,
        string causationId,
        string? correlationId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventType);
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrEmpty(causationId);
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
    /// <returns>True when <see cref="EventType"/> is non-empty and <see cref="Payload"/> is not null; otherwise false.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(EventType) && Payload is not null;
    }

    /// <summary>
    /// Returns a concise, log-friendly summary of the envelope.
    /// The payload is intentionally omitted to avoid large or sensitive output.
    /// </summary>
    /// <returns>A string in the form "EventEnvelope { Id=..., Type=..., Version=..., CreatedAt=..., CorrelationId=..., Source=... }".</returns>
    public override string ToString()
    {
        return $"EventEnvelope {{ Id={EventId ?? "(none)"}, Type={EventType}, Version={Version}, " +
               $"CreatedAt={CreatedAt:o}, CorrelationId={CorrelationId ?? "(none)"}, Source={Source ?? "(none)"} }}";
    }
}

/// <summary>
/// Represents the result of event processing.
/// </summary>
public sealed class EventProcessingResult
{
    /// <summary>
    /// Whether the event was processed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ID of the processed event.
    /// </summary>
    public string? EventId { get; set; }

    /// <summary>
    /// Optional human-readable details about the outcome.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// When processing finished (UTC).
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Wall-clock processing duration in milliseconds.
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Number of retries performed before this result.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Exception raised during processing, if any.
    /// </summary>
    public Exception? Exception { get; set; }
}

/// <summary>
/// Batch of events to be processed together.
/// </summary>
public sealed class EventBatch
{
    /// <summary>
    /// Unique identifier for this batch.
    /// </summary>
    public string? BatchId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Events contained in this batch.
    /// </summary>
    public List<EventEnvelope> Events { get; set; } = [];

    /// <summary>
    /// When the batch was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Priority level (0-100, higher = more important).
    /// </summary>
    public int Priority { get; set; } = 50;

    /// <summary>
    /// Number of events in the batch.
    /// </summary>
    public int EventCount => Events.Count;

    /// <summary>
    /// Creates a new batch containing the given events.
    /// </summary>
    /// <param name="events">Events to include in the batch.</param>
    /// <returns>A new batch with a fresh <see cref="BatchId"/>.</returns>
    public static EventBatch Create(params EventEnvelope[] events)
    {
        return new EventBatch { Events = new List<EventEnvelope>(events) };
    }
}
