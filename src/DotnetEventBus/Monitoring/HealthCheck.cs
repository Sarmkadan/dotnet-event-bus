#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotnetEventBus.Monitoring;

/// <summary>
/// Monitors the health of the event bus system.
/// Performs periodic checks on critical components and reports status.
/// Why: Enables automated detection of system degradation and failures.
/// </summary>
public sealed class HealthCheck
{
    private readonly Dictionary<string, IHealthCheckProbe> _probes = [];
    private HealthStatus _lastStatus = HealthStatus.Unknown;
    private DateTime _lastCheckTime = DateTime.MinValue;

    /// <summary>
    /// Registers a health check probe.
    /// </summary>
    public void RegisterProbe(string name, IHealthCheckProbe probe)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(probe);

        _probes[name] = probe;
    }

    /// <summary>
    /// Performs all health checks and returns the aggregate status.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync()
    {
        var result = new HealthCheckResult();
        var allProbesHealthy = true;

        foreach (var kvp in _probes)
        {
            try
            {
                var probeResult = await kvp.Value.CheckAsync();
                result.Checks[kvp.Key] = probeResult;

                if (probeResult.Status != HealthStatus.Healthy)
                {
                    allProbesHealthy = false;
                }
            }
            catch (Exception ex)
            {
                result.Checks[kvp.Key] = new ProbeResult
                {
                    Status = HealthStatus.Unhealthy,
                    Message = $"Probe failed: {ex.Message}"
                };

                allProbesHealthy = false;
            }
        }

        result.OverallStatus = allProbesHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy;
        result.CheckedAt = DateTime.UtcNow;

        _lastStatus = result.OverallStatus;
        _lastCheckTime = result.CheckedAt;

        return result;
    }

    /// <summary>
    /// Gets the last health check status.
    /// </summary>
    public HealthStatus GetLastStatus() => _lastStatus;

    /// <summary>
    /// Gets the time of the last health check.
    /// </summary>
    public DateTime GetLastCheckTime() => _lastCheckTime;
}

/// <summary>
/// Interface for health check probes.
/// </summary>
public interface IHealthCheckProbe
{
    Task<ProbeResult> CheckAsync();
}

/// <summary>
/// Result of a single health check probe.
/// </summary>
public sealed class ProbeResult
{
    public HealthStatus Status { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, object> Details { get; set; } = [];
}

/// <summary>
/// Result of the overall health check.
/// </summary>
public sealed class HealthCheckResult
{
    public HealthStatus OverallStatus { get; set; }
    public DateTime CheckedAt { get; set; }
    public Dictionary<string, ProbeResult> Checks { get; set; } = [];
}

/// <summary>
/// Enumeration of health statuses.
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}

/// <summary>
/// Built-in health check probes.
/// </summary>
public static class BuiltInProbes
{
    /// <summary>
    /// Creates a probe that checks memory usage.
    /// </summary>
    public static IHealthCheckProbe CreateMemoryProbe(long warningThresholdBytes = 1_073_741_824)
    {
        return new MemoryHealthProbe(warningThresholdBytes);
    }

    /// <summary>
    /// Creates a probe that checks event bus responsiveness.
    /// </summary>
    public static IHealthCheckProbe CreateResponsivenessProbe()
    {
        return new ResponsivenessProbe();
    }

    private class MemoryHealthProbe : IHealthCheckProbe
    {
        private readonly long _warningThreshold;

        public MemoryHealthProbe(long warningThreshold)
        {
            _warningThreshold = warningThreshold;
        }

        public async Task<ProbeResult> CheckAsync()
        {
            await Task.Yield();

            var currentMemory = GC.GetTotalMemory(false);
            var status = currentMemory > _warningThreshold ? HealthStatus.Degraded : HealthStatus.Healthy;

            return new ProbeResult
            {
                Status = status,
                Message = $"Memory usage: {currentMemory / 1024 / 1024}MB",
                Details = new Dictionary<string, object>
                {
                    { "memoryBytes", currentMemory },
                    { "warningThresholdBytes", _warningThreshold }
                }
            };
        }
    }

    private class ResponsivenessProbe : IHealthCheckProbe
    {
        public async Task<ProbeResult> CheckAsync()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await Task.Delay(10);
            sw.Stop();

            var status = sw.ElapsedMilliseconds > 100 ? HealthStatus.Degraded : HealthStatus.Healthy;

            return new ProbeResult
            {
                Status = status,
                Message = $"Responsiveness: {sw.ElapsedMilliseconds}ms",
                Details = new Dictionary<string, object>
                {
                    { "latencyMs", sw.ElapsedMilliseconds }
                }
            };
        }
    }
}
