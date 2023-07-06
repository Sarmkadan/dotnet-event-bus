#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;
using DotnetEventBus.Models;
using DotnetEventBus.Services;

namespace DotnetEventBus.Tests;

/// <summary>
/// Provides validation helpers for DeadLetterServiceTests test fixture.
/// </summary>
public static class DeadLetterServiceTestsValidation
{
    /// <summary>
    /// Validates a DeadLetterServiceTests instance and returns a list of validation errors.
    /// </summary>
    /// <param name="value">The DeadLetterServiceTests instance to validate.</param>
    /// <returns>A read-only list of validation error messages. Empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this DeadLetterServiceTests value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // DeadLetterServiceTests is a test fixture class with no public data members to validate
        // All validation is handled by the individual test methods

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified DeadLetterServiceTests instance is valid.
    /// </summary>
    /// <param name="value">The DeadLetterServiceTests instance to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this DeadLetterServiceTests value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return true; // DeadLetterServiceTests is always valid as it's a test fixture
    }

    /// <summary>
    /// Ensures that the specified DeadLetterServiceTests instance is valid, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="value">The DeadLetterServiceTests instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is invalid.</exception>
    public static void EnsureValid(this DeadLetterServiceTests value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            var errorMessages = string.Join("\n- ", errors);
            throw new ArgumentException(
                $"DeadLetterServiceTests instance is invalid. Validation errors:\n- {errorMessages}",
                nameof(value));
        }
    }
}