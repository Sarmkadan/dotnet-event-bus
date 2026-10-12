#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetEventBus.Configuration;

/// <summary>
/// Marker registered by each <c>AddEventBus</c> overload to record which bus implementation the
/// service collection was configured with. Lets a second, different implementation fail fast.
/// </summary>
internal sealed class EventBusRegistration
{
    /// <summary>Kind name for the in-process bus registered by <c>AddEventBus</c>.</summary>
    public const string InMemory = "InMemory";

    /// <summary>Kind name for the RabbitMQ bus overload of <c>AddEventBus</c>.</summary>
    public const string RabbitMq = "RabbitMq";

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBusRegistration"/> class.
    /// </summary>
    /// <param name="kind">The bus implementation kind, e.g. <see cref="InMemory"/>.</param>
    public EventBusRegistration(string kind)
    {
        Kind = kind;
    }

    /// <summary>
    /// Gets the bus implementation kind.
    /// </summary>
    public string Kind { get; }
}
