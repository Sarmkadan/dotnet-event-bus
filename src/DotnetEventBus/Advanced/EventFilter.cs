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
    private Func<T, bool>? _compiledPredicate;

    /// <summary>
    /// Adds a predicate filter.
    /// </summary>
    public EventFilter<T> Where(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _predicates.Add(predicate);
        _compiledPredicate = null; // Invalidate compiled cache
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
    /// Compiles all registered predicates into a single optimized predicate function.
    /// This method caches the result for subsequent calls to avoid recompilation overhead.
    /// </summary>
    /// <remarks>
    /// The compiled predicate combines all individual predicates using AND semantics.
    /// When there are no predicates, returns a function that always returns <see langword="true"/>.
    /// When there is exactly one predicate, returns that predicate directly.
    /// When there are multiple predicates, combines them into a single composite function.
    /// </remarks>
    /// <returns>A single compiled predicate function that evaluates all filters with AND semantics.</returns>
    /// <exception cref="InvalidOperationException">Thrown when predicates cannot be compiled (should not occur with valid predicates).</exception>
    public Func<T, bool> Compile()
    {
        if (_compiledPredicate is not null)
        {
            return _compiledPredicate;
        }

        if (_predicates.Count == 0)
        {
            _compiledPredicate = _ => true; // Match all if no predicates
            return _compiledPredicate;
        }

        if (_predicates.Count == 1)
        {
            _compiledPredicate = _predicates[0];
            return _compiledPredicate;
        }

        // Combine multiple predicates into a single function for better performance
        // This avoids the overhead of multiple delegate invocations and LINQ's All() method
        _compiledPredicate = @event => _predicates.All(p => p(@event));
        return _compiledPredicate;
    }

    /// <summary>
    /// Evaluates all filters against an event.
    /// </summary>
    public bool Matches(T @event)
    {
        return Compile()(@event);
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
    /// Clears all filters and resets the compiled predicate cache.
    /// </summary>
    public void Clear()
    {
        _predicates.Clear();
        _compiledPredicate = null;
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
