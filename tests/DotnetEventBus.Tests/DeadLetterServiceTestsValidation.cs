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
/// Provides validation extensions for the <see cref="DeadLetterServiceTests"/> test fixture class.
/// </summary>
public static class DeadLetterServiceTestsValidation
{
    /// <summary>
    /// Validates a <see cref="DeadLetterServiceTests"/> instance and returns a list of validation errors.
    /// </summary>
    /// <remarks>
    /// This method performs validation on the test fixture instance. Currently,
    /// <see cref="DeadLetterServiceTests"/> has no public data members to validate,
    /// so this method always returns an empty list. The validation infrastructure
    /// is maintained for future extensibility and consistency with the project's
    /// validation patterns.
    /// </remarks>
    /// <param name="value">The <see cref="DeadLetterServiceTests"/> instance to validate.</param>
    /// <returns>A read-only list of validation error messages. Empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this DeadLetterServiceTests value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // DeadLetterServiceTests is a test fixture class with no public data members to validate.
        // All validation is handled by the individual test methods.
        // This infrastructure is maintained for consistency with the project's validation patterns.

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="DeadLetterServiceTests"/> instance is valid.
    /// </summary>
    /// <remarks>
    /// A <see cref="DeadLetterServiceTests"/> instance is always considered valid as it's a test fixture
    /// with no mutable state or data members that require validation.
    /// </remarks>
    /// <param name="value">The <see cref="DeadLetterServiceTests"/> instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    public static bool IsValid(this DeadLetterServiceTests value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return true;
    }

    /// <summary>
    /// Ensures that the specified <see cref="DeadLetterServiceTests"/> instance is valid,
    /// throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <remarks>
    /// This method validates the test fixture instance and throws if invalid.
    /// Currently, all <see cref="DeadLetterServiceTests"/> instances are valid,
    /// so this method only validates for null.
    /// </remarks>
    /// <param name="value">The <see cref="DeadLetterServiceTests"/> instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is invalid.
    /// This exception is theoretically possible but will never occur with the
    /// current implementation of <see cref="DeadLetterServiceTests"/>.</exception>
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