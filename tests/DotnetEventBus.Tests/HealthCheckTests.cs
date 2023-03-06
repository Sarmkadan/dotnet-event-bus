// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetEventBus.Monitoring;
using FluentAssertions;
using Moq;

namespace DotnetEventBus.Tests;

public class HealthCheckTests
{
    [Fact]
    public void GetLastStatus_BeforeAnyCheck_ReturnsUnknown()
    {
        var healthCheck = new HealthCheck();
        healthCheck.GetLastStatus().Should().Be(HealthStatus.Unknown);
    }

    [Fact]
    public void GetLastCheckTime_BeforeAnyCheck_ReturnsMinValue()
    {
        var healthCheck = new HealthCheck();
        healthCheck.GetLastCheckTime().Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void RegisterProbe_NullName_ThrowsArgumentNullException()
    {
        var healthCheck = new HealthCheck();
        var probe = new Mock<IHealthCheckProbe>();
        var act = () => healthCheck.RegisterProbe(null!, probe.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisterProbe_NullProbe_ThrowsArgumentNullException()
    {
        var healthCheck = new HealthCheck();
        var act = () => healthCheck.RegisterProbe("test", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CheckHealthAsync_NoProbes_ReturnsHealthy()
    {
        var healthCheck = new HealthCheck();
        var result = await healthCheck.CheckHealthAsync();
        result.OverallStatus.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_AllProbesHealthy_ReturnsHealthy()
    {
        var healthCheck = new HealthCheck();
        var probe = new Mock<IHealthCheckProbe>();
        probe.Setup(p => p.CheckAsync()).ReturnsAsync(new ProbeResult { Status = HealthStatus.Healthy });

        healthCheck.RegisterProbe("test-probe", probe.Object);
        var result = await healthCheck.CheckHealthAsync();

        result.OverallStatus.Should().Be(HealthStatus.Healthy);
        result.Checks.Should().ContainKey("test-probe");
    }

    [Fact]
    public async Task CheckHealthAsync_OneProbeUnhealthy_ReturnsUnhealthy()
    {
        var healthCheck = new HealthCheck();
        var healthyProbe = new Mock<IHealthCheckProbe>();
        healthyProbe.Setup(p => p.CheckAsync()).ReturnsAsync(new ProbeResult { Status = HealthStatus.Healthy });

        var unhealthyProbe = new Mock<IHealthCheckProbe>();
        unhealthyProbe.Setup(p => p.CheckAsync()).ReturnsAsync(new ProbeResult { Status = HealthStatus.Unhealthy, Message = "db down" });

        healthCheck.RegisterProbe("healthy", healthyProbe.Object);
        healthCheck.RegisterProbe("unhealthy", unhealthyProbe.Object);

        var result = await healthCheck.CheckHealthAsync();
        result.OverallStatus.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ProbeThrowsException_MarksAsUnhealthy()
    {
        var healthCheck = new HealthCheck();
        var probe = new Mock<IHealthCheckProbe>();
        probe.Setup(p => p.CheckAsync()).ThrowsAsync(new Exception("connection refused"));

        healthCheck.RegisterProbe("failing", probe.Object);
        var result = await healthCheck.CheckHealthAsync();

        result.OverallStatus.Should().Be(HealthStatus.Unhealthy);
        result.Checks["failing"].Message.Should().Contain("connection refused");
    }

    [Fact]
    public async Task CheckHealthAsync_UpdatesLastStatusAndTime()
    {
        var healthCheck = new HealthCheck();
        var before = DateTime.UtcNow;
        await healthCheck.CheckHealthAsync();

        healthCheck.GetLastStatus().Should().Be(HealthStatus.Healthy);
        healthCheck.GetLastCheckTime().Should().BeOnOrAfter(before);
    }
}
