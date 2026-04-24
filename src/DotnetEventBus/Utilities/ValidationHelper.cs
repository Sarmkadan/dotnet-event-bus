#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DotnetEventBus.Utilities;

/// <summary>
/// Utility class for common validation operations.
/// Provides fluent validation API for event bus parameters and configurations.
/// Why: Centralized validation ensures consistent error messages and validation rules.
/// </summary>
public sealed class ValidationHelper
{
    private readonly List<string> _errors = [];

    /// <summary>
    /// Validates that a string is not null or empty.
    /// </summary>
    public ValidationHelper RequireNotEmpty(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _errors.Add($"{fieldName} is required and cannot be empty");
        }

        return this;
    }

    /// <summary>
    /// Validates that a value is not null.
    /// </summary>
    public ValidationHelper RequireNotNull<T>(T? value, string fieldName) where T : class
    {
        if (value is null)
        {
            _errors.Add($"{fieldName} is required and cannot be null");
        }

        return this;
    }

    /// <summary>
    /// Validates that a string matches a regex pattern.
    /// </summary>
    public ValidationHelper RequirePattern(string? value, string pattern, string fieldName, string message)
    {
        if (!string.IsNullOrEmpty(value) && !Regex.IsMatch(value, pattern))
        {
            _errors.Add($"{fieldName}: {message}");
        }

        return this;
    }

    /// <summary>
    /// Validates that a string length is within bounds.
    /// </summary>
    public ValidationHelper RequireLength(string? value, int minLength, int maxLength, string fieldName)
    {
        if (value is not null)
        {
            if (value.Length < minLength || value.Length > maxLength)
            {
                _errors.Add($"{fieldName} must be between {minLength} and {maxLength} characters");
            }
        }

        return this;
    }

    /// <summary>
    /// Validates that a numeric value is within a range.
    /// </summary>
    public ValidationHelper RequireRange<T>(T value, T minimum, T maximum, string fieldName) where T : IComparable<T>
    {
        if (value.CompareTo(minimum) < 0 || value.CompareTo(maximum) > 0)
        {
            _errors.Add($"{fieldName} must be between {minimum} and {maximum}");
        }

        return this;
    }

    /// <summary>
    /// Validates that a collection has at least the minimum number of items.
    /// </summary>
    public ValidationHelper RequireMinimumItems<T>(IEnumerable<T>? items, int minimum, string fieldName)
    {
        var count = items?.Count() ?? 0;
        if (count < minimum)
        {
            _errors.Add($"{fieldName} must contain at least {minimum} items");
        }

        return this;
    }

    /// <summary>
    /// Validates that a collection doesn't exceed the maximum number of items.
    /// </summary>
    public ValidationHelper RequireMaximumItems<T>(IEnumerable<T>? items, int maximum, string fieldName)
    {
        var count = items?.Count() ?? 0;
        if (count > maximum)
        {
            _errors.Add($"{fieldName} cannot contain more than {maximum} items");
        }

        return this;
    }

    /// <summary>
    /// Validates that a custom condition is true.
    /// </summary>
    public ValidationHelper RequireCondition(bool condition, string errorMessage)
    {
        if (!condition)
        {
            _errors.Add(errorMessage);
        }

        return this;
    }

    /// <summary>
    /// Validates that a string is a valid email address.
    /// </summary>
    public ValidationHelper RequireValidEmail(string? email, string fieldName = "Email")
    {
        if (!string.IsNullOrEmpty(email))
        {
            var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(email, emailPattern))
            {
                _errors.Add($"{fieldName} is not a valid email address");
            }
        }

        return this;
    }

    /// <summary>
    /// Validates that a string is a valid URL.
    /// </summary>
    public ValidationHelper RequireValidUrl(string? url, string fieldName = "Url")
    {
        if (!string.IsNullOrEmpty(url) && !Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            _errors.Add($"{fieldName} is not a valid URL");
        }

        return this;
    }

    /// <summary>
    /// Throws an exception if any validation errors exist.
    /// </summary>
    public void ThrowIfInvalid()
    {
        if (_errors.Count > 0)
        {
            throw new ValidationException(string.Join(Environment.NewLine, _errors));
        }
    }

    /// <summary>
    /// Gets all validation errors without throwing.
    /// </summary>
    public IReadOnlyList<string> GetErrors() => _errors.AsReadOnly();

    /// <summary>
    /// Determines if validation passed (no errors).
    /// </summary>
    public bool IsValid => _errors.Count == 0;
}

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
