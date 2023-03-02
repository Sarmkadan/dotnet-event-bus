#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace DotnetEventBus.Advanced;

/// <summary>
/// Base class for event-sourced aggregates.
/// Maintains state by replaying events and supports snapshot loading for performance.
/// Why: Event sourcing provides complete audit trail and enables temporal queries.
/// </summary>
public abstract class EventSourcedAggregate
{
    private readonly List<object> _uncommittedEvents = [];
    private int _version = 0;

    public string? Id { get; protected set; }
    public int Version => _version;
    public IReadOnlyList<object> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    /// <summary>
    /// Applies an event to the aggregate, updating its state.
    /// </summary>
    protected void ApplyEvent(object @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        // Call the appropriate Apply method based on event type
        var method = GetType().GetMethod(
            "Apply",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new[] { @event.GetType() },
            null);

        if (method is not null)
        {
            method.Invoke(this, new[] { @event });
        }

        _version++;
    }

    /// <summary>
    /// Raises an event and applies it to the aggregate.
    /// </summary>
    protected void RaiseEvent(object @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        ApplyEvent(@event);
        _uncommittedEvents.Add(@event);
    }

    /// <summary>
    /// Loads events from history to restore aggregate state.
    /// </summary>
    public void LoadFromHistory(IEnumerable<object> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        foreach (var @event in events)
        {
            ApplyEvent(@event);
        }
    }

    /// <summary>
    /// Marks all uncommitted events as committed.
    /// </summary>
    public void CommitChanges()
    {
        _uncommittedEvents.Clear();
    }

    /// <summary>
    /// Loads a snapshot and restores the aggregate to that state.
    /// </summary>
    public void LoadSnapshot(AggregateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        Id = snapshot.AggregateId;
        _version = snapshot.Version;

        // Apply snapshot state via reflection
        var properties = GetType().GetProperties();
        foreach (var prop in properties)
        {
            if (snapshot.State.TryGetValue(prop.Name, out var value) && prop.CanWrite)
            {
                prop.SetValue(this, value);
            }
        }
    }

    /// <summary>
    /// Creates a snapshot of the current state.
    /// </summary>
    public AggregateSnapshot CreateSnapshot()
    {
        var state = new Dictionary<string, object?>();
        var properties = GetType().GetProperties();

        foreach (var prop in properties)
        {
            if (prop.CanRead)
            {
                state[prop.Name] = prop.GetValue(this);
            }
        }

        return new AggregateSnapshot
        {
            AggregateId = Id,
            AggregateType = GetType().Name,
            Version = _version,
            CreatedAt = DateTime.UtcNow,
            State = state
        };
    }
}

/// <summary>
/// Represents a snapshot of an aggregate's state at a point in time.
/// Used to optimize event replay by jumping to a known good state.
/// </summary>
public sealed class AggregateSnapshot
{
    public string? AggregateId { get; set; }
    public string? AggregateType { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object?> State { get; set; } = [];
}

/// <summary>
/// Configuration for event sourcing behavior.
/// </summary>
public sealed class EventSourcingOptions
{
    /// <summary>
    /// Number of events after which a snapshot should be created.
    /// </summary>
    public int SnapshotInterval { get; set; } = 100;

    /// <summary>
    /// Whether to automatically create snapshots.
    /// </summary>
    public bool EnableAutoSnapshots { get; set; } = true;

    /// <summary>
    /// Maximum number of events to load during replay.
    /// </summary>
    public int MaxEventsToReplay { get; set; } = 10000;

    /// <summary>
    /// Whether to validate event schema.
    /// </summary>
    public bool ValidateEventSchema { get; set; } = true;
}
