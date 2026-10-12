#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetEventBus.Configuration;

/// <summary>
/// Connection settings for the RabbitMQ overload of <c>AddEventBus</c>.
/// </summary>
public sealed class RabbitMqOptions
{
    /// <summary>
    /// Gets or sets the AMQP connection string, e.g. <c>amqp://user:password@host:5672/</c>.
    /// Required: registration fails fast when this is empty.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the broker host name. Must not be empty.
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the broker port. Must be between 1 and 65535.
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Gets or sets the name of the exchange events are published to. Must not be empty.
    /// </summary>
    public string ExchangeName { get; set; } = "dotnet-event-bus";

    /// <summary>
    /// Gets or sets the number of unacknowledged messages a consumer may hold at once. Must be greater than zero.
    /// </summary>
    public int PrefetchCount { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum number of times a failed message is retried. Cannot be negative.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay for the exponential backoff between retries. Cannot be negative.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or sets the multiplier applied to the retry delay on each attempt. Must be at least 1.0.
    /// </summary>
    public double RetryDelayMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets the upper bound for any single retry delay. Must not be less than <see cref="RetryDelay"/>.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
}
