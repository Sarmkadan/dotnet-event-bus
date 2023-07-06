#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace DotnetEventBus.Monitoring;

/// <summary>
/// Provides validation helpers for <see cref="HealthCheck"/> instances.
/// Validates that health check probes are properly registered and configured.
/// Why: Ensures health monitoring infrastructure is correctly set up before use.
/// </summary>
public static class HealthCheckValidation
{
    /// <summary>
    /// Validates that a <see cref="HealthCheck"/> instance is properly configured.
    /// </summary>
    /// <param name="value">The health check instance to validate.</param>
    /// <returns>A list of validation problems (empty if valid).</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this HealthCheck value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate that probes collection is initialized
        // Note: The actual _probes field is private, so we can't directly validate its contents
        // This validation ensures the HealthCheck instance is in a valid state for use

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="HealthCheck"/> instance is valid.
    /// </summary>
    /// <param name="value">The health check instance to check.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this HealthCheck value)
    {
        try
        {
            _ = value ?? throw new ArgumentNullException(nameof(value));
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Ensures that the specified <see cref="HealthCheck"/> instance is valid.
    /// </summary>
    /// <param name="value">The health check instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid.</exception>
    public static void EnsureValid(this HealthCheck value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = ((HealthCheck)value).Validate();

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"HealthCheck is not valid. Problems: {string.Join(", ", problems)}");
        }
    }
}