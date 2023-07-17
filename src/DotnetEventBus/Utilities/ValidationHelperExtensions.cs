#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DotnetEventBus.Utilities;

/// <summary>
/// Extension methods for ValidationHelper providing additional validation scenarios.
/// </summary>
public static class ValidationHelperExtensions
{
    private static readonly FieldInfo? _errorsField = typeof(ValidationHelper).GetField("_errors", BindingFlags.NonPublic | BindingFlags.Instance);

    private static void AddError(this ValidationHelper helper, string errorMessage)
    {
        if (_errorsField != null)
        {
            var errorsList = _errorsField.GetValue(helper) as List<string>;
            errorsList?.Add(errorMessage);
        }
    }

    /// <summary>
    /// Validates that a string is not null or whitespace and provides a custom error message.
    /// </summary>
    /// <param name="helper">The validation helper instance.</param>
    /// <param name="value">The string value to validate.</param>
    /// <param name="fieldName">Name of the field being validated.</param>
    /// <param name="customErrorMessage">Custom error message to add if validation fails.</param>
    /// <returns>The validation helper instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="fieldName"/> or <paramref name="customErrorMessage"/> is null.</exception>
    public static ValidationHelper RequireNotEmpty(this ValidationHelper helper, string? value, string fieldName, string customErrorMessage)
    {
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentNullException.ThrowIfNull(customErrorMessage);

        if (string.IsNullOrWhiteSpace(value))
        {
            helper.AddError(customErrorMessage);
        }

        return helper;
    }

    /// <summary>
    /// Validates that a value is not null and provides a custom error message.
    /// </summary>
    /// <param name="helper">The validation helper instance.</param>
    /// <param name="value">The value to validate.</param>
    /// <param name="fieldName">Name of the field being validated.</param>
    /// <param name="customErrorMessage">Custom error message to add if validation fails.</param>
    /// <returns>The validation helper instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="fieldName"/> or <paramref name="customErrorMessage"/> is null.</exception>
    public static ValidationHelper RequireNotNull<T>(this ValidationHelper helper, T? value, string fieldName, string customErrorMessage) where T : class
    {
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentNullException.ThrowIfNull(customErrorMessage);

        if (value is null)
        {
            helper.AddError(customErrorMessage);
        }

        return helper;
    }

    /// <summary>
    /// Validates that a collection is not null or empty.
    /// </summary>
    /// <param name="helper">The validation helper instance.</param>
    /// <param name="items">The collection to validate.</param>
    /// <param name="fieldName">Name of the field being validated.</param>
    /// <returns>The validation helper instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="fieldName"/> is null.</exception>
    public static ValidationHelper RequireNotEmpty<T>(this ValidationHelper helper, IEnumerable<T>? items, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(fieldName);

        if (items is null || !items.Any())
        {
            helper.AddError($"{fieldName} collection is required and cannot be empty");
        }

        return helper;
    }

    /// <summary>
    /// Validates that a string matches a regex pattern with a custom error message.
    /// </summary>
    /// <param name="helper">The validation helper instance.</param>
    /// <param name="value">The string value to validate.</param>
    /// <param name="pattern">The regex pattern to match against.</param>
    /// <param name="fieldName">Name of the field being validated.</param>
    /// <param name="message">Custom error message to display if validation fails.</param>
    /// <returns>The validation helper instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="pattern"/>, <paramref name="fieldName"/>, or <paramref name="message"/> is null.</exception>
    public static ValidationHelper RequirePattern(this ValidationHelper helper, string? value, string pattern, string fieldName, string message)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentNullException.ThrowIfNull(message);

        if (!string.IsNullOrEmpty(value) && !Regex.IsMatch(value, pattern))
        {
            helper.AddError($"{fieldName}: {message}");
        }

        return helper;
    }

    /// <summary>
    /// Validates that a string contains only alphanumeric characters.
    /// </summary>
    /// <param name="helper">The validation helper instance.</param>
    /// <param name="value">The string value to validate.</param>
    /// <param name="fieldName">Name of the field being validated.</param>
    /// <returns>The validation helper instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="fieldName"/> is null.</exception>
    public static ValidationHelper RequireAlphanumeric(this ValidationHelper helper, string? value, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(fieldName);

        if (!string.IsNullOrEmpty(value) && !Regex.IsMatch(value, "^[a-zA-Z0-9]*$"))
        {
            helper.AddError($"{fieldName} must contain only alphanumeric characters");
        }

        return helper;
    }

    /// <summary>
    /// Validates that a string contains only alphabetic characters.
    /// </summary>
    /// <param name="helper">The validation helper instance.</param>
    /// <param name="value">The string value to validate.</param>
    /// <param name="fieldName">Name of the field being validated.</param>
    /// <returns>The validation helper instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="fieldName"/> is null.</exception>
    public static ValidationHelper RequireAlphabetic(this ValidationHelper helper, string? value, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(fieldName);

        if (!string.IsNullOrEmpty(value) && !Regex.IsMatch(value, "^[a-zA-Z]*$"))
        {
            helper.AddError($"{fieldName} must contain only alphabetic characters");
        }

        return helper;
    }

    /// <summary>
    /// Validates that a string contains only numeric characters.
    /// </summary>
    /// <param name="helper">The validation helper instance.</param>
    /// <param name="value">The string value to validate.</param>
    /// <param name="fieldName">Name of the field being validated.</param>
    /// <returns>The validation helper instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="fieldName"/> is null.</exception>
    public static ValidationHelper RequireNumeric(this ValidationHelper helper, string? value, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(fieldName);

        if (!string.IsNullOrEmpty(value) && !Regex.IsMatch(value, "^[0-9]*$"))
        {
            helper.AddError($"{fieldName} must contain only numeric characters");
        }

        return helper;
    }

    /// <summary>
    /// Validates that a string has a minimum length.
    /// </summary>
    /// <param name="helper">The validation helper instance.</param>
    /// <param name="value">The string value to validate.</param>
    /// <param name="minLength">The minimum required length.</param>
    /// <param name="fieldName">Name of the field being validated.</param>
    /// <returns>The validation helper instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="fieldName"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="minLength"/> is negative.</exception>
    public static ValidationHelper RequireMinLength(this ValidationHelper helper, string? value, int minLength, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentOutOfRangeException.ThrowIfNegative(minLength);

        if (value is not null && value.Length < minLength)
        {
            helper.AddError($"{fieldName} must be at least {minLength} characters long");
        }

        return helper;
    }

    /// <summary>
    /// Validates that a string has a maximum length.
    /// </summary>
    /// <param name="helper">The validation helper instance.</param>
    /// <param name="value">The string value to validate.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="fieldName">Name of the field being validated.</param>
    /// <returns>The validation helper instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="fieldName"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxLength"/> is negative.</exception>
    public static ValidationHelper RequireMaxLength(this ValidationHelper helper, string? value, int maxLength, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        if (value is not null && value.Length > maxLength)
        {
            helper.AddError($"{fieldName} cannot exceed {maxLength} characters");
        }

        return helper;
    }

    /// <summary>
    /// Validates that a collection has exactly the specified number of items.
    /// </summary>
    /// <param name="helper">The validation helper instance.</param>
    /// <param name="items">The collection to validate.</param>
    /// <param name="exactCount">The exact number of items required.</param>
    /// <param name="fieldName">Name of the field being validated.</param>
    /// <returns>The validation helper instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="fieldName"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="exactCount"/> is negative.</exception>
    public static ValidationHelper RequireExactItems<T>(this ValidationHelper helper, IEnumerable<T>? items, int exactCount, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentOutOfRangeException.ThrowIfNegative(exactCount);

        var count = items?.Count() ?? 0;
        if (count != exactCount)
        {
            helper.AddError($"{fieldName} must contain exactly {exactCount} items, but contains {count}");
        }

        return helper;
    }

    /// <summary>
    /// Validates that a value is greater than a minimum value.
    /// </summary>
    /// <param name="helper">The validation helper instance.</param>
    /// <param name="value">The value to validate.</param>
    /// <param name="minimum">The minimum value (exclusive).</param>
    /// <param name="fieldName">Name of the field being validated.</param>
    /// <returns>The validation helper instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="fieldName"/> is null.</exception>
    public static ValidationHelper RequireGreaterThan<T>(this ValidationHelper helper, T value, T minimum, string fieldName) where T : IComparable<T>
    {
        ArgumentNullException.ThrowIfNull(fieldName);

        if (value.CompareTo(minimum) <= 0)
        {
            helper.AddError($"{fieldName} must be greater than {minimum}");
        }

        return helper;
    }

    /// <summary>
    /// Validates that a value is less than a maximum value.
    /// </summary>
    /// <param name="helper">The validation helper instance.</param>
    /// <param name="value">The value to validate.</param>
    /// <param name="maximum">The maximum value (exclusive).</param>
    /// <param name="fieldName">Name of the field being validated.</param>
    /// <returns>The validation helper instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="fieldName"/> is null.</exception>
    public static ValidationHelper RequireLessThan<T>(this ValidationHelper helper, T value, T maximum, string fieldName) where T : IComparable<T>
    {
        ArgumentNullException.ThrowIfNull(fieldName);

        if (value.CompareTo(maximum) >= 0)
        {
            helper.AddError($"{fieldName} must be less than {maximum}");
        }

        return helper;
    }

    /// <summary>
    /// Validates that a string is a valid IPv4 address.
    /// </summary>
    /// <param name="helper">The validation helper instance.</param>
    /// <param name="ipAddress">The IP address string to validate.</param>
    /// <param name="fieldName">Name of the field being validated. Defaults to "IP Address".</param>
    /// <returns>The validation helper instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="fieldName"/> is null.</exception>
    public static ValidationHelper RequireValidIpAddress(this ValidationHelper helper, string? ipAddress, string fieldName = "IP Address")
    {
        ArgumentNullException.ThrowIfNull(fieldName);

        if (!string.IsNullOrEmpty(ipAddress))
        {
            var ipPattern = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
            if (!Regex.IsMatch(ipAddress, ipPattern))
            {
                helper.AddError($"{fieldName} is not a valid IPv4 address");
            }
        }

        return helper;
    }

    /// <summary>
    /// Validates that a string is a valid GUID.
    /// </summary>
    /// <param name="helper">The validation helper instance.</param>
    /// <param name="guidString">The GUID string to validate.</param>
    /// <param name="fieldName">Name of the field being validated. Defaults to "GUID".</param>
    /// <returns>The validation helper instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="fieldName"/> is null.</exception>
    public static ValidationHelper RequireValidGuid(this ValidationHelper helper, string? guidString, string fieldName = "GUID")
    {
        ArgumentNullException.ThrowIfNull(fieldName);

        if (!string.IsNullOrEmpty(guidString) && !Guid.TryParse(guidString, out _))
        {
            helper.AddError($"{fieldName} is not a valid GUID");
        }

        return helper;
    }
}