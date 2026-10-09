#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Collections.Concurrent;
using DotnetEventBus.Integration;
using DotnetEventBus.Models;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Transport;

/// <summary>
/// Webhook transport that delivers events to external HTTP endpoints.
/// This transport enables integration with external systems and microservices.
/// </summary>
public sealed class WebhookTransport : IEventTransport
{
    /// <summary>Stable identifier reported by <see cref="TransportId"/>.</summary>
    private const string TransportIdValue = "webhook-transport";

    /// <summary>Transport type reported by <see cref="TransportType"/>.</summary>
    private const string TransportTypeValue = "webhook";

    /// <summary>Status message returned by <see cref="GetStatus"/> while the transport is healthy.</summary>
    private const string OperationalStatusMessage = "Webhook transport is operational";

    /// <summary>Zero value used as the default for metrics without samples.</summary>
    private const int ZeroMetricValue = 0;

    /// <summary>Failure message returned when the envelope fails validation.</summary>
    private const string InvalidEnvelopeMessage = "Invalid event envelope";

    /// <summary>Message of the exception attached to an invalid-envelope failure.</summary>
    private const string InvalidEnvelopeExceptionMessage = "Event envelope is not valid";

    /// <summary>Prefix of the exception message thrown when a webhook delivery fails.</summary>
    private const string DeliveryFailedMessagePrefix = "Webhook delivery failed: ";

    /// <summary>Handler that owns subscriptions and performs HTTP deliveries.</summary>
    private readonly WebhookHandler _webhookHandler;

    /// <summary>Optional logger; null disables logging.</summary>
    private readonly ILogger<WebhookTransport>? _logger;

    /// <summary>Thread-safe counters backing <see cref="GetStatus"/>.</summary>
    private readonly ConcurrentCounter _metrics = new ConcurrentCounter();

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookTransport"/> class.
    /// </summary>
    /// <param name="webhookHandler">The webhook handler to use for deliveries.</param>
    /// <param name="logger">Optional logger.</param>
    public WebhookTransport(WebhookHandler webhookHandler, ILogger<WebhookTransport>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(webhookHandler);
        _webhookHandler = webhookHandler;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string TransportId => TransportIdValue;

    /// <inheritdoc/>
    public string TransportType => TransportTypeValue;

    /// <inheritdoc/>
    public TransportCapabilities Capabilities => TransportCapabilities.SupportsFireAndForget
        | TransportCapabilities.SupportsBatching
        | TransportCapabilities.SupportsPriority
        | TransportCapabilities.IsRemote;

    /// <summary>
    /// Registers a webhook subscription with this transport.
    /// </summary>
    /// <param name="subscription">The webhook subscription to register.</param>
    public void Subscribe(WebhookSubscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        _webhookHandler.Subscribe(subscription);
        _logger?.LogInformation("Webhook subscription registered: {Url} for event types: {EventTypes}",
            subscription.Url, string.Join(", ", subscription.EventTypes));
    }

    /// <summary>
    /// Unregisters a webhook subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID to unregister.</param>
    /// <returns>True if the subscription was found and removed.</returns>
    public bool Unsubscribe(string subscriptionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);
        return _webhookHandler.Unsubscribe(subscriptionId);
    }

    /// <inheritdoc/>
    public async Task<TransportPublishResult> PublishAsync(
        EventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (!envelope.IsValid())
        {
            return TransportPublishResult.FailedResult(
                envelope.EventId ?? Guid.NewGuid().ToString(),
                new ArgumentException(InvalidEnvelopeExceptionMessage),
                InvalidEnvelopeMessage);
        }

        _metrics.IncrementMessagesPublished();
        var startTime = DateTime.UtcNow;

        try
        {
            // Get all webhook subscriptions that should receive this event type
            var applicableWebhooks = _webhookHandler.GetWebhooksForEvent(envelope.EventType);

            if (!applicableWebhooks.Any())
            {
                _logger?.LogDebug("No webhook subscriptions found for event type: {EventType}", envelope.EventType);
                return TransportPublishResult.SuccessResult(envelope.EventId ?? Guid.NewGuid().ToString());
            }

            // Deliver to each applicable webhook
            var deliveryTasks = applicableWebhooks.Select(webhook =>
                DeliverToWebhookAsync(webhook, envelope, cancellationToken));

            await Task.WhenAll(deliveryTasks);

            _metrics.RecordSuccess();
            var elapsedMs = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _metrics.RecordPublishTime(elapsedMs);

            _logger?.LogInformation(
                "Webhook transport published event {EventId} of type {EventType} to {Count} webhooks in {ElapsedMs}ms",
                envelope.EventId, envelope.EventType, applicableWebhooks.Count(), elapsedMs);

            return TransportPublishResult.SuccessResult(envelope.EventId ?? Guid.NewGuid().ToString());
        }
        catch (Exception ex)
        {
            _metrics.IncrementFailedPublishes();
            _logger?.LogError(ex, "Webhook transport failed to publish event {EventId} of type {EventType}",
                envelope.EventId, envelope.EventType);

            return TransportPublishResult.FailedResult(envelope.EventId ?? Guid.NewGuid().ToString(), ex);
        }
    }

