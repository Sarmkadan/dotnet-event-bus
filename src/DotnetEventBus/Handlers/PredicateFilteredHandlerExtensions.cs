#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Handlers;

/// <summary>
/// Provides extension methods for <see cref="PredicateFilteredHandler{TEvent}"/> to enable fluent configuration
/// and common scenarios when working with predicate-filtered handlers.
/// </summary>
public static class PredicateFilteredHandlerExtensions
{
    /// <summary>
    /// Creates a new <see cref="PredicateFilteredHandler{TEvent}"/> with the same predicate and logger
    /// but wrapping a different inner handler. Useful for handler composition scenarios.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="source">The source predicate-filtered handler.</param>
    /// <param name="newInner">The new inner handler to wrap.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="newInner"/> is <see langword="null"/>.</exception>
    /// <returns>A new predicate-filtered handler with the same predicate and logger.</returns>
    public static PredicateFilteredHandler<TEvent> WithInnerHandler<TEvent>(
        this PredicateFilteredHandler<TEvent> source,
        IEventHandler<TEvent> newInner)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(newInner);

        // Extract the predicate and logger from the source handler via reflection
        var predicateField = typeof(PredicateFilteredHandler<TEvent>).GetField(
            "_predicate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var loggerField = typeof(PredicateFilteredHandler<TEvent>).GetField(
            "_logger",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var predicate = (Func<TEvent, bool>?)predicateField?.GetValue(source);
        var logger = (ILogger?)loggerField?.GetValue(source);

        return predicate is null
            ? throw new InvalidOperationException("Predicate cannot be null")
            : new PredicateFilteredHandler<TEvent>(
                newInner,
                predicate,
                logger);
    }

    /// <summary>
    /// Creates a new <see cref="PredicateFilteredHandler{TEvent}"/> with an inverted predicate.
    /// The new handler will process events that the original handler would skip, and vice versa.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="source">The source predicate-filtered handler.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <returns>A new predicate-filtered handler with inverted filtering logic.</returns>
    public static PredicateFilteredHandler<TEvent> InvertPredicate<TEvent>(
        this PredicateFilteredHandler<TEvent> source)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(source);

        var predicateField = typeof(PredicateFilteredHandler<TEvent>).GetField(
            "_predicate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var predicate = (Func<TEvent, bool>?)predicateField?.GetValue(source);

        return predicate is null
            ? throw new InvalidOperationException("Predicate cannot be null")
            : new PredicateFilteredHandler<TEvent>(
                source,
                e => !predicate(e),
                null);
    }

    /// <summary>
    /// Creates a new <see cref="PredicateFilteredHandler{TEvent}"/> with a predicate that combines
    /// the original predicate with an additional condition using logical AND.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="source">The source predicate-filtered handler.</param>
    /// <param name="additionalPredicate">The additional predicate to combine with AND.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="additionalPredicate"/> is <see langword="null"/>.</exception>
    /// <returns>A new predicate-filtered handler with combined filtering logic.</returns>
    public static PredicateFilteredHandler<TEvent> AndPredicate<TEvent>(
        this PredicateFilteredHandler<TEvent> source,
        Func<TEvent, bool> additionalPredicate)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(additionalPredicate);

        var predicateField = typeof(PredicateFilteredHandler<TEvent>).GetField(
            "_predicate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var originalPredicate = (Func<TEvent, bool>?)predicateField?.GetValue(source);

        return originalPredicate is null
            ? throw new InvalidOperationException("Original predicate cannot be null")
            : new PredicateFilteredHandler<TEvent>(
                source,
                e => originalPredicate(e) && additionalPredicate(e),
                null);
    }

    /// <summary>
    /// Creates a new <see cref="PredicateFilteredHandler{TEvent}"/> with a predicate that combines
    /// the original predicate with an additional condition using logical OR.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="source">The source predicate-filtered handler.</param>
    /// <param name="additionalPredicate">The additional predicate to combine with OR.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="additionalPredicate"/> is <see langword="null"/>.</exception>
    /// <returns>A new predicate-filtered handler with combined filtering logic.</returns>
    public static PredicateFilteredHandler<TEvent> OrPredicate<TEvent>(
        this PredicateFilteredHandler<TEvent> source,
        Func<TEvent, bool> additionalPredicate)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(additionalPredicate);

        var predicateField = typeof(PredicateFilteredHandler<TEvent>).GetField(
            "_predicate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var originalPredicate = (Func<TEvent, bool>?)predicateField?.GetValue(source);

        return originalPredicate is null
            ? throw new InvalidOperationException("Original predicate cannot be null")
            : new PredicateFilteredHandler<TEvent>(
                source,
                e => originalPredicate(e) || additionalPredicate(e),
                null);
    }
}