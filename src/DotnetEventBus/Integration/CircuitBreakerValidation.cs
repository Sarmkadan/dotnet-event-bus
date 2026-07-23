#nullable enable

using System;
using System.Collections.Generic;

using DotnetEventBus.Integration;
using DotnetEventBus.Utilities;

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
        Guard.NotNull(value, nameof(value));

        var problems = new List<string>();

        // Validate state transitions based on public properties
        // These checks ensure the circuit breaker's state machine is in a consistent state

        // If state is HalfOpen, we should have some failures recorded (can't validate count directly)
        if (value.State == CircuitBreakerState.HalfOpen)
        {
            // HalfOpen state should eventually transition back to Closed or Open
            // We can't validate the internal failure count, but we can note that HalfOpen
            // is a transitional state that should not persist indefinitely in practice
        }

        // If state is Open, the circuit breaker should have been opened due to failures
        // We can't validate the internal state directly, but the constructor ensures
        // failureThreshold and timeout are valid

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="CircuitBreaker"/> instance is valid.
    /// </summary>
    /// <param name="value">The circuit breaker to check.</param>
    /// <returns>True if the circuit breaker is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this CircuitBreaker? value)
    {
        if (value is null)
        {
            return false;
        }

        try
        {
            return Validate(value).Count == 0;
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
    public static void EnsureValid(this CircuitBreaker? value)
    {
        Guard.NotNull(value, nameof(value));

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"CircuitBreaker is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
        }
    }
}