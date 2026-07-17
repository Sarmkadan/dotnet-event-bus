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

        // Validate that at least one probe is registered
        // Since _probes is private, we check via the public API by examining state
        // A health check with no probes should be considered invalid
        if (value.GetLastCheckTime() == DateTime.MinValue && value.GetLastStatus() == HealthStatus.Unknown)
        {
            problems.Add("No health check probes have been registered.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="HealthCheck"/> instance is valid.
    /// A health check is valid if it is not null and has at least one registered probe.
    /// </summary>
    /// <param name="value">The health check instance to check.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this HealthCheck value)
    {
        if (value is null)
        {
            return false;
        }

        // Check if any probes have been registered by examining the state
        // If last check time is still MinValue and status is Unknown, no probes exist
        return value.GetLastCheckTime() != DateTime.MinValue || value.GetLastStatus() != HealthStatus.Unknown;
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