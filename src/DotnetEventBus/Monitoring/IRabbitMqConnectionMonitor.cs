#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetEventBus.Monitoring;

/// <summary>
/// Reports the current RabbitMQ connection state so that <see cref="RabbitMqBusHealthCheck"/> can read it.
/// A RabbitMQ event bus implements this interface. The library does not ship a RabbitMQ bus yet, so until a
/// monitor is registered the health check reports Unhealthy.
/// </summary>
public interface IRabbitMqConnectionMonitor
{
    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    RabbitMqConnectionState State { get; }
}
