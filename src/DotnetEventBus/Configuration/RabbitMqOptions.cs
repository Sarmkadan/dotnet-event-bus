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
}
