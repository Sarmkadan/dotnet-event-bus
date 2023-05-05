#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Integration;

/// <summary>
/// Manages webhook subscriptions and event routing to external endpoints.
/// Provides signature verification for security and filtering capabilities.
/// Why: Webhooks allow external systems to react to events in near real-time.
/// </summary>
public sealed class WebhookHandler
{
    private readonly List<WebhookSubscription> _subscriptions = [];
    private readonly string? _signingSecret;
    private readonly ILogger<WebhookHandler>? _logger;

    public WebhookHandler(string? signingSecret = null, ILogger<WebhookHandler>? logger = null)
    {
        _signingSecret = signingSecret;
        _logger = logger;
    }

    /// <summary>
    /// Registers a webhook endpoint for specific event types.
    /// </summary>
    public void Subscribe(WebhookSubscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        subscription.Id = subscription.Id ?? Guid.NewGuid().ToString();
        _subscriptions.Add(subscription);

        _logger?.LogInformation("Webhook subscription registered: {Id} for event types: {EventTypes}",
            subscription.Id, string.Join(", ", subscription.EventTypes));
    }

    /// <summary>
    /// Unregisters a webhook endpoint.
    /// </summary>
    public bool Unsubscribe(string subscriptionId)
    {
        var subscription = _subscriptions.FirstOrDefault(s => s.Id == subscriptionId);
        if (subscription is null)
            return false;

        _subscriptions.Remove(subscription);

        _logger?.LogInformation("Webhook subscription unregistered: {Id}", subscriptionId);
        return true;
    }

    /// <summary>
    /// Gets all webhooks that should receive an event.
    /// Filters by event type and active status.
    /// </summary>
    public IEnumerable<WebhookSubscription> GetWebhooksForEvent(string eventType)
    {
        return _subscriptions.Where(s =>
            s.IsActive &&
            (s.EventTypes.Contains("*") || s.EventTypes.Contains(eventType)));
    }

    /// <summary>
    /// Generates a signature for webhook validation.
    /// Uses HMAC-SHA256 for security.
    /// </summary>
    public string GenerateSignature(string payload)
    {
        if (string.IsNullOrEmpty(_signingSecret))
            return string.Empty;

        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_signingSecret)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash);
        }
    }

    /// <summary>
    /// Verifies a webhook signature for authenticity.
    /// </summary>
    public bool VerifySignature(string payload, string providedSignature)
    {
        if (string.IsNullOrEmpty(_signingSecret))
            return true; // No verification if no secret

        var expectedSignature = GenerateSignature(payload);
        return expectedSignature == providedSignature;
    }

    /// <summary>
    /// Gets all registered webhooks.
    /// </summary>
    public IEnumerable<WebhookSubscription> GetAllSubscriptions()
    {
        return _subscriptions.ToList();
    }

    /// <summary>
    /// Updates a webhook subscription.
    /// </summary>
    public bool UpdateSubscription(string subscriptionId, Action<WebhookSubscription> updates)
    {
        var subscription = _subscriptions.FirstOrDefault(s => s.Id == subscriptionId);
        if (subscription is null)
            return false;

        updates(subscription);

        _logger?.LogInformation("Webhook subscription updated: {Id}", subscriptionId);
        return true;
    }
}

/// <summary>
/// Represents a webhook subscription for event delivery.
/// </summary>
public sealed class WebhookSubscription
{
    public string? Id { get; set; }
    public required string Url { get; set; }
    public List<string> EventTypes { get; set; } = [];
    public Dictionary<string, string> Headers { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public int RetryCount { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastDeliveryAt { get; set; }
    public string? LastDeliveryStatus { get; set; }
}
