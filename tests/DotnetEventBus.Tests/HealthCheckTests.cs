#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotnetEventBus.Monitoring;
using Xunit;

namespace DotnetEventBus.Tests;

/// <summary>
/// Unit tests for <see cref="HealthCheck"/>.
/// </summary>
public sealed class HealthCheckTests
{
    private sealed class TestProbe : IHealthCheckProbe
    {
        private readonly HealthStatus _status;
        private readonly string _message;
        private readonly Dictionary<string, object>? _details;
        private readonly Exception? _exception;

        public TestProbe(
            HealthStatus status = HealthStatus.Healthy,
            string? message = null,
            Dictionary<string, object>? details = null,
            Exception? exception = null)
        {
            _status = status;
            _message = message ?? $"Status: {status}";
            _details = details;
            _exception = exception;
        }

        public async Task<ProbeResult> CheckAsync()
        {
            if (_exception != null)
                throw _exception;

            await Task.Yield(); // simulate async work

            return new ProbeResult
            {
                Status = _status,
                Message = _message,
                Details = _details ?? new Dictionary<string, object>()
            };
        }
    }

    [Fact]
    public void RegisterProbe_NullName_ThrowsArgumentNullException()
    {
        var healthCheck = new HealthCheck();

        Assert.Throws<ArgumentNullException>(() => healthCheck.RegisterProbe(null!, new TestProbe()));
    }

    [Fact]
    public void RegisterProbe_NullProbe_ThrowsArgumentNullException()
    {
        var healthCheck = new HealthCheck();

        Assert.Throws<ArgumentNullException>(() => healthCheck.RegisterProbe("probe", null!));
    }

    [Fact]
    public async Task CheckHealthAsync_NoProbes_ReturnsHealthyResult()
    {
        var healthCheck = new HealthCheck();

        var result = await healthCheck.CheckHealthAsync();

        Assert.Equal(HealthStatus.Healthy, result.OverallStatus);
        Assert.Empty(result.Checks);
        Assert.Equal(HealthStatus.Healthy, healthCheck.GetLastStatus());
        Assert.True((DateTime.UtcNow - healthCheck.GetLastCheckTime()).TotalSeconds < 5);
    }

    [Fact]
    public async Task CheckHealthAsync_SingleHealthyProbe_ReturnsHealthy()
    {
        var healthCheck = new HealthCheck();
        healthCheck.RegisterProbe("healthy", new TestProbe(HealthStatus.Healthy));

        var result = await healthCheck.CheckHealthAsync();

        Assert.Equal(HealthStatus.Healthy, result.OverallStatus);
        Assert.Single(result.Checks);
        Assert.Contains("healthy", result.Checks.Keys);
        var probeResult = result.Checks["healthy"];
        Assert.Equal(HealthStatus.Healthy, probeResult.Status);
        Assert.NotNull(probeResult.Message);
        Assert.NotNull(probeResult.Details);
        Assert.Equal(HealthStatus.Healthy, healthCheck.GetLastStatus());
    }

    [Fact]
    public async Task CheckHealthAsync_ProbeThrows_ReturnsUnhealthyForThatProbe()
    {
        var healthCheck = new HealthCheck();
        healthCheck.RegisterProbe("faulty", new TestProbe(exception: new InvalidOperationException("boom")));

        var result = await healthCheck.CheckHealthAsync();

        Assert.Equal(HealthStatus.Unhealthy, result.OverallStatus);
        Assert.Single(result.Checks);
        var probeResult = result.Checks["faulty"];
        Assert.Equal(HealthStatus.Unhealthy, probeResult.Status);
        Assert.Contains("Probe failed", probeResult.Message);
        Assert.Equal(HealthStatus.Unhealthy, healthCheck.GetLastStatus());
    }

    [Fact]
    public async Task CheckHealthAsync_MixedProbes_ReturnsUnhealthyOverall()
    {
        var healthCheck = new HealthCheck();
        healthCheck.RegisterProbe("healthy", new TestProbe(HealthStatus.Healthy));
        healthCheck.RegisterProbe("degraded", new TestProbe(HealthStatus.Degraded));
        healthCheck.RegisterProbe("unhealthy", new TestProbe(HealthStatus.Unhealthy));

        var result = await healthCheck.CheckHealthAsync();

        // Any non‑healthy status makes the aggregate Unhealthy
        Assert.Equal(HealthStatus.Unhealthy, result.OverallStatus);
        Assert.Equal(3, result.Checks.Count);
        Assert.Equal(HealthStatus.Healthy, result.Checks["healthy"].Status);
        Assert.Equal(HealthStatus.Degraded, result.Checks["degraded"].Status);
        Assert.Equal(HealthStatus.Unhealthy, result.Checks["unhealthy"].Status);
        Assert.Equal(HealthStatus.Unhealthy, healthCheck.GetLastStatus());
    }

    [Fact]
    public void InitialState_ReturnsUnknownAndMinValue()
    {
        var healthCheck = new HealthCheck();

        Assert.Equal(HealthStatus.Unknown, healthCheck.GetLastStatus());
        Assert.Equal(DateTime.MinValue, healthCheck.GetLastCheckTime());
    }
}
