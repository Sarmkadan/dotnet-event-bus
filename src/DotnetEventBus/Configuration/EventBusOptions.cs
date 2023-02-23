// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetEventBus.Configuration;

/// <summary>
/// Configuration options for the event bus.
/// </summary>
public class EventBusOptions
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
    /// Validates the configuration options.
    /// Throws if any configuration is invalid.
    /// </summary>
    public void Validate()
    {
        if (DefaultHandlerTimeout <= TimeSpan.Zero)
            throw new ArgumentException(
                "DefaultHandlerTimeout must be greater than zero",
                nameof(DefaultHandlerTimeout));

        if (MaxRetryAttempts < 0)
            throw new ArgumentException(
                "MaxRetryAttempts cannot be negative",
                nameof(MaxRetryAttempts));

        if (RetryDelay < TimeSpan.Zero)
            throw new ArgumentException(
                "RetryDelay cannot be negative",
                nameof(RetryDelay));

        if (RetryDelayMultiplier < 1.0)
            throw new ArgumentException(
                "RetryDelayMultiplier must be at least 1.0",
                nameof(RetryDelayMultiplier));

        if (MaxConcurrentHandlers < 1)
            throw new ArgumentException(
                "MaxConcurrentHandlers must be at least 1",
                nameof(MaxConcurrentHandlers));

        if (IsDistributed && string.IsNullOrWhiteSpace(DistributedTransportType))
            throw new ArgumentException(
                "DistributedTransportType must be specified when IsDistributed is true",
                nameof(DistributedTransportType));
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
        return new EventBusOptions
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
            IsDistributed = IsDistributed,
            DistributedTransportType = DistributedTransportType,
            DistributedTransportConnectionString = DistributedTransportConnectionString
        };
    }
}
