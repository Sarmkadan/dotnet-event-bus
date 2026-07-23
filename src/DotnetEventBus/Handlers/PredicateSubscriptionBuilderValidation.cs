#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Reflection;

using DotnetEventBus.Handlers;
using DotnetEventBus.Utilities;

/// <summary>
/// Provides validation extension methods for <see cref="PredicateSubscriptionBuilder{TEvent}"/> instances.
/// </summary>
public static class PredicateSubscriptionBuilderValidation
{
    private const int MinPriority = -1000;
    private const int MaxPriority = 1000;
    private static readonly FieldInfo? AsyncHandlerField = typeof(PredicateSubscriptionBuilder<>).GetField(
        "_asyncHandler", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo? HandlerNameField = typeof(PredicateSubscriptionBuilder<>).GetField(
        "_handlerName", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo? PriorityField = typeof(PredicateSubscriptionBuilder<>).GetField(
        "_priority", BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// Validates the configuration of a predicate subscription builder.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="builder">The predicate subscription builder to validate.</param>
    /// <returns>
    /// An empty list if the builder is valid; otherwise, a list of human-readable
    /// validation error messages describing the problems found.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder"/> is <see langword="null"/>.
    /// </exception>
    public static IReadOnlyList<string> Validate<TEvent>(this PredicateSubscriptionBuilder<TEvent> builder)
        where TEvent : class
    {
        Guard.NotNull(builder, nameof(builder));

        var errors = new List<string>();

        // Check if handler is configured
        if (GetAsyncHandlerValue(builder) is null)
        {
            errors.Add("No handler configured. Call WithHandler before calling Register.");
        }

        // Check handler name if set
        if (GetHandlerNameValue(builder) is string handlerName && string.IsNullOrWhiteSpace(handlerName))
        {
            errors.Add("Handler name cannot be empty or whitespace.");
        }

        // Check priority is within reasonable range
        int priority = GetPriorityValue(builder);
        if (priority < MinPriority || priority > MaxPriority)
        {
            errors.Add($"Priority must be between {MinPriority} and {MaxPriority}.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the predicate subscription builder is in a valid state.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="builder">The predicate subscription builder to check.</param>
    /// <returns>
    /// <see langword="true"/> if the builder is valid; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder"/> is <see langword="null"/>.
    /// </exception>
    public static bool IsValid<TEvent>(this PredicateSubscriptionBuilder<TEvent> builder)
        where TEvent : class
    {
        Guard.NotNull(builder, nameof(builder));
        return Validate(builder).Count == 0;
    }

    /// <summary>
    /// Ensures that the predicate subscription builder is in a valid state.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="builder">The predicate subscription builder to validate.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the builder is not in a valid state. The exception message
    /// contains a list of all validation errors found.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder"/> is <see langword="null"/>.
    /// </exception>
    public static void EnsureValid<TEvent>(this PredicateSubscriptionBuilder<TEvent> builder)
        where TEvent : class
    {
        Guard.NotNull(builder, nameof(builder));

        var errors = Validate(builder);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"Predicate subscription builder is not valid. Errors: {string.Join(" ", errors)}");
        }
    }

    /// <summary>
    /// Gets the configured async handler delegate value using reflection.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <returns>The handler delegate, or <see langword="null"/> if not configured.</returns>
    private static object? GetAsyncHandlerValue<TEvent>(PredicateSubscriptionBuilder<TEvent> builder)
        where TEvent : class
    {
        return AsyncHandlerField?.GetValue(builder);
    }

    /// <summary>
    /// Gets the configured handler name value using reflection.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <returns>The handler name, or <see langword="null"/> if not set.</returns>
    private static string? GetHandlerNameValue<TEvent>(PredicateSubscriptionBuilder<TEvent> builder)
        where TEvent : class
    {
        return HandlerNameField?.GetValue(builder) as string;
    }

    /// <summary>
    /// Gets the configured priority value using reflection.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <returns>The priority value (default 0).</returns>
    private static int GetPriorityValue<TEvent>(PredicateSubscriptionBuilder<TEvent> builder)
        where TEvent : class
    {
        return PriorityField?.GetValue(builder) as int? ?? 0;
    }
}