#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DotnetEventBus.Utilities;

/// <summary>
/// Extension methods for string manipulation and validation.
/// Provides common operations used throughout the event bus infrastructure.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts a string to PascalCase format.
    /// Example: "user_created" -> "UserCreated"
    /// </summary>
    public static string ToPascalCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var words = input.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Select(w => char.ToUpper(w[0], CultureInfo.InvariantCulture) + w.Substring(1).ToLower(CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// Converts a string to snake_case format.
    /// Example: "UserCreated" -> "user_created"
    /// </summary>
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var pattern = new Regex(@"([a-z\d])([A-Z])");
        return pattern.Replace(input, "$1_$2").ToLower(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts a string to kebab-case format.
    /// Example: "UserCreated" -> "user-created"
    /// </summary>
    public static string ToKebabCase(this string input)
    {
        return input.ToSnakeCase().Replace("_", "-");
    }

    /// <summary>
    /// Determines if a string is a valid event type name (alphanumeric with underscores/dots).
    /// </summary>
    public static bool IsValidEventTypeName(this string input)
    {
        if (string.IsNullOrEmpty(input) || input.Length > 256)
            return false;

        return Regex.IsMatch(input, @"^[a-zA-Z0-9._]+$");
    }

    /// <summary>
    /// Safely truncates a string to a maximum length with optional ellipsis.
    /// </summary>
    public static string Truncate(this string input, int maxLength, bool addEllipsis = false)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input;

        var truncated = input.Substring(0, maxLength);
        return addEllipsis ? truncated + "..." : truncated;
    }

    /// <summary>
    /// Checks if a string is null or contains only whitespace.
    /// </summary>
    public static bool IsNullOrWhitespace(this string? input) => string.IsNullOrWhiteSpace(input);

    /// <summary>
    /// Converts a string to a slug-friendly format for URLs.
    /// Removes special characters and spaces.
    /// </summary>
    public static string ToSlug(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var slug = Regex.Replace(input.ToLower(CultureInfo.InvariantCulture), @"[^a-z0-9\-]+", "-").TrimEnd('-');
        return Regex.Replace(slug, @"-+", "-");
    }

    /// <summary>
    /// Extracts the event category from an event type name.
    /// Example: "user.created" -> "user" or "UserCreated" -> "User"
    /// </summary>
    public static string GetEventCategory(this string eventType)
    {
        if (string.IsNullOrEmpty(eventType))
            return string.Empty;

        return eventType.Contains('.')
            ? eventType.Split('.')[0]
            : Regex.Match(eventType, @"^[A-Z]+(?=[A-Z][a-z]|\b)").Value;
    }

    /// <summary>
    /// Repeats a string a specified number of times.
    /// </summary>
    public static string Repeat(this string input, int count)
    {
        if (count < 0)
            throw new ArgumentException("Count must be non-negative", nameof(count));

        return string.Concat(Enumerable.Repeat(input, count));
    }
}
