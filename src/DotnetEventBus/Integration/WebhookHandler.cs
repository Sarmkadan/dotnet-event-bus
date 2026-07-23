#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

    /// <summary>
    /// Delivers an event to a webhook endpoint with retry policy.
    /// Applies exponential backoff for transient failures (5xx, timeouts, network errors).
    /// Does not retry on client errors (4xx) except for specific cases like 408 (Request Timeout).
    /// </summary>
    /// <param name="subscription">The webhook subscription to deliver to.</param>
    /// <param name="eventData">The event data to send.</param>
    /// <param name="eventType">The type of event being delivered.</param>
    /// <param name="onRetry">Optional callback invoked before each retry attempt.</param>
    /// <returns>Delivery result containing success status and error information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when subscription or eventData is null.</exception>
    /// <exception cref="HandlerInvocationException">Thrown when all retry attempts are exhausted.</exception>
    public async Task<WebhookDeliveryResult> DeliverEventAsync(
        WebhookSubscription subscription,
        object eventData,
        string eventType,
        Func<int, Exception, TimeSpan, Task>? onRetry = null)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentNullException.ThrowIfNull(eventData);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        subscription.LastDeliveryAt = DateTime.UtcNow;
        subscription.LastDeliveryStatus = null;
        subscription.LastDeliveryStatusCode = null;
        subscription.LastError = null;

        _logger?.LogInformation("Delivering event {EventType} to webhook {Url}", eventType, subscription.Url);

        try
        {
            var retryPolicy = new RetryPolicy()
                .WithMaxRetries(subscription.MaxRetryAttempts)
                .WithInitialDelay(subscription.InitialRetryDelay)
                .WithBackoffMultiplier(subscription.RetryBackoffMultiplier)
                .WithMaxDelay(subscription.MaxRetryDelay)
                .WithJitter(subscription.UseJitter)
                .WithRetryableExceptionFilter(IsTransientFailure);

            await retryPolicy.ExecuteAsync(async () =>
            {
                await DeliverOnceAsync(subscription, eventData, eventType);
            }, onRetry);

            subscription.LastDeliveryStatus = "Success";
            _logger?.LogInformation("Webhook delivery successful to {Url}", subscription.Url);
            return new WebhookDeliveryResult(true, null, null, 0);
        }
        catch (Exception ex) when (ex is not HandlerInvocationException)
        {
            subscription.LastError = ex.Message;
            _logger?.LogError(ex, "Webhook delivery failed after all retry attempts to {Url}", subscription.Url);
            throw new HandlerInvocationException($"Webhook delivery failed after all retry attempts to {subscription.Url}. Error: {ex.Message}", ex, subscription.MaxRetryAttempts);
        }
        catch (HandlerInvocationException)
        {
            throw;
        }
    }

    private async Task DeliverOnceAsync(
        WebhookSubscription subscription,
        object eventData,
        string eventType)
    {
        using var httpClient = new HttpClient();

        // Add custom headers
        if (subscription.Headers is not null)
        {
            foreach (var header in subscription.Headers)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        // Add event type header
        httpClient.DefaultRequestHeaders.Add("X-Event-Type", eventType);

        var content = new StringContent(
            JsonSerializer.Serialize(eventData),
            Encoding.UTF8,
            "application/json");

        try
        {
            var response = await httpClient.PostAsync(subscription.Url, content);
            subscription.LastDeliveryStatusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                _logger?.LogDebug("Webhook delivery successful with status {StatusCode} to {Url}", response.StatusCode, subscription.Url);
                return;
            }

            // Read response content for error details
            var responseContent = await response.Content.ReadAsStringAsync();

            // Don't retry on client errors (4xx) except timeout
            var statusCodeInt = (int)response.StatusCode;
            if (statusCodeInt >= 400 && statusCodeInt < 500 && statusCodeInt != 408)
            {
                _logger?.LogWarning("Webhook delivery failed with client error {StatusCode} to {Url}: {Response}", response.StatusCode, subscription.Url, responseContent);
                throw new HttpRequestException($"Client error: {statusCodeInt}") { Data = { ["StatusCode"] = statusCodeInt, ["Response"] = responseContent } };
            }

            // For server errors (5xx) and timeout (408), retry
            _logger?.LogWarning("Webhook delivery failed with status {StatusCode} to {Url}, will retry", response.StatusCode, subscription.Url);
            throw new HttpRequestException($"Server error: {statusCodeInt}") { Data = { ["StatusCode"] = statusCodeInt, ["Response"] = responseContent } };
        }
        catch (HttpRequestException ex) when (ex.Data.Contains("StatusCode"))
        {
            var statusCode = (int)ex.Data["StatusCode"];
            var responseContent = ex.Data["Response"] as string ?? string.Empty;

            // Don't retry on client errors (except 408)
            if (statusCode >= 400 && statusCode < 500 && statusCode != 408)
            {
                throw new HttpRequestException($"Client error {statusCode} - not retryable", ex) { Data = { ["StatusCode"] = statusCode, ["Response"] = responseContent } };
            }

            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger?.LogWarning(ex, "Webhook delivery timed out to {Url}, will retry", subscription.Url);
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Webhook delivery failed to {Url}, will retry", subscription.Url);
            throw;
        }
    }

    private bool IsTransientFailure(Exception ex)
    {
        // Retry on network errors, timeouts, and server errors
        if (ex is HttpRequestException httpEx && httpEx.Data.Contains("StatusCode"))
        {
            var statusCode = (int)httpEx.Data["StatusCode"];
            // Retry on 5xx, 408 (Request Timeout), and network errors
            return statusCode >= 500 || statusCode == 408;
        }

        // Retry on timeout exceptions
        if (ex is TaskCanceledException taskEx && taskEx.InnerException is TimeoutException)
        {
            return true;
        }

        // Retry on network exceptions
        if (ex is HttpRequestException || ex is System.Net.Sockets.SocketException)
        {
            return true;
        }

        return false;
    }
}

