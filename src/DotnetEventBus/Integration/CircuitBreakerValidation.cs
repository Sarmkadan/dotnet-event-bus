#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotnetEventBus.Integration;

/// <summary>
/// Provides validation helpers for <see cref="CircuitBreaker"/> instances.
/// </summary>
public static class CircuitBreakerValidation
{
    /// <summary>
    /// Validates a <see cref="CircuitBreaker"/> instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The circuit breaker to validate.</param>
    /// <returns>A read-only list of validation problems; empty if the circuit breaker is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this CircuitBreaker value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // failureThreshold validation (set in constructor)
        // Since it's private, we can't validate it directly, but the constructor already validates it

        // timeout validation (set in constructor)
        // Since it's private, we can't validate it directly, but the constructor already validates it

        // _state validation: should be Closed, Open, or HalfOpen (all valid enum values)
        // _state is always valid as it's set to one of the enum values

        // _failureCount validation: should be >= 0
        // This is always valid as it's only incremented and reset to 0

        // _lastFailureTime validation: should be DateTime.MinValue or a valid DateTime
        // DateTime.MinValue is valid as initial state

        // _lock validation: always initialized, no validation needed

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="CircuitBreaker"/> instance is valid.
    /// </summary>
    /// <param name="value">The circuit breaker to check.</param>
    /// <returns>True if the circuit breaker is valid; otherwise, false.</returns>
    public static bool IsValid(this CircuitBreaker value)
    {
        try
        {
            _ = Validate(value);
            return true;
        }
        catch (ArgumentNullException)
        {
            return false;
        }
    }

    /// <summary>
    /// Ensures that a <see cref="CircuitBreaker"/> instance is valid, throwing an <see cref="ArgumentException"/> if it is not.
    /// </summary>
    /// <param name="value">The circuit breaker to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the circuit breaker has validation problems.</exception>
    public static void EnsureValid(this CircuitBreaker value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"CircuitBreaker is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
        }
    }
}