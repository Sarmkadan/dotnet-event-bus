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
    /// <param name="input">The string to convert.</param>
    /// <returns>The PascalCase formatted string, or empty string if input is null or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    public static string ToPascalCase(this string? input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var words = input.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Select(w => char.ToUpper(w[0], CultureInfo.InvariantCulture) + w.Substring(1).ToLower(CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// Converts a string to snake_case format.
    /// Example: "UserCreated" -> "user_created"
    /// </summary>
    /// <param name="input">The string to convert.</param>
    /// <returns>The snake_case formatted string, or empty string if input is null or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    public static string ToSnakeCase(this string? input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var pattern = new Regex(@"([a-z\d])([A-Z])");
        return pattern.Replace(input, "$1_$2").ToLower(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts a string to kebab-case format.
    /// Example: "UserCreated" -> "user-created"
    /// </summary>
    /// <param name="input">The string to convert.</param>
    /// <returns>The kebab-case formatted string, or empty string if input is null or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    public static string ToKebabCase(this string? input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return input.ToSnakeCase().Replace("_", "-");
    }

    /// <summary>
    /// Determines if a string is a valid event type name (alphanumeric with underscores/dots).
    /// </summary>
    /// <param name="input">The string to validate.</param>
    /// <returns>True if the string is a valid event type name; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    public static bool IsValidEventTypeName(this string? input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrEmpty(input) || input.Length > 256)
            return false;

        return Regex.IsMatch(input, @"^[a-zA-Z0-9._]+$");
    }

    /// <summary>
    /// Safely truncates a string to a maximum length with optional ellipsis.
    /// </summary>
    /// <param name="input">The string to truncate.</param>
    /// <param name="maxLength">The maximum length of the resulting string.</param>
    /// <param name="addEllipsis">Whether to add ellipsis (...) at the end.</param>
    /// <returns>The truncated string, or the original string if it's shorter than maxLength.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxLength"/> is negative.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    public static string Truncate(this string? input, int maxLength, bool addEllipsis = false)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (maxLength < 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength), "Max length must be non-negative");

        if (input.Length <= maxLength)
            return input;

        var truncated = input.Substring(0, maxLength);
        return addEllipsis ? truncated + "..." : truncated;
    }

    /// <summary>
    /// Checks if a string is null or contains only whitespace.
    /// </summary>
    /// <param name="input">The string to check.</param>
    /// <returns>True if the string is null or whitespace; otherwise, false.</returns>
    public static bool IsNullOrWhitespace(this string? input) => string.IsNullOrWhiteSpace(input);

    /// <summary>
    /// Converts a string to a slug-friendly format for URLs.
    /// Removes special characters and spaces.
    /// </summary>
    /// <param name="input">The string to convert to a slug.</param>
    /// <returns>The slugified string, or empty string if input is null or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    public static string ToSlug(this string? input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var slug = Regex.Replace(input.ToLower(CultureInfo.InvariantCulture), @"[^a-z0-9\-]+", "-").TrimEnd('-');
        return Regex.Replace(slug, @"-+", "-");
    }

    /// <summary>
    /// Extracts the event category from an event type name.
    /// Example: "user.created" -> "user" or "UserCreated" -> "User"
    /// </summary>
    /// <param name="eventType">The event type name to extract category from.</param>
    /// <returns>The extracted category, or empty string if input is null or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="eventType"/> is null.</exception>
    public static string GetEventCategory(this string? eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        if (string.IsNullOrEmpty(eventType))
            return string.Empty;

        return eventType.Contains('.')
            ? eventType.Split('.')[0]
            : Regex.Match(eventType, @"^[A-Z][a-zA-Z]*").Value;
    }

    /// <summary>
    /// Repeats a string a specified number of times.
    /// </summary>
    /// <param name="input">The string to repeat.</param>
    /// <param name="count">The number of times to repeat the string.</param>
    /// <returns>The repeated string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
    public static string Repeat(this string? input, int count)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");

        return string.Concat(Enumerable.Repeat(input, count));
    }
}