/// <summary>
/// Exception thrown when webhook delivery fails after all retry attempts.
/// </summary>
public sealed class HandlerInvocationException : Exception
{
    /// <summary>
    /// Gets the number of retry attempts that were made.
    /// </summary>
    public int RetryAttempts { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HandlerInvocationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="retryAttempts">The number of retry attempts that were made.</param>
    public HandlerInvocationException(string message, Exception? innerException, int retryAttempts)
        : base(message, innerException)
    {
        RetryAttempts = retryAttempts;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HandlerInvocationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="retryAttempts">The number of retry attempts that were made.</param>
    public HandlerInvocationException(string message, int retryAttempts)
        : base(message)
    {
        RetryAttempts = retryAttempts;
    }
}

/// <summary>
/// Result of a webhook delivery attempt.
/// </summary>
public sealed class WebhookDeliveryResult
{
    /// <summary>
    /// Gets whether the delivery was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets any error message if the delivery failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the HTTP status code if available.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Gets the exception that occurred, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookDeliveryResult"/> class.
    /// </summary>
    /// <param name="success">Whether the delivery was successful.</param>
    /// <param name="errorMessage">Error message if failed.</param>
    /// <param name="exception">Exception that occurred.</param>
    /// <param name="statusCode">HTTP status code.</param>
    public WebhookDeliveryResult(bool success, string? errorMessage, Exception? exception, int? statusCode)
    {
        Success = success;
        ErrorMessage = errorMessage;
        Exception = exception;
        StatusCode = statusCode;
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
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public double RetryBackoffMultiplier { get; set; } = 2.0;
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(5);
    public bool UseJitter { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastDeliveryAt { get; set; }
    public string? LastDeliveryStatus { get; set; }
    public int? LastDeliveryStatusCode { get; set; }
    public string? LastError { get; set; }
}
