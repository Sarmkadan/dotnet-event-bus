#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace DotnetEventBus.Configuration;

/// <summary>
/// Configuration options for middleware components.
/// Centralizes all middleware settings in one place.
/// </summary>
public sealed class MiddlewareConfiguration
{
    /// <summary>
    /// Logging configuration.
    /// </summary>
    public LoggingOptions Logging { get; set; } = new();

    /// <summary>
    /// Error handling configuration.
    /// </summary>
    public ErrorHandlingOptions ErrorHandling { get; set; } = new();

    /// <summary>
    /// Rate limiting configuration.
    /// </summary>
    public RateLimitingOptions RateLimiting { get; set; } = new();

    /// <summary>
    /// Caching configuration.
    /// </summary>
    public CachingOptions Caching { get; set; } = new();
}

/// <summary>
/// Logging middleware configuration.
/// </summary>
public sealed class LoggingOptions
{
    /// <summary>
    /// Whether to log event payloads.
    /// </summary>
    public bool LogPayloads { get; set; } = false;

    /// <summary>
    /// Maximum payload size to log in bytes.
    /// </summary>
    public int MaxPayloadSizeBytes { get; set; } = 10240;

    /// <summary>
    /// Whether to redact sensitive data.
    /// </summary>
    public bool RedactSensitiveData { get; set; } = true;

    /// <summary>
    /// List of sensitive field names to redact.
    /// </summary>
    public List<string> SensitiveFields { get; set; } = new() { "password", "token", "apiKey", "secret" };

    /// <summary>
    /// Log level for events.
    /// </summary>
    public string LogLevel { get; set; } = "Information";
}

/// <summary>
/// Error handling middleware configuration.
/// </summary>
public sealed class ErrorHandlingOptions
{
    /// <summary>
    /// Maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in milliseconds.
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Whether to use exponential backoff.
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Backoff multiplier for exponential backoff.
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Whether to send failed events to dead letter queue.
    /// </summary>
    public bool EnableDeadLetterQueue { get; set; } = true;

    /// <summary>
    /// Custom error handler type name (must implement IErrorHandler).
    /// </summary>
    public string? CustomErrorHandlerType { get; set; }
}

/// <summary>
/// Rate limiting middleware configuration.
/// </summary>
public sealed class RateLimitingOptions
{
    /// <summary>
    /// Whether to enable rate limiting.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Number of requests allowed per window.
    /// </summary>
    public int RequestsPerWindow { get; set; } = 1000;

    /// <summary>
    /// Time window in seconds.
    /// </summary>
    public int WindowSizeSeconds { get; set; } = 60;

    /// <summary>
    /// Whether to limit per event type.
    /// </summary>
    public bool LimitPerEventType { get; set; } = true;

    /// <summary>
    /// Custom limits per event type (eventType -> requestsPerWindow).
    /// </summary>
    public Dictionary<string, int> EventTypeSpecificLimits { get; set; } = [];
}

/// <summary>
/// Caching configuration.
/// </summary>
public sealed class CachingOptions
{
    /// <summary>
    /// Whether to enable caching.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of cached items.
    /// </summary>
    public int MaxCacheSize { get; set; } = 10000;

    /// <summary>
    /// Default cache expiration time in minutes.
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Cache type: "memory" or "distributed".
    /// </summary>
    public string CacheType { get; set; } = "memory";

    /// <summary>
    /// Whether to use compression for cached items.
    /// </summary>
    public bool UseCompression { get; set; } = false;

    /// <summary>
    /// Cache key prefix.
    /// </summary>
    public string KeyPrefix { get; set; } = "eventbus:";
}
