#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Handlers;

/// <summary>
/// Extension methods for <see cref="PredicateSubscriptionBuilder{TEvent}"/> that provide
/// additional convenience methods for building predicate subscriptions.
/// </summary>
public static class PredicateSubscriptionBuilderExtensions
{
    /// <summary>
    /// Adds a predicate that must evaluate to <see langword="true"/> for the event to be processed.
    /// This overload accepts an expression for better IntelliSense and compile-time safety.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="predicate">The condition to evaluate against each published event.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public static PredicateSubscriptionBuilder<TEvent> Where<TEvent>(
        this PredicateSubscriptionBuilder<TEvent> builder,
        Expression<Func<TEvent, bool>> predicate)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(predicate);

        return builder.Where(predicate.Compile());
    }

    /// <summary>
    /// Adds a negated predicate. The event is processed only when this condition evaluates to
    /// <see langword="false"/>.
    /// This overload accepts an expression for better IntelliSense and compile-time safety.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="predicate">The condition to negate.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public static PredicateSubscriptionBuilder<TEvent> WhereNot<TEvent>(
        this PredicateSubscriptionBuilder<TEvent> builder,
        Expression<Func<TEvent, bool>> predicate)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(predicate);

        return builder.WhereNot(predicate.Compile());
    }

    /// <summary>
    /// Adds a predicate that checks if the event is of a specific type.
    /// Useful for handling polymorphic event hierarchies.
    /// </summary>
    /// <typeparam name="TEvent">The base event type.</typeparam>
    /// <typeparam name="TDerived">The derived event type to match.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <see langword="null"/>.</exception>
    public static PredicateSubscriptionBuilder<TEvent> WhereTypeIs<TEvent, TDerived>(this PredicateSubscriptionBuilder<TEvent> builder)
        where TEvent : class
        where TDerived : class, TEvent
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Where(e => e is TDerived);
    }

    /// <summary>
    /// Adds a predicate that checks if the event is NOT of a specific type.
    /// Useful for excluding certain event types from a subscription.
    /// </summary>
    /// <typeparam name="TEvent">The base event type.</typeparam>
    /// <typeparam name="TExcluded">The derived event type to exclude.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <see langword="null"/>.</exception>
    public static PredicateSubscriptionBuilder<TEvent> WhereTypeIsNot<TEvent, TExcluded>(this PredicateSubscriptionBuilder<TEvent> builder)
        where TEvent : class
        where TExcluded : class, TEvent
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WhereNot(e => e is TExcluded);
    }

    /// <summary>
    /// Adds a predicate that checks if any of the specified values match a property.
    /// Useful for filtering events based on a set of allowed values.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="propertySelector">Selector that extracts the property from the event.</param>
    /// <param name="allowedValues">The set of allowed values for the property.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/>, <paramref name="propertySelector"/>, or <paramref name="allowedValues"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="allowedValues"/> is empty.</exception>
    public static PredicateSubscriptionBuilder<TEvent> WherePropertyIn<TEvent, TProperty>(
        this PredicateSubscriptionBuilder<TEvent> builder,
        Func<TEvent, TProperty> propertySelector,
        params TProperty[] allowedValues)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(propertySelector);
        ArgumentNullException.ThrowIfNull(allowedValues);
        if (allowedValues.Length == 0)
            throw new ArgumentException("At least one allowed value must be provided.", nameof(allowedValues));

        return builder.Where(e => allowedValues.Contains(propertySelector(e)));
    }

    /// <summary>
    /// Adds a predicate that checks if a property is null.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="propertySelector">Selector that extracts the property from the event.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="propertySelector"/> is <see langword="null"/>.</exception>
    public static PredicateSubscriptionBuilder<TEvent> WherePropertyIsNull<TEvent, TProperty>(
        this PredicateSubscriptionBuilder<TEvent> builder,
        Func<TEvent, TProperty> propertySelector)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(propertySelector);

        return builder.Where(e => propertySelector(e) is null);
    }

    /// <summary>
    /// Adds a predicate that checks if a property is NOT null.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="propertySelector">Selector that extracts the property from the event.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="propertySelector"/> is <see langword="null"/>.</exception>
    public static PredicateSubscriptionBuilder<TEvent> WherePropertyIsNotNull<TEvent, TProperty>(
        this PredicateSubscriptionBuilder<TEvent> builder,
        Func<TEvent, TProperty> propertySelector)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(propertySelector);

        return builder.Where(e => propertySelector(e) is not null);
    }

    /// <summary>
    /// Adds a predicate that checks if a string property matches a regular expression pattern.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="propertySelector">Selector that extracts the string property from the event.</param>
    /// <param name="pattern">The regular expression pattern to match.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/>, <paramref name="propertySelector"/>, or <paramref name="pattern"/> is <see langword="null"/>.</exception>
    public static PredicateSubscriptionBuilder<TEvent> WherePropertyMatches<TEvent>(
        this PredicateSubscriptionBuilder<TEvent> builder,
        Func<TEvent, string?> propertySelector,
        string pattern)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(propertySelector);
        ArgumentNullException.ThrowIfNull(pattern);

        var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return builder.Where(e => propertySelector(e) is not null && regex.IsMatch(propertySelector(e)!));
    }

    /// <summary>
    /// Adds a predicate that checks if a numeric property is within a specified range.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <typeparam name="TProperty">The type of the numeric property.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="propertySelector">Selector that extracts the numeric property from the event.</param>
    /// <param name="minValue">The minimum allowed value (inclusive).</param>
    /// <param name="maxValue">The maximum allowed value (inclusive).</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="propertySelector"/> is <see langword="null"/>.</exception>
    public static PredicateSubscriptionBuilder<TEvent> WherePropertyInRange<TEvent, TProperty>(
        this PredicateSubscriptionBuilder<TEvent> builder,
        Func<TEvent, TProperty> propertySelector,
        TProperty minValue,
        TProperty maxValue)
        where TEvent : class
        where TProperty : IComparable<TProperty>
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(propertySelector);

        return builder.Where(e =>
        {
            var value = propertySelector(e);
            return value.CompareTo(minValue) >= 0 && value.CompareTo(maxValue) <= 0;
        });
    }

    /// <summary>
    /// Sets both the handler and handler name in a single call.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="handlerName">A descriptive name that identifies this subscription.</param>
    /// <param name="handler">The async delegate that processes the event.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/>, <paramref name="handlerName"/>, or <paramref name="handler"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="handlerName"/> is empty or whitespace.</exception>
    public static PredicateSubscriptionBuilder<TEvent> WithHandler<TEvent>(
        this PredicateSubscriptionBuilder<TEvent> builder,
        string handlerName,
        Func<TEvent, CancellationToken, Task> handler)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(handlerName);
        if (string.IsNullOrWhiteSpace(handlerName))
            throw new ArgumentException("Handler name cannot be empty or whitespace.", nameof(handlerName));
        ArgumentNullException.ThrowIfNull(handler);

        return builder.WithHandlerName(handlerName).WithHandler(handler);
    }

    /// <summary>
    /// Sets the logger using a logger factory.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="loggerFactory">The logger factory to create the logger.</param>
    /// <param name="categoryName">The category name for the logger.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/>, <paramref name="loggerFactory"/>, or <paramref name="categoryName"/> is <see langword="null"/>.</exception>
    public static PredicateSubscriptionBuilder<TEvent> WithLogger<TEvent>(
        this PredicateSubscriptionBuilder<TEvent> builder,
        ILoggerFactory loggerFactory,
        string categoryName)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(categoryName);

        return builder.WithLogger(loggerFactory.CreateLogger(categoryName));
    }
}