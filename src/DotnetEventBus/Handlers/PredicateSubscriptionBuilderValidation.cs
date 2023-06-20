#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Collections.Generic;

namespace DotnetEventBus.Handlers;

/// <summary>
/// Provides validation extension methods for <see cref="PredicateSubscriptionBuilder{TEvent}"/> instances.
/// </summary>
public static class PredicateSubscriptionBuilderValidation
{
    /// <summary>
    /// Validates the configuration of a predicate subscription builder.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="value">The predicate subscription builder to validate.</param>
    /// <returns>
    /// An empty list if the builder is valid; otherwise, a list of human-readable
    /// validation error messages describing the problems found.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> is <see langword="null"/>.
    /// </exception>
    public static IReadOnlyList<string> Validate<TEvent>(this PredicateSubscriptionBuilder<TEvent> value)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Check if handler is configured
        if (value.GetType()
            .GetField("_asyncHandler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(value) is null)
        {
            errors.Add("No handler configured. Call WithHandler before calling Register.");
        }

        // Check handler name if set
        var handlerNameField = value.GetType()
            .GetField("_handlerName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (handlerNameField?.GetValue(value) is string handlerName && string.IsNullOrWhiteSpace(handlerName))
        {
            errors.Add("Handler name cannot be empty or whitespace.");
        }

        // Check priority is within reasonable range
        var priorityField = value.GetType()
            .GetField("_priority", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (priorityField?.GetValue(value) is int priority && (priority < -1000 || priority > 1000))
        {
            errors.Add("Priority must be between -1000 and 1000.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the predicate subscription builder is in a valid state.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="value">The predicate subscription builder to check.</param>
    /// <returns>
    /// <see langword="true"/> if the builder is valid; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> is <see langword="null"/>.
    /// </exception>
    public static bool IsValid<TEvent>(this PredicateSubscriptionBuilder<TEvent> value)
        where TEvent : class
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the predicate subscription builder is in a valid state.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="value">The predicate subscription builder to validate.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the builder is not in a valid state. The exception message
    /// contains a list of all validation errors found.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> is <see langword="null"/>.
    /// </exception>
    public static void EnsureValid<TEvent>(this PredicateSubscriptionBuilder<TEvent> value)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"Predicate subscription builder is not valid. Errors: {string.Join(" ", errors)}");
        }
    }
}