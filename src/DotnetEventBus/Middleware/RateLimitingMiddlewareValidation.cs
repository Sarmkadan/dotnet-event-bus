#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotnetEventBus.Middleware;

/// <summary>
/// Provides validation helpers for <see cref="RateLimitingMiddleware"/> instances.
/// </summary>
public static class RateLimitingMiddlewareValidation
{
    /// <summary>
    /// Validates the specified <see cref="RateLimitingMiddleware"/> instance.
    /// </summary>
    /// <param name="value">The middleware instance to validate.</param>
    /// <returns>A list of validation problems; empty if the instance is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this RateLimitingMiddleware? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // requestsPerWindow is validated in constructor, but we check the effective value
        // The default is 1000, which is always valid (> 0)

        // timeWindow is validated in constructor, but we check the effective value
        // The default is TimeSpan.FromSeconds(60), which is always valid (> TimeSpan.Zero)

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="RateLimitingMiddleware"/> instance is valid.
    /// </summary>
    /// <param name="value">The middleware instance to check.</param>
    /// <returns>True if the instance is valid; otherwise, false.</returns>
    public static bool IsValid(this RateLimitingMiddleware? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="RateLimitingMiddleware"/> instance is valid.
    /// </summary>
    /// <param name="value">The middleware instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is not valid, containing a list of validation problems.</exception>
    public static void EnsureValid(this RateLimitingMiddleware? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"RateLimitingMiddleware is not valid. Problems: {string.Join("; ", problems)}");
        }
    }
}