    /// <summary>
    /// Delivers a single envelope to one webhook subscription, with retries handled by the webhook handler.
    /// </summary>
    /// <param name="webhook">The subscription to deliver to.</param>
    /// <param name="envelope">The event envelope being published.</param>
    /// <param name="cancellationToken">Token used to cancel the delivery.</param>
    /// <exception cref="InvalidOperationException">Thrown when the handler reports a failed delivery.</exception>
    private async Task DeliverToWebhookAsync(
        WebhookSubscription webhook,
        EventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        try
        {
            // Convert EventEnvelope to a simple object for webhook delivery
            var eventData = new
            {
                EventId = envelope.EventId,
                EventType = envelope.EventType,
                Version = envelope.Version,
                CreatedAt = envelope.CreatedAt,
                CorrelationId = envelope.CorrelationId,
                CausationId = envelope.CausationId,
                Source = envelope.Source,
                Actor = envelope.Actor,
                Metadata = envelope.Metadata,
                Payload = envelope.Payload,
                ProcessingAttempts = envelope.ProcessingAttempts,
                Priority = envelope.Priority,
                IsCritical = envelope.IsCritical
            };

            var deliveryResult = await _webhookHandler.DeliverEventAsync(
                webhook,
                eventData,
                envelope.EventType,
                onRetry: (attempt, ex, delay) =>
                {
                    _logger?.LogWarning(
                        "Webhook delivery retry {Attempt} for {Url}: {ExceptionMessage} (delay: {DelayMs}ms)",
                        attempt, webhook.Url, ex.Message, delay.TotalMilliseconds);
                    return Task.CompletedTask;
                }
            );

            if (!deliveryResult.Success)
            {
                _logger?.LogWarning(
                    "Webhook delivery failed for {Url}: {ErrorMessage}",
                    webhook.Url, deliveryResult.ErrorMessage);
                throw new InvalidOperationException($"{DeliveryFailedMessagePrefix}{deliveryResult.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to deliver event {EventId} to webhook {Url}", envelope.EventId, webhook.Url);
            throw;
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

    /// <summary>
    /// Reports health. Currently always true while the handler is available.
    /// </summary>
    /// <returns>True when the transport is considered healthy.</returns>
    private bool IsHealthy()
    {
        // Webhook transport is healthy if it can deliver to at least one webhook
        // For now, we consider it healthy as long as the webhook handler is available
        return true;
    }

    /// <summary>Human-readable status message included in <see cref="GetStatus"/>.</summary>
    private string? StatusMessage => OperationalStatusMessage;

    /// <summary>
    /// Simple thread-safe counter for transport metrics.
    /// </summary>
    private sealed class ConcurrentCounter
    {
        private long _messagesPublished;
        private long _failedPublishes;
        private double _totalPublishTimeMs;
        private long _publishCount;

        /// <summary>Gets the number of publish attempts, read atomically.</summary>
        public long MessagesPublished => Interlocked.Read(ref _messagesPublished);

        /// <summary>Gets the number of failed publishes, read atomically.</summary>
        public long FailedPublishes => Interlocked.Read(ref _failedPublishes);

        /// <summary>Gets the mean publish duration in milliseconds, or zero when no samples exist.</summary>
        public double AveragePublishTimeMs => _publishCount > ZeroMetricValue
            ? _totalPublishTimeMs / _publishCount
            : ZeroMetricValue;

        /// <summary>Increments the publish-attempt counter.</summary>
        public void IncrementMessagesPublished() => Interlocked.Increment(ref _messagesPublished);

        /// <summary>Increments the failed-publish counter.</summary>
        public void IncrementFailedPublishes() => Interlocked.Increment(ref _failedPublishes);

        /// <summary>Records a successful publish by incrementing the published counter.</summary>
        public void RecordSuccess() => Interlocked.Increment(ref _messagesPublished);

        /// <summary>Adds a duration sample used for the running average.</summary>
        /// <param name="milliseconds">Elapsed publish time in milliseconds.</param>
        public void RecordPublishTime(double milliseconds)
        {
            _totalPublishTimeMs += milliseconds;
            Interlocked.Increment(ref _publishCount);
        }
    }
}
