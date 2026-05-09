// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetEventBus.Models;

/// <summary>
/// Represents a message published to the event bus.
/// </summary>
public class EventMessage
{
    /// <summary>
    /// Unique identifier for this message (alias for MessageId, required by repository infrastructure).
    /// </summary>
    public string Id => MessageId;

    /// <summary>
    /// Unique identifier for this message.
    /// </summary>
    public string MessageId { get; set; }

    /// <summary>
    /// The fully qualified type name of the event payload.
    /// </summary>
    public string EventType { get; set; }

    /// <summary>
    /// Timestamp when the message was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// The event payload as a serialized string (typically JSON).
    /// </summary>
    public string Payload { get; set; }

    /// <summary>
    /// Correlation ID for tracing related messages.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// The source/origin of this message.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Optional metadata headers attached to the message.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; }

    /// <summary>
    /// Indicates if this is an in-process message or distributed.
    /// </summary>
    public MessageScope Scope { get; set; }

    /// <summary>
    /// Number of times this message has been attempted to be processed.
    /// </summary>
    public int ProcessingAttempts { get; set; }

    /// <summary>
    /// Initializes a new instance of the EventMessage class.
    /// </summary>
    public EventMessage(string eventType, string payload)
    {
        MessageId = Guid.NewGuid().ToString();
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        CreatedAtUtc = DateTime.UtcNow;
        Headers = new Dictionary<string, string>();
        Scope = MessageScope.InProcess;
        ProcessingAttempts = 0;
    }

    /// <summary>
    /// Validates the message properties.
    /// Throws if any critical properties are invalid.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(MessageId))
            throw new ArgumentException("MessageId cannot be empty", nameof(MessageId));

        if (string.IsNullOrWhiteSpace(EventType))
            throw new ArgumentException("EventType cannot be empty", nameof(EventType));

        if (string.IsNullOrWhiteSpace(Payload))
            throw new ArgumentException("Payload cannot be empty", nameof(Payload));
    }

    /// <summary>
    /// Creates a copy of this message with a new MessageId for retry scenarios.
    /// </summary>
    public EventMessage CreateRetry()
    {
        return new EventMessage(EventType, Payload)
        {
            CorrelationId = CorrelationId,
            Source = Source,
            Scope = Scope,
            ProcessingAttempts = ProcessingAttempts + 1,
            Headers = new Dictionary<string, string>(Headers)
        };
    }

    /// <summary>
    /// Adds a header to the message.
    /// </summary>
    public void AddHeader(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Header key cannot be empty", nameof(key));

        Headers[key] = value;
    }

    /// <summary>
    /// Gets a header value by key, returning null if not found.
    /// </summary>
    public string? GetHeader(string key)
    {
        return Headers.TryGetValue(key, out var value) ? value : null;
    }
}

/// <summary>
/// Defines the scope of a message in the event bus.
/// </summary>
public enum MessageScope
{
    /// <summary>
    /// Message is processed within the same process/application.
    /// </summary>
    InProcess = 0,

    /// <summary>
    /// Message is distributed across multiple processes/machines.
    /// </summary>
    Distributed = 1,

    /// <summary>
    /// Message is a request that expects a reply.
    /// </summary>
    RequestReply = 2
}
