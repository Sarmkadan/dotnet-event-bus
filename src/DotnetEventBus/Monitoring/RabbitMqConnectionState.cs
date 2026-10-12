#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetEventBus.Monitoring;

/// <summary>
/// The state of the RabbitMQ connection behind a RabbitMQ event bus.
/// </summary>
public enum RabbitMqConnectionState
{
    /// <summary>The connection is open and usable.</summary>
    Open,

    /// <summary>The connection was lost and is being re-established.</summary>
    Reconnecting,

    /// <summary>The connection is closed and is not being re-established.</summary>
    Closed
}
