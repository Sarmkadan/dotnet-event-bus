#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DotnetEventBus.Exceptions;

namespace DotnetEventBus.Utilities;

/// <summary>
/// Provides guard clauses for validating method arguments and object states.
/// This class consolidates validation logic across the event bus to ensure consistency.
/// </summary>
internal static class Guard
{
    /// <summary>
    /// Validates that an object reference is not null.
    /// </summary>
    /// <typeparam name="T">The type of the object to validate.</typeparam>
    /// <param name="value">The object reference to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static void NotNull<T>([NotNull] T? value, string paramName) where T : class
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
    }

    /// <summary>
    /// Validates that a string is not null or empty.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty.</exception>
    public static void NotNullOrEmpty([NotNull] string? value, string paramName)
    {
        ArgumentException.ThrowIfNullOrEmpty(value, paramName);
    }

    /// <summary>
    /// Validates that a string is not null, empty, or whitespace.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty or whitespace.</exception>
    public static void NotNullOrWhitespace([NotNull] string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("String cannot be null, empty, or whitespace.", paramName);
        }
    }

    /// <summary>
    /// Validates that a value is within a specified range.
    /// </summary>
    /// <typeparam name="T">The type of the value to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum allowed value (inclusive).</param>
    /// <param name="max">The maximum allowed value (inclusive).</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is outside the specified range.</exception>
    public static void InRange<T>(T value, T min, T max, string paramName) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"Value must be between {min} and {max} (inclusive).");
        }
    }

    /// <summary>
    /// Validates that a value is greater than a specified minimum.
    /// </summary>
    /// <typeparam name="T">The type of the value to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum allowed value (exclusive).</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is less than or equal to <paramref name="min"/>.</exception>
    public static void GreaterThan<T>(T value, T min, string paramName) where T : IComparable<T>
    {
        if (value.CompareTo(min) <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"Value must be greater than {min}.");
        }
    }

    /// <summary>
    /// Validates that a value is greater than or equal to a specified minimum.
    /// </summary>
    /// <typeparam name="T">The type of the value to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum allowed value (inclusive).</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is less than <paramref name="min"/>.</exception>
    public static void GreaterThanOrEqualTo<T>(T value, T min, string paramName) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"Value must be greater than or equal to {min}.");
        }
    }

    /// <summary>
    /// Validates that a value is less than a specified maximum.
    /// </summary>
    /// <typeparam name="T">The type of the value to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="max">The maximum allowed value (exclusive).</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is greater than or equal to <paramref name="max"/>.</exception>
    public static void LessThan<T>(T value, T max, string paramName) where T : IComparable<T>
    {
        if (value.CompareTo(max) >= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"Value must be less than {max}.");
        }
    }

    /// <summary>
    /// Validates that a value is less than or equal to a specified maximum.
    /// </summary>
    /// <typeparam name="T">The type of the value to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="max">The maximum allowed value (inclusive).</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is greater than <paramref name="max"/>.</exception>
    public static void LessThanOrEqualTo<T>(T value, T max, string paramName) where T : IComparable<T>
    {
        if (value.CompareTo(max) > 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"Value must be less than or equal to {max}.");
        }
    }

    /// <summary>
    /// Validates that a TimeSpan is positive.
    /// </summary>
    /// <param name="value">The TimeSpan to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is not positive.</exception>
    public static void PositiveTimeSpan(TimeSpan value, string paramName)
    {
        if (value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "TimeSpan must be positive.");
        }
    }

    /// <summary>
    /// Validates that a collection is not null or empty.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="collection"/> is empty.</exception>
    public static void NotNullOrEmpty<T>(IReadOnlyCollection<T>? collection, string paramName)
    {
        NotNull(collection, paramName);

        if (collection.Count == 0)
        {
            throw new ArgumentException("Collection cannot be empty.", paramName);
        }
    }

    /// <summary>
    /// Validates that a condition is true, throwing a <see cref="ValidationException"/> if false.
    /// This is used for domain/business rule validation.
    /// </summary>
    /// <param name="condition">The condition to validate.</param>
    /// <param name="message">The error message to include if the condition is false.</param>
    /// <exception cref="ValidationException">Thrown when <paramref name="condition"/> is false.</exception>
    public static void Domain([DoesNotReturnIf(false)] bool condition, string message)
    {
        if (!condition)
        {
            throw new ValidationException(message);
        }
    }

    /// <summary>
    /// Validates that a condition is true, throwing a <see cref="ConfigurationException"/> if false.
    /// This is used for configuration validation.
    /// </summary>
    /// <param name="condition">The condition to validate.</param>
    /// <param name="message">The error message to include if the condition is false.</param>
    /// <exception cref="ConfigurationException">Thrown when <paramref name="condition"/> is false.</exception>
    public static void Configuration([DoesNotReturnIf(false)] bool condition, string message)
    {
        if (!condition)
        {
            throw new ConfigurationException(message);
        }
    }
}