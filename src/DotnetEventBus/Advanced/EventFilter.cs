#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace DotnetEventBus.Advanced;

/// <summary>
/// Provides fluent filtering API for events.
/// Allows handlers to filter events based on predicates before processing.
/// Why: Reduces unnecessary handler invocations by filtering at the bus level.
/// </summary>
public sealed class EventFilter<T> where T : class
{
    private readonly List<Func<T, bool>> _predicates = [];

    /// <summary>
    /// Adds a predicate filter.
    /// </summary>
    public EventFilter<T> Where(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _predicates.Add(predicate);
        return this;
    }

    /// <summary>
    /// Adds a property value filter.
    /// Example: .WhereProperty(e => e.UserId, 123)
    /// </summary>
    public EventFilter<T> WhereProperty<TProperty>(Func<T, TProperty> propertySelector, TProperty expectedValue)
    {
        return Where(x => propertySelector(x)?.Equals(expectedValue) ?? false);
    }

    /// <summary>
    /// Adds a property range filter.
    /// </summary>
    public EventFilter<T> WherePropertyInRange<TProperty>(
        Func<T, TProperty> propertySelector,
        TProperty min,
        TProperty max) where TProperty : IComparable<TProperty>
    {
        return Where(x =>
        {
            var value = propertySelector(x);
            return value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;
        });
    }

    /// <summary>
    /// Adds a string contains filter.
    /// </summary>
    public EventFilter<T> WherePropertyContains(Func<T, string?> propertySelector, string value)
    {
        return Where(x => (propertySelector(x) ?? string.Empty).Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Inverts the previous filter (NOT).
    /// </summary>
    public EventFilter<T> Not(Func<T, bool> predicate)
    {
        return Where(x => !predicate(x));
    }

    /// <summary>
    /// Evaluates all filters against an event.
    /// </summary>
    public bool Matches(T @event)
    {
        return _predicates.All(p => p(@event));
    }

    /// <summary>
    /// Filters a collection of events.
    /// </summary>
    public IEnumerable<T> FilterCollection(IEnumerable<T> events)
    {
        return events.Where(Matches);
    }

    /// <summary>
    /// Gets the number of registered filters.
    /// </summary>
    public int FilterCount => _predicates.Count;

    /// <summary>
    /// Clears all filters.
    /// </summary>
    public void Clear()
    {
        _predicates.Clear();
    }
}

/// <summary>
/// Factory for creating event filters.
/// </summary>
public static class FilterBuilder
{
    /// <summary>
    /// Creates a new filter for the specified event type.
    /// </summary>
    public static EventFilter<T> CreateFilter<T>() where T : class
    {
        return new EventFilter<T>();
    }

    /// <summary>
    /// Creates a filter that matches all events.
    /// </summary>
    public static EventFilter<T> CreateWildcardFilter<T>() where T : class
    {
        return new EventFilter<T>().Where(_ => true);
    }

    /// <summary>
    /// Creates a filter that matches no events.
    /// </summary>
    public static EventFilter<T> CreateEmptyFilter<T>() where T : class
    {
        return new EventFilter<T>().Where(_ => false);
    }
}
