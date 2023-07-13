#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DotnetEventBus.Advanced;

/// <summary>
/// Extension methods for <see cref="EventSourcedAggregate"/> providing common operations
/// for event-sourced aggregates.
/// </summary>
public static class EventSourcedAggregateExtensions
{
    /// <summary>
    /// Creates a deep clone of the aggregate by replaying its uncommitted events.
    /// Useful for testing, backup, or creating aggregate copies without shared state.
    /// </summary>
    /// <param name="aggregate">The aggregate to clone.</param>
    /// <returns>A new aggregate instance with the same state.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="aggregate"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the aggregate type cannot be instantiated.</exception>
    public static EventSourcedAggregate DeepClone(this EventSourcedAggregate aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        // Create a new instance using the same type
        var clone = (EventSourcedAggregate)Activator.CreateInstance(aggregate.GetType()
            ?? throw new InvalidOperationException("Cannot create instance of aggregate type"));

        // Copy Id if set using reflection since it's protected
        if (aggregate.Id is not null)
        {
            var idProperty = aggregate.GetType().GetProperty("Id");
            idProperty?.SetValue(clone, aggregate.Id);
        }

        // Clone uncommitted events if any
        foreach (var @event in aggregate.UncommittedEvents)
        {
            // Use reflection to call the protected RaiseEvent method
            var raiseMethod = aggregate.GetType().GetMethod(
                "RaiseEvent",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            raiseMethod?.Invoke(clone, new[] { @event });
        }

        return clone;
    }

    /// <summary>
    /// Replays a sequence of events to restore the aggregate state.
    /// This is useful when you have a stream of events that need to be replayed
    /// without going through the normal event sourcing pipeline.
    /// </summary>
    /// <param name="aggregate">The aggregate to load events into.</param>
    /// <param name="events">The sequence of events to replay.</param>
    /// <returns>The aggregate with events applied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="aggregate"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="events"/> is null.</exception>
    public static EventSourcedAggregate RehydrateFromEvents(
        this EventSourcedAggregate aggregate,
        IEnumerable<object> events)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        ArgumentNullException.ThrowIfNull(events);

        // Apply events to existing aggregate
        aggregate.LoadFromHistory(events);
        return aggregate;
    }

    /// <summary>
    /// Determines whether the aggregate has uncommitted events that need to be persisted.
    /// Useful for checking if a save operation is needed.
    /// </summary>
    /// <param name="aggregate">The aggregate to check.</param>
    /// <returns>True if there are uncommitted events; otherwise false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="aggregate"/> is null.</exception>
    public static bool HasUncommittedEvents(this EventSourcedAggregate aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        return aggregate.UncommittedEvents.Count > 0;
    }
}