#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Collections.Concurrent;

namespace DotnetEventBus.Transport;

/// <summary>
/// Registry for managing multiple event transports and providing unified access.
/// </summary>
public interface ITransportRegistry
{
    /// <summary>
    /// Gets the default transport (typically in-process).
    /// </summary>
    IEventTransport DefaultTransport { get; }

    /// <summary>
    /// Gets all registered transports.
    /// </summary>
    IEnumerable<IEventTransport> GetAllTransports();

    /// <summary>
    /// Gets a transport by its ID.
    /// </summary>
    /// <param name="transportId">The transport ID to find.</param>
    /// <returns>The transport if found, otherwise null.</returns>
    IEventTransport? GetTransport(string transportId);

    /// <summary>
    /// Registers a transport with the registry.
    /// </summary>
    /// <param name="transport">The transport to register.</param>
    /// <param name="isDefault">Whether this transport should be the default.</param>
    void RegisterTransport(IEventTransport transport, bool isDefault = false);

    /// <summary>
    /// Gets the status of all transports.
    /// </summary>
    /// <returns>A dictionary of transport statuses keyed by transport ID.</returns>
    Dictionary<string, TransportStatus> GetAllStatuses();

    /// <summary>
    /// Gets the status of a specific transport.
    /// </summary>
    /// <param name="transportId">The transport ID to get status for.</param>
    /// <returns>The transport status, or null if not found.</returns>
    TransportStatus? GetTransportStatus(string transportId);
}

/// <summary>
/// Default implementation of ITransportRegistry.
/// </summary>
public sealed class TransportRegistry : ITransportRegistry
{
    private readonly ConcurrentDictionary<string, IEventTransport> _transports = new();
    private IEventTransport? _defaultTransport;

    /// <inheritdoc/>
    public IEventTransport DefaultTransport => _defaultTransport ?? throw new InvalidOperationException("No default transport registered");

    /// <inheritdoc/>
    public IEnumerable<IEventTransport> GetAllTransports() => _transports.Values;

    /// <inheritdoc/>
    public IEventTransport? GetTransport(string transportId) => _transports.TryGetValue(transportId, out var transport) ? transport : null;

    /// <inheritdoc/>
    public void RegisterTransport(IEventTransport transport, bool isDefault = false)
    {
        ArgumentNullException.ThrowIfNull(transport);

        _transports.AddOrUpdate(transport.TransportId, transport, (_, _) => transport);

        if (isDefault)
        {
            _defaultTransport = transport;
        }
    }

    /// <inheritdoc/>
    public Dictionary<string, TransportStatus> GetAllStatuses()
    {
        return _transports.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.GetStatus()
        );
    }

    /// <inheritdoc/>
    public TransportStatus? GetTransportStatus(string transportId)
    {
        return _transports.TryGetValue(transportId, out var transport)
            ? transport.GetStatus()
            : null;
    }
}