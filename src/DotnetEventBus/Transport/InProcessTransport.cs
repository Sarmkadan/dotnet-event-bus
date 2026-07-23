#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotnetEventBus.Models;
using DotnetEventBus.Services;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Transport;

/// <summary>
/// In-process transport that delivers events within the same application domain.
/// This is the default transport for the event bus when no distributed transport is configured.
/// </summary>
public sealed class InProcessTransport : IEventTransport
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<InProcessTransport>? _logger;
    private readonly ConcurrentCounter _metrics = new ConcurrentCounter();

    /// <summary>
    /// Initializes a new instance of the <see cref="InProcessTransport"/> class.
    /// </summary>
    /// <param name="eventBus">The in-process event bus to delegate to.</param>
    /// <param name="logger">Optional logger.</param>
    public InProcessTransport(IEventBus eventBus, ILogger<InProcessTransport>? logger = null)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger;
    }

    /// <inheritdoc/>
    public string TransportId => "in-process-transport";

    /// <inheritdoc/>
    public string TransportType => "in-process";

    /// <inheritdoc/>
    public TransportCapabilities Capabilities => TransportCapabilities.SupportsFireAndForget
        | TransportCapabilities.SupportsRequestReply
        | TransportCapabilities.SupportsBatching
        | TransportCapabilities.SupportsPriority
        | TransportCapabilities.SupportsPersistence
        | TransportCapabilities.IsInProcess;

    /// <inheritdoc/>
    public async Task<TransportPublishResult> PublishAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (envelope is null)
            throw new ArgumentNullException(nameof(envelope));

        if (!envelope.IsValid())
            return TransportPublishResult.FailedResult(envelope.EventId ?? Guid.NewGuid().ToString(), new ArgumentException("Event envelope is not valid"), "Invalid event envelope");

        _metrics.IncrementMessagesPublished();
        var startTime = DateTime.UtcNow;

        try
        {
            // Convert EventEnvelope to the internal event bus format
            var @event = envelope.Payload;
            var eventType = envelope.EventType;
            var correlationId = envelope.CorrelationId;

            var result = await _eventBus.PublishAsync(@event, @event.GetType(), correlationId, cancellationToken);

            _metrics.RecordSuccess();
            var elapsedMs = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _metrics.RecordPublishTime(elapsedMs);

            _logger?.LogDebug("In-process transport published event {EventId} of type {EventType} in {ElapsedMs}ms",
                envelope.EventId, eventType, elapsedMs);

            return TransportPublishResult.SuccessResult(envelope.EventId ?? result.MessageId);
        }
        catch (Exception ex)
        {
            _metrics.IncrementFailedPublishes();
            _logger?.LogError(ex, "In-process transport failed to publish event {EventId} of type {EventType}",
                envelope.EventId, envelope.EventType);

            return TransportPublishResult.FailedResult(envelope.EventId ?? Guid.NewGuid().ToString(), ex);
        }
    }

    /// <inheritdoc/>
    public TransportStatus GetStatus()
    {
        return new TransportStatus(
            TransportId,
            TransportType,
            IsHealthy(),
            StatusMessage,
            _metrics.MessagesPublished,
            _metrics.FailedPublishes,
            _metrics.AveragePublishTimeMs
        );
    }

    private bool IsHealthy()
    {
        // In-process transport is always healthy as long as the event bus is available
        return true;
    }

    private string? StatusMessage => "In-process transport is operational";

    /// <summary>
    /// Simple thread-safe counter for transport metrics.
    /// </summary>
    private sealed class ConcurrentCounter
    {
        private long _messagesPublished;
        private long _failedPublishes;
        private double _totalPublishTimeMs;
        private long _publishCount;

        public long MessagesPublished => Interlocked.Read(ref _messagesPublished);
        public long FailedPublishes => Interlocked.Read(ref _failedPublishes);
        public double AveragePublishTimeMs => _publishCount > 0 ? _totalPublishTimeMs / _publishCount : 0;

        public void IncrementMessagesPublished() => Interlocked.Increment(ref _messagesPublished);
        public void IncrementFailedPublishes() => Interlocked.Increment(ref _failedPublishes);
        public void RecordSuccess() => Interlocked.Increment(ref _messagesPublished);
        public void RecordPublishTime(double milliseconds)
        {
            _totalPublishTimeMs += milliseconds;
            Interlocked.Increment(ref _publishCount);
        }
    }
}