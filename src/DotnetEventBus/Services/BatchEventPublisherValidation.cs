#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotnetEventBus.Services;

/// <summary>
/// Provides validation helpers for <see cref="BatchEventPublisher"/> instances.
/// </summary>
public static class BatchEventPublisherValidation
{
    /// <summary>
    /// Validates the specified <see cref="BatchEventPublisher"/> instance.
    /// </summary>
    /// <param name="value">The publisher to validate.</param>
    /// <returns>A list of validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this BatchEventPublisher? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate internal state through public members
        var stats = value.GetStats();

        if (stats.BufferedEventCount < 0)
        {
            problems.Add("BufferedEventCount cannot be negative.");
        }

        if (stats.BufferedEventSize < 0)
        {
            problems.Add("BufferedEventSize cannot be negative.");
        }

        if (stats.LastFlushTime == default)
        {
            problems.Add("LastFlushTime has not been set to a valid DateTime.");
        }

        // Validate public properties through reflection or by checking accessible state
        // Since we can't access private fields, we validate through public methods

        // Check if buffer size is reasonable (should be non-negative and not exceed batch size)
        var bufferSize = value.GetBufferSize();
        if (bufferSize < 0)
        {
            problems.Add("Buffer size cannot be negative.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="BatchEventPublisher"/> instance is valid.
    /// </summary>
    /// <param name="value">The publisher to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this BatchEventPublisher? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="BatchEventPublisher"/> instance is valid.
    /// </summary>
    /// <param name="value">The publisher to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid.
    /// The exception message contains a list of all validation problems.</exception>
    public static void EnsureValid(this BatchEventPublisher? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();

        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"BatchEventPublisher is not valid. Problems:\n{string.Join("\n", problems)}");
    }
}