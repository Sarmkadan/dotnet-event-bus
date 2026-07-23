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
    private readonly WebhookHandler _webhookHandler;
    private readonly ILogger<WebhookTransport>? _logger;
    private readonly ConcurrentCounter _metrics = new ConcurrentCounter();

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookTransport"/> class.
    /// </summary>
    /// <param name="webhookHandler">The webhook handler to use for deliveries.</param>
    /// <param name="logger">Optional logger.</param>
    public WebhookTransport(WebhookHandler webhookHandler, ILogger<WebhookTransport>? logger = null)
    {
        _webhookHandler = webhookHandler ?? throw new ArgumentNullException(nameof(webhookHandler));
        _logger = logger;
    }

    /// <inheritdoc/>
    public string TransportId => "webhook-transport";

    /// <inheritdoc/>
    public string TransportType => "webhook";

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

            _logger?.LogInformation("Webhook transport published event {EventId} of type {EventType} to {Count} webhooks in {ElapsedMs}ms",
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

    private async Task DeliverToWebhookAsync(WebhookSubscription webhook, EventEnvelope envelope, CancellationToken cancellationToken)
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
                    _logger?.LogWarning("Webhook delivery retry {Attempt} for {Url}: {ExceptionMessage} (delay: {DelayMs}ms)",
                        attempt, webhook.Url, ex.Message, delay.TotalMilliseconds);
                    return Task.CompletedTask;
                }
            );

            if (!deliveryResult.Success)
            {
                _logger?.LogWarning("Webhook delivery failed for {Url}: {ErrorMessage}", webhook.Url, deliveryResult.ErrorMessage);
                throw new InvalidOperationException($"Webhook delivery failed: {deliveryResult.ErrorMessage}");
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

    private bool IsHealthy()
    {
        // Webhook transport is healthy if it can deliver to at least one webhook
        // For now, we consider it healthy as long as the webhook handler is available
        return true;
    }

    private string? StatusMessage => "Webhook transport is operational";

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