#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace DotnetEventBus.Advanced;

/// <summary>
/// Provides validation helpers for <see cref="EventSourcedAggregate"/> instances.
/// Validates the core properties of event-sourced aggregates including identity and version.
/// </summary>
public static class EventSourcedAggregateValidation
{
    /// <summary>
    /// Validates the specified aggregate and returns a list of validation problems.
    /// </summary>
    /// <param name="value">The aggregate to validate</param>
    /// <returns>An enumerable of validation problem descriptions; empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    public static IReadOnlyList<string> Validate(this EventSourcedAggregate value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate Id
        if (string.IsNullOrWhiteSpace(value.Id))
        {
            problems.Add("Aggregate Id is null, empty, or whitespace.");
        }

        // Validate Version (should be non-negative)
        if (value.Version < 0)
        {
            problems.Add("Aggregate Version is negative, which is invalid.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified aggregate is valid.
    /// </summary>
    /// <param name="value">The aggregate to check</param>
    /// <returns>True if the aggregate is valid; otherwise false</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    public static bool IsValid(this EventSourcedAggregate value) => value.Validate().Count == 0;

    /// <summary>
    /// Ensures that the specified aggregate is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The aggregate to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    /// <exception cref="ArgumentException">Thrown if the aggregate is invalid, containing all validation problems</exception>
    public static void EnsureValid(this EventSourcedAggregate value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Aggregate validation failed:{Environment.NewLine} - {string.Join($"{Environment.NewLine} - ", problems)}");
        }
    }
}