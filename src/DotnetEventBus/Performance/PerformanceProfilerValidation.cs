#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;

namespace DotnetEventBus.Performance;

/// <summary>
/// Provides validation helpers for <see cref="OperationStats"/> and <see cref="ProfilingSessionSummary"/> instances.
/// Ensures that profiler statistics are valid before generating reports or using metrics.
/// Why: Prevents misleading or incorrect performance data from being used in decisions.
/// </summary>
public static class PerformanceProfilerValidation
{
    /// <summary>
    /// Validates an <see cref="OperationStats"/> instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The operation statistics to validate.</param>
    /// <returns>An empty list if valid; otherwise, a list of validation error messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this OperationStats? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate OperationName
        if (string.IsNullOrWhiteSpace(value.OperationName))
        {
            errors.Add("OperationName must not be null, empty, or whitespace.");
        }

        // Validate ExecutionCount
        if (value.ExecutionCount < 0)
        {
            errors.Add("ExecutionCount must be non-negative.");
        }

        // Validate TotalTimeMs
        if (value.TotalTimeMs < 0)
        {
            errors.Add("TotalTimeMs must be non-negative.");
        }

        // Validate AverageTimeMs
        if (double.IsNaN(value.AverageTimeMs) || double.IsInfinity(value.AverageTimeMs))
        {
            errors.Add("AverageTimeMs must be a valid number.");
        }
        else if (value.AverageTimeMs < 0)
        {
            errors.Add("AverageTimeMs must be non-negative.");
        }

        // Validate MinTimeMs
        if (value.MinTimeMs < 0)
        {
            errors.Add("MinTimeMs must be non-negative.");
        }

        // Validate MaxTimeMs
        if (value.MaxTimeMs < 0)
        {
            errors.Add("MaxTimeMs must be non-negative.");
        }

        // Validate that MaxTimeMs >= MinTimeMs when there are executions
        if (value.ExecutionCount > 0 && value.MaxTimeMs < value.MinTimeMs)
        {
            errors.Add("MaxTimeMs must be greater than or equal to MinTimeMs.");
        }

        // Validate MedianTimeMs
        if (double.IsNaN(value.MedianTimeMs) || double.IsInfinity(value.MedianTimeMs))
        {
            errors.Add("MedianTimeMs must be a valid number.");
        }
        else if (value.MedianTimeMs < 0)
        {
            errors.Add("MedianTimeMs must be non-negative.");
        }

        // Validate P95TimeMs
        if (double.IsNaN(value.P95TimeMs) || double.IsInfinity(value.P95TimeMs))
        {
            errors.Add("P95TimeMs must be a valid number.");
        }
        else if (value.P95TimeMs < 0)
        {
            errors.Add("P95TimeMs must be non-negative.");
        }

        // Validate P99TimeMs
        if (double.IsNaN(value.P99TimeMs) || double.IsInfinity(value.P99TimeMs))
        {
            errors.Add("P99TimeMs must be a valid number.");
        }
        else if (value.P99TimeMs < 0)
        {
            errors.Add("P99TimeMs must be non-negative.");
        }

        // Cross-validate counts and execution metrics
        if (value.ExecutionCount == 0)
        {
            if (value.TotalTimeMs != 0)
            {
                errors.Add("TotalTimeMs must be 0 when ExecutionCount is 0.");
            }

            if (value.AverageTimeMs != 0)
            {
                errors.Add("AverageTimeMs must be 0 when ExecutionCount is 0.");
            }
        }
        else
        {
            // When there are executions, ensure we have valid statistics
            if (value.TotalTimeMs <= 0)
            {
                errors.Add("TotalTimeMs must be positive when ExecutionCount is positive.");
            }

            if (value.AverageTimeMs <= 0)
            {
                errors.Add("AverageTimeMs must be positive when ExecutionCount is positive.");
            }
        }

        // Validate percentile relationships
        if (value.P95TimeMs > 0 && value.MaxTimeMs > 0 && value.P95TimeMs > value.MaxTimeMs)
        {
            errors.Add("P95TimeMs must be less than or equal to MaxTimeMs.");
        }

        if (value.P99TimeMs > 0 && value.MaxTimeMs > 0 && value.P99TimeMs > value.MaxTimeMs)
        {
            errors.Add("P99TimeMs must be less than or equal to MaxTimeMs.");
        }

        if (value.P99TimeMs > 0 && value.P95TimeMs > 0 && value.P99TimeMs < value.P95TimeMs)
        {
            errors.Add("P99TimeMs must be greater than or equal to P95TimeMs.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates a <see cref="ProfilingSessionSummary"/> instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The session summary to validate.</param>
    /// <returns>An empty list if valid; otherwise, a list of validation error messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ProfilingSessionSummary? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate SessionDuration
        if (value.SessionDuration < TimeSpan.Zero)
        {
            errors.Add("SessionDuration must be non-negative.");
        }

        // Validate OperationCount
        if (value.OperationCount < 0)
        {
            errors.Add("OperationCount must be non-negative.");
        }

        // Validate TotalExecutions
        if (value.TotalExecutions < 0)
        {
            errors.Add("TotalExecutions must be non-negative.");
        }

        // Validate TotalTimeMs
        if (value.TotalTimeMs < 0)
        {
            errors.Add("TotalTimeMs must be non-negative.");
        }

        // Validate AverageTimeMs
        if (double.IsNaN(value.AverageTimeMs) || double.IsInfinity(value.AverageTimeMs))
        {
            errors.Add("AverageTimeMs must be a valid number.");
        }
        else if (value.AverageTimeMs < 0)
        {
            errors.Add("AverageTimeMs must be non-negative.");
        }

        // Validate ThroughputPerSecond
        if (double.IsNaN(value.ThroughputPerSecond) || double.IsInfinity(value.ThroughputPerSecond))
        {
            errors.Add("ThroughputPerSecond must be a valid number.");
        }
        else if (value.ThroughputPerSecond < 0)
        {
            errors.Add("ThroughputPerSecond must be non-negative.");
        }

        // Cross-validate counts and metrics
        if (value.TotalExecutions == 0)
        {
            if (value.TotalTimeMs != 0)
            {
                errors.Add("TotalTimeMs must be 0 when TotalExecutions is 0.");
            }

            if (value.AverageTimeMs != 0)
            {
                errors.Add("AverageTimeMs must be 0 when TotalExecutions is 0.");
            }

            if (value.OperationCount != 0)
            {
                errors.Add("OperationCount must be 0 when TotalExecutions is 0.");
            }
        }
        else
        {
            // When there are executions, ensure we have valid statistics
            if (value.TotalTimeMs <= 0)
            {
                errors.Add("TotalTimeMs must be positive when TotalExecutions is positive.");
            }

            if (value.AverageTimeMs <= 0)
            {
                errors.Add("AverageTimeMs must be positive when TotalExecutions is positive.");
            }
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="OperationStats"/> instance is valid.
    /// </summary>
    /// <param name="value">The operation statistics to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this OperationStats? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Determines whether the specified <see cref="ProfilingSessionSummary"/> instance is valid.
    /// </summary>
    /// <param name="value">The session summary to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this ProfilingSessionSummary? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="OperationStats"/> instance is valid.
    /// </summary>
    /// <param name="value">The operation statistics to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid.
    /// The exception message contains a list of all validation errors.</exception>
    public static void EnsureValid(this OperationStats? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"OperationStats is not valid. Validation errors: {string.Join(" ", errors)}",
            nameof(value));
    }

    /// <summary>
    /// Ensures that the specified <see cref="ProfilingSessionSummary"/> instance is valid.
    /// </summary>
    /// <param name="value">The session summary to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid.
    /// The exception message contains a list of all validation errors.</exception>
    public static void EnsureValid(this ProfilingSessionSummary? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"ProfilingSessionSummary is not valid. Validation errors: {string.Join(" ", errors)}",
            nameof(value));
    }
}