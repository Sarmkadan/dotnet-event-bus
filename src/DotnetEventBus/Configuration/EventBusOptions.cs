#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetEventBus.Exceptions;

namespace DotnetEventBus.Configuration;

/// <summary>
/// Configuration options for the event bus.
/// </summary>
public sealed class EventBusOptions
{
    /// <summary>
    /// Default timeout for synchronous handler execution.
    /// </summary>
    public TimeSpan DefaultHandlerTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum number of times a message will be retried on failure.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay for exponential backoff retry strategy.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Multiplier for exponential backoff retry strategy.
    /// </summary>
    public double RetryDelayMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Maximum delay between retries.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether to use parallel handler invocation for a single event.
    /// </summary>
    public bool AllowParallelHandling { get; set; } = true;

    /// <summary>
    /// Maximum number of concurrent handlers that can execute in parallel.
    /// </summary>
    public int MaxConcurrentHandlers { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Whether to automatically send failed messages to dead letter queue.
    /// </summary>
    public bool EnableDeadLetterQueue { get; set; } = true;

    /// <summary>
    /// Whether to throw exceptions from handlers or catch and log them.
    /// </summary>
    public bool ThrowOnHandlerFailure { get; set; } = false;

    /// <summary>
    /// Whether publishing an event with no registered handlers should throw
    /// <see cref="NoHandlersRegisteredException"/>. When <see langword="false"/> (the default),
    /// the situation is logged and, if <see cref="DeadLetterOnNoHandlers"/> is enabled,
    /// recorded to the dead letter queue instead.
    /// </summary>
    public bool ThrowOnNoHandlers { get; set; } = false;

    /// <summary>
    /// Whether an event published with no registered handlers should be recorded in the
    /// dead letter queue for later inspection or replay. Has no effect if
    /// <see cref="EnableDeadLetterQueue"/> is <see langword="false"/> or if
    /// <see cref="ThrowOnNoHandlers"/> is <see langword="true"/> (the throw takes precedence).
    /// </summary>
    public bool DeadLetterOnNoHandlers { get; set; } = false;

    /// <summary>
    /// Whether this is a distributed event bus.
    /// </summary>
    public bool IsDistributed { get; set; } = false;

    /// <summary>
    /// Transport type for distributed messaging (e.g., RabbitMQ, Kafka, Azure Service Bus).
    /// </summary>
    public string? DistributedTransportType { get; set; }

    /// <summary>
    /// Connection string for distributed transport.
    /// </summary>
    public string? DistributedTransportConnectionString { get; set; }

    /// <summary>
    /// List of middleware types to be executed in the event bus pipeline.
    /// </summary>
    public List<Type> MiddlewareTypes { get; } = new List<Type>();

    /// <summary>
    /// Default timeout for request/reply operations when no per-request timeout is configured.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Validates the configuration options.
    /// Throws if any configuration is invalid.
    /// </summary>
    public void Validate()
    {
        if (RequestTimeout <= TimeSpan.Zero)
            throw new ValidationException("RequestTimeout must be greater than zero");

        if (MaxRetryAttempts < 0)
            throw new ValidationException("MaxRetryAttempts cannot be negative");

        if (RetryDelay < TimeSpan.Zero)
            throw new ValidationException("RetryDelay cannot be negative");

        if (RetryDelayMultiplier < 1.0)
            throw new ValidationException("RetryDelayMultiplier must be at least 1.0");

        if (MaxConcurrentHandlers < 1)
            throw new ValidationException("MaxConcurrentHandlers must be at least 1");

        if (IsDistributed && string.IsNullOrWhiteSpace(DistributedTransportType))
            throw new ValidationException(
                "DistributedTransportType must be specified when IsDistributed is true");
    }

    /// <summary>
    /// Calculates the retry delay for a given attempt number using exponential backoff.
    /// </summary>
    public TimeSpan CalculateRetryDelay(int attemptNumber)
    {
        if (attemptNumber < 0)
            throw new ArgumentException("Attempt number cannot be negative", nameof(attemptNumber));

        var delay = TimeSpan.FromMilliseconds(
            RetryDelay.TotalMilliseconds * Math.Pow(RetryDelayMultiplier, attemptNumber));

        return delay > MaxRetryDelay ? MaxRetryDelay : delay;
    }

    /// <summary>
    /// Creates a copy of these options.
    /// </summary>
    public EventBusOptions Clone()
    {
        var clone = new EventBusOptions
        {
            DefaultHandlerTimeout = DefaultHandlerTimeout,
            MaxRetryAttempts = MaxRetryAttempts,
            RetryDelay = RetryDelay,
            RetryDelayMultiplier = RetryDelayMultiplier,
            MaxRetryDelay = MaxRetryDelay,
            AllowParallelHandling = AllowParallelHandling,
            MaxConcurrentHandlers = MaxConcurrentHandlers,
            EnableDeadLetterQueue = EnableDeadLetterQueue,
            ThrowOnHandlerFailure = ThrowOnHandlerFailure,
            ThrowOnNoHandlers = ThrowOnNoHandlers,
            DeadLetterOnNoHandlers = DeadLetterOnNoHandlers,
            IsDistributed = IsDistributed,
            DistributedTransportType = DistributedTransportType,
            DistributedTransportConnectionString = DistributedTransportConnectionString,
            RequestTimeout = RequestTimeout
        };
        clone.MiddlewareTypes.AddRange(MiddlewareTypes);
        return clone;
    }
}
