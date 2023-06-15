using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotnetEventBus.Benchmarks;

/// <summary>
/// Validation helpers for <see cref="DeadLetterBenchmarks"/> to ensure benchmark data is valid before execution.
/// </summary>
public static class DeadLetterBenchmarksValidation
{
    /// <summary>
    /// Validates a <see cref="DeadLetterBenchmarks"/> instance and returns a list of validation problems.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>A read-only list of human-readable validation problems, or empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this DeadLetterBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate EventId
        if (string.IsNullOrWhiteSpace(value.EventId))
        {
            problems.Add($"EventId must not be null, empty, or whitespace. Current value: '{value.EventId ?? "null"}'");
        }

        // Validate Name
        if (string.IsNullOrWhiteSpace(value.Name))
        {
            problems.Add($"Name must not be null, empty, or whitespace. Current value: '{value.Name ?? "null"}'");
        }

        // Validate AttemptCount
        if (value.AttemptCount < 0)
        {
            problems.Add($"AttemptCount must be non-negative. Current value: {value.AttemptCount}");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="DeadLetterBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this DeadLetterBenchmarks value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="DeadLetterBenchmarks"/> instance is valid, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is invalid, containing a list of validation problems.</exception>
    public static void EnsureValid(this DeadLetterBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"DeadLetterBenchmarks instance is invalid. Problems:\n{string.Join("\n", problems)}");
        }
    }
}