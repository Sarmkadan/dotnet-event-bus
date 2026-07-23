#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Threading;
using DotnetEventBus.Models;

namespace DotnetEventBus.Transport;

/// <summary>
/// Represents a transport mechanism for delivering events.
/// Transports can be in-process, webhook-based, or distributed (e.g., message brokers).
/// This abstraction enables unified handling of different delivery mechanisms through
/// circuit breakers, retry policies, and other cross-cutting concerns.
/// </summary>
public interface IEventTransport
{
    /// <summary>
    /// Gets the unique identifier for this transport.
    /// </summary>
    string TransportId { get; }

    /// <summary>
    /// Gets the transport type (e.g., "in-process", "webhook", "rabbitmq", "kafka").
    /// </summary>
    string TransportType { get; }

    /// <summary>
    /// Gets the capabilities supported by this transport.
    /// </summary>
    TransportCapabilities Capabilities { get; }

    /// <summary>
    /// Publishes an event envelope to the transport.
    /// </summary>
    /// <param name="envelope">The event envelope to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the publish operation result.</returns>
    Task<TransportPublishResult> PublishAsync(EventEnvelope envelope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of the transport.
    /// </summary>
    /// <returns>A transport status object.</returns>
    TransportStatus GetStatus();
}

/// <summary>
/// Represents the capabilities supported by an event transport.
/// </summary>
[Flags]
public enum TransportCapabilities
{
    /// <summary>
    /// The transport supports fire-and-forget event publishing.
    /// </summary>
    SupportsFireAndForget = 1 << 0,

    /// <summary>
    /// The transport supports request/reply pattern.
    /// </summary>
    SupportsRequestReply = 1 << 1,

    /// <summary>
    /// The transport supports batch publishing.
    /// </summary>
    SupportsBatching = 1 << 2,

    /// <summary>
    /// The transport supports priority-based message delivery.
    /// </summary>
    SupportsPriority = 1 << 3,

    /// <summary>
    /// The transport supports message persistence.
    /// </summary>
    SupportsPersistence = 1 << 4,

    /// <summary>
    /// The transport is in-process (events don't leave the application).
    /// </summary>
    IsInProcess = 1 << 5,

    /// <summary>
    /// The transport is remote (events are sent over network).
    /// </summary>
    IsRemote = 1 << 6
}

/// <summary>
/// Represents the result of a transport publish operation.
/// </summary>
public sealed class TransportPublishResult
{
    /// <summary>
    /// Gets whether the publish operation was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the event ID that was published.
    /// </summary>
    public string? EventId { get; }

    /// <summary>
    /// Gets any error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the exception that occurred, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the timestamp when the operation completed.
    /// </summary>
    public DateTime CompletedAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransportPublishResult"/> class.
    /// </summary>
    /// <param name="success">Whether the operation was successful.</param>
    /// <param name="eventId">The event ID that was published.</param>
    /// <param name="errorMessage">Error message if failed.</param>
    /// <param name="exception">Exception that occurred.</param>
    public TransportPublishResult(bool success, string? eventId, string? errorMessage, Exception? exception)
    {
        Success = success;
        EventId = eventId;
        ErrorMessage = errorMessage;
        Exception = exception;
    }

    /// <summary>
    /// Creates a successful transport publish result.
    /// </summary>
    public static TransportPublishResult SuccessResult(string eventId) =>
        new TransportPublishResult(true, eventId, null, null);

    /// <summary>
    /// Creates a failed transport publish result.
    /// </summary>
    public static TransportPublishResult FailedResult(string? eventId, Exception exception, string? errorMessage = null) =>
        new TransportPublishResult(false, eventId, errorMessage ?? exception.Message, exception);
}

/// <summary>
/// Represents the current status of an event transport.
/// </summary>
public sealed class TransportStatus
{
    /// <summary>
    /// Gets the transport identifier.
    /// </summary>
    public string TransportId { get; }

    /// <summary>
    /// Gets the transport type.
    /// </summary>
    public string TransportType { get; }

    /// <summary>
    /// Gets whether the transport is healthy.
    /// </summary>
    public bool IsHealthy { get; }

    /// <summary>
    /// Gets the current status message.
    /// </summary>
    public string? StatusMessage { get; }

    /// <summary>
    /// Gets the timestamp when the status was checked.
    /// </summary>
    public DateTime CheckedAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the number of messages published through this transport.
    /// </summary>
    public long MessagesPublished { get; }

    /// <summary>
    /// Gets the number of failed publishes.
    /// </summary>
    public long FailedPublishes { get; }

    /// <summary>
    /// Gets the average publish time in milliseconds.
    /// </summary>
    public double AveragePublishTimeMs { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransportStatus"/> class.
    /// </summary>
    public TransportStatus(
        string transportId,
        string transportType,
        bool isHealthy,
        string? statusMessage = null,
        long messagesPublished = 0,
        long failedPublishes = 0,
        double averagePublishTimeMs = 0)
    {
        TransportId = transportId ?? throw new ArgumentNullException(nameof(transportId));
        TransportType = transportType ?? throw new ArgumentNullException(nameof(transportType));
        IsHealthy = isHealthy;
        StatusMessage = statusMessage;
        MessagesPublished = messagesPublished;
        FailedPublishes = failedPublishes;
        AveragePublishTimeMs = averagePublishTimeMs;
    }
}