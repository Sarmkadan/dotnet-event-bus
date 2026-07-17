#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;

namespace DotnetEventBus.Models;

/// <summary>
/// Provides validation helpers for <see cref="DeadLetterEntry"/> instances.
/// </summary>
public static class DeadLetterEntryValidation
{
    /// <summary>
    /// Validates a <see cref="DeadLetterEntry"/> instance and returns a list of validation errors.
    /// </summary>
    /// <param name="value">The dead letter entry to validate.</param>
    /// <returns>A read-only list of validation error messages. Empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this DeadLetterEntry value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate Id
        if (string.IsNullOrWhiteSpace(value.Id))
        {
            errors.Add("Id cannot be null or whitespace.");
        }
        else if (!IsValidGuidFormat(value.Id))
        {
            errors.Add("Id must be a valid GUID format.");
        }

        // Validate Message (cannot be null)
        if (value.Message is null)
        {
            errors.Add("Message cannot be null.");
        }
        else
        {
            // Validate the EventMessage itself
            try
            {
                value.Message.Validate();
            }
            catch (ArgumentException ex)
            {
                errors.Add($"Message validation failed: {ex.Message}");
            }
        }

        // Validate FailedHandlerName
        if (string.IsNullOrWhiteSpace(value.FailedHandlerName))
        {
            errors.Add("FailedHandlerName cannot be null or whitespace.");
        }

        // Validate ExceptionMessage
        if (string.IsNullOrWhiteSpace(value.ExceptionMessage))
        {
            errors.Add("ExceptionMessage cannot be null or whitespace.");
        }

        // Validate CreatedAtUtc (should not be default DateTime)
        if (value.CreatedAtUtc == default)
        {
            errors.Add("CreatedAtUtc cannot be the default DateTime value.");
        }
        else if (value.CreatedAtUtc > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("CreatedAtUtc cannot be in the future.");
        }
        else if (value.CreatedAtUtc < DateTime.UtcNow.AddYears(-1))
        {
            errors.Add("CreatedAtUtc cannot be more than one year in the past.");
        }

        // Validate MaxRetryAttempts (should be non-negative)
        if (value.MaxRetryAttempts < 0)
        {
            errors.Add("MaxRetryAttempts cannot be negative.");
        }

        // Validate Status (should be a valid enum value)
        if (!Enum.IsDefined(typeof(DeadLetterStatus), value.Status))
        {
            errors.Add("Status must be a valid DeadLetterStatus value.");
        }

        // Validate StatusReason based on Status
        if (value.Status == DeadLetterStatus.ReviewedNotProcessed && string.IsNullOrWhiteSpace(value.StatusReason))
        {
            errors.Add("StatusReason is required when Status is ReviewedNotProcessed.");
        }

        if (value.Status == DeadLetterStatus.ReprocessFailed && string.IsNullOrWhiteSpace(value.StatusReason))
        {
            errors.Add("StatusReason is required when Status is ReprocessFailed.");
        }

        if (value.Status == DeadLetterStatus.Archived && string.IsNullOrWhiteSpace(value.StatusReason))
        {
            errors.Add("StatusReason is required when Status is Archived.");
        }

        // Validate ExceptionStackTrace if present
        if (!string.IsNullOrEmpty(value.ExceptionStackTrace) && value.ExceptionStackTrace.Length > 100_000)
        {
            errors.Add("ExceptionStackTrace cannot exceed 100,000 characters.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="DeadLetterEntry"/> is valid.
    /// </summary>
    /// <param name="value">The dead letter entry to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this DeadLetterEntry value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="DeadLetterEntry"/> is valid, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="value">The dead letter entry to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the entry is invalid, containing all validation errors.</exception>
    public static void EnsureValid(this DeadLetterEntry value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            var errorMessages = string.Join("\n- ", errors);
            throw new ArgumentException(
                $"DeadLetterEntry is invalid. Validation errors:\n- {errorMessages}",
                nameof(value));
        }
    }

    /// <summary>
    /// Validates that a string is a valid GUID format.
    /// </summary>
    /// <param name="input">The string to validate.</param>
    /// <returns>True if valid GUID format; otherwise, false.</returns>
    private static bool IsValidGuidFormat(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        // Try standard parse first
        if (Guid.TryParse(input, out _))
        {
            return true;
        }

        // Try parsing with different formats
        var formats = new[] { "D", "N", "B", "P", "X" };
        return formats.Any(format => Guid.TryParseExact(input, format, out _));
    }
}
