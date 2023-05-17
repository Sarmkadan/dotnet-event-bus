#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DotnetEventBus.Utilities;

/// <summary>
/// Extension methods for ValidationHelper providing additional validation scenarios.
/// </summary>
public static class ValidationHelperExtensions
{
    /// <summary>
    /// Validates that a string is not null or whitespace and provides a custom error message.
    /// </summary>
    public static ValidationHelper RequireNotEmpty(this ValidationHelper helper, string? value, string fieldName, string customErrorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            var errors = helper.GetErrors() as List<string> ?? new List<string>(helper.GetErrors());
            errors.Add(customErrorMessage);
        }

        return helper;
    }

    /// <summary>
    /// Validates that a value is not null and provides a custom error message.
    /// </summary>
    public static ValidationHelper RequireNotNull<T>(this ValidationHelper helper, T? value, string fieldName, string customErrorMessage) where T : class
    {
        if (value is null)
        {
            var errors = helper.GetErrors() as List<string> ?? new List<string>(helper.GetErrors());
            errors.Add(customErrorMessage);
        }

        return helper;
    }

    /// <summary>
    /// Validates that a collection is not null or empty.
    /// </summary>
    public static ValidationHelper RequireNotEmpty<T>(this ValidationHelper helper, IEnumerable<T>? items, string fieldName)
    {
        if (items is null || !items.Any())
        {
            var errors = helper.GetErrors() as List<string> ?? new List<string>(helper.GetErrors());
            errors.Add($"{fieldName} collection is required and cannot be empty");
        }

        return helper;
    }

    /// <summary>
    /// Validates that a string matches a regex pattern with a custom error message.
    /// </summary>
    public static ValidationHelper RequirePattern(this ValidationHelper helper, string? value, string pattern, string fieldName, string message)
    {
        if (!string.IsNullOrEmpty(value) && !Regex.IsMatch(value, pattern))
        {
            var errors = helper.GetErrors() as List<string> ?? new List<string>(helper.GetErrors());
            errors.Add($"{fieldName}: {message}");
        }

        return helper;
    }

    /// <summary>
    /// Validates that a string contains only alphanumeric characters.
    /// </summary>
    public static ValidationHelper RequireAlphanumeric(this ValidationHelper helper, string? value, string fieldName)
    {
        if (!string.IsNullOrEmpty(value) && !Regex.IsMatch(value, "^[a-zA-Z0-9]*$"))
        {
            var errors = helper.GetErrors() as List<string> ?? new List<string>(helper.GetErrors());
            errors.Add($"{fieldName} must contain only alphanumeric characters");
        }

        return helper;
    }

    /// <summary>
    /// Validates that a string contains only alphabetic characters.
    /// </summary>
    public static ValidationHelper RequireAlphabetic(this ValidationHelper helper, string? value, string fieldName)
    {
        if (!string.IsNullOrEmpty(value) && !Regex.IsMatch(value, "^[a-zA-Z]*$"))
        {
            var errors = helper.GetErrors() as List<string> ?? new List<string>(helper.GetErrors());
            errors.Add($"{fieldName} must contain only alphabetic characters");
        }

        return helper;
    }

    /// <summary>
    /// Validates that a string contains only numeric characters.
    /// </summary>
    public static ValidationHelper RequireNumeric(this ValidationHelper helper, string? value, string fieldName)
    {
        if (!string.IsNullOrEmpty(value) && !Regex.IsMatch(value, "^[0-9]*$"))
        {
            var errors = helper.GetErrors() as List<string> ?? new List<string>(helper.GetErrors());
            errors.Add($"{fieldName} must contain only numeric characters");
        }

        return helper;
    }

    /// <summary>
    /// Validates that a string has a minimum length.
    /// </summary>
    public static ValidationHelper RequireMinLength(this ValidationHelper helper, string? value, int minLength, string fieldName)
    {
        if (value is not null && value.Length < minLength)
        {
            var errors = helper.GetErrors() as List<string> ?? new List<string>(helper.GetErrors());
            errors.Add($"{fieldName} must be at least {minLength} characters long");
        }

        return helper;
    }

    /// <summary>
    /// Validates that a string has a maximum length.
    /// </summary>
    public static ValidationHelper RequireMaxLength(this ValidationHelper helper, string? value, int maxLength, string fieldName)
    {
        if (value is not null && value.Length > maxLength)
        {
            var errors = helper.GetErrors() as List<string> ?? new List<string>(helper.GetErrors());
            errors.Add($"{fieldName} cannot exceed {maxLength} characters");
        }

        return helper;
    }

    /// <summary>
    /// Validates that a collection has exactly the specified number of items.
    /// </summary>
    public static ValidationHelper RequireExactItems<T>(this ValidationHelper helper, IEnumerable<T>? items, int exactCount, string fieldName)
    {
        var count = items?.Count() ?? 0;
        if (count != exactCount)
        {
            var errors = helper.GetErrors() as List<string> ?? new List<string>(helper.GetErrors());
            errors.Add($"{fieldName} must contain exactly {exactCount} items, but contains {count}");
        }

        return helper;
    }

    /// <summary>
    /// Validates that a value is greater than a minimum value.
    /// </summary>
    public static ValidationHelper RequireGreaterThan<T>(this ValidationHelper helper, T value, T minimum, string fieldName) where T : IComparable<T>
    {
        if (value.CompareTo(minimum) <= 0)
        {
            var errors = helper.GetErrors() as List<string> ?? new List<string>(helper.GetErrors());
            errors.Add($"{fieldName} must be greater than {minimum}");
        }

        return helper;
    }

    /// <summary>
    /// Validates that a value is less than a maximum value.
    /// </summary>
    public static ValidationHelper RequireLessThan<T>(this ValidationHelper helper, T value, T maximum, string fieldName) where T : IComparable<T>
    {
        if (value.CompareTo(maximum) >= 0)
        {
            var errors = helper.GetErrors() as List<string> ?? new List<string>(helper.GetErrors());
            errors.Add($"{fieldName} must be less than {maximum}");
        }

        return helper;
    }

    /// <summary>
    /// Validates that a string is a valid IPv4 address.
    /// </summary>
    public static ValidationHelper RequireValidIpAddress(this ValidationHelper helper, string? ipAddress, string fieldName = "IP Address")
    {
        if (!string.IsNullOrEmpty(ipAddress))
        {
            var ipPattern = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
            if (!Regex.IsMatch(ipAddress, ipPattern))
            {
                var errors = helper.GetErrors() as List<string> ?? new List<string>(helper.GetErrors());
                errors.Add($"{fieldName} is not a valid IPv4 address");
            }
        }

        return helper;
    }

    /// <summary>
    /// Validates that a string is a valid GUID.
    /// </summary>
    public static ValidationHelper RequireValidGuid(this ValidationHelper helper, string? guidString, string fieldName = "GUID")
    {
        if (!string.IsNullOrEmpty(guidString) && !Guid.TryParse(guidString, out _))
        {
            var errors = helper.GetErrors() as List<string> ?? new List<string>(helper.GetErrors());
            errors.Add($"{fieldName} is not a valid GUID");
        }

        return helper;
    }
}