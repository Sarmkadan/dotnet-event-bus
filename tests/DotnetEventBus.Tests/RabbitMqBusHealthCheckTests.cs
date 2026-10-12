using System;
using System.Linq;
using System.Threading.Tasks;
using DotnetEventBus.Monitoring;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

// The ASP.NET HealthStatus is aliased because DotnetEventBus.Monitoring declares its own HealthStatus.
using AspNetHealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace DotnetEventBus.Tests;

public class RabbitMqBusHealthCheckTests
{
    private static readonly HealthCheckContext Context = new();

    [Theory]
    [InlineData(RabbitMqConnectionState.Open, AspNetHealthStatus.Healthy)]
    [InlineData(RabbitMqConnectionState.Reconnecting, AspNetHealthStatus.Degraded)]
    [InlineData(RabbitMqConnectionState.Closed, AspNetHealthStatus.Unhealthy)]
    public async Task CheckHealthAsync_MapsConnectionStateToStatus(
        RabbitMqConnectionState state,
        AspNetHealthStatus expected)
    {
        // Arrange
        var monitor = new FakeRabbitMqConnectionMonitor { State = state };
        var check = new RabbitMqBusHealthCheck(monitor);

        // Act
        var result = await check.CheckHealthAsync(Context);

        // Assert
        result.Status.Should().Be(expected);
        result.Data["connectionState"].Should().Be(state.ToString());
    }

    [Fact]
    public async Task CheckHealthAsync_ReflectsStateChangesBetweenChecks()
    {
        // Arrange
        var monitor = new FakeRabbitMqConnectionMonitor { State = RabbitMqConnectionState.Open };
        var check = new RabbitMqBusHealthCheck(monitor);

        // Act
        var before = await check.CheckHealthAsync(Context);
        monitor.State = RabbitMqConnectionState.Reconnecting;
        var during = await check.CheckHealthAsync(Context);
        monitor.State = RabbitMqConnectionState.Open;
        var after = await check.CheckHealthAsync(Context);

        // Assert
        before.Status.Should().Be(AspNetHealthStatus.Healthy);
        during.Status.Should().Be(AspNetHealthStatus.Degraded);
        after.Status.Should().Be(AspNetHealthStatus.Healthy);
    }

    [Fact]
    public void Constructor_NullMonitor_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new RabbitMqBusHealthCheck(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddEventBusHealthCheck_RegistersCheckUnderDefaultName()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSingleton<IRabbitMqConnectionMonitor>(new FakeRabbitMqConnectionMonitor());
        services.AddHealthChecks().AddEventBusHealthCheck();
        var provider = services.BuildServiceProvider();

        // Act
        var registration = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value.Registrations.Single();

        // Assert
        registration.Name.Should().Be("eventbus");
        registration.Factory(provider).Should().BeOfType<RabbitMqBusHealthCheck>();
    }

    [Fact]
    public async Task AddEventBusHealthCheck_ReportsDegradedThroughHealthCheckService()
    {
        // Arrange
        var monitor = new FakeRabbitMqConnectionMonitor { State = RabbitMqConnectionState.Reconnecting };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IRabbitMqConnectionMonitor>(monitor);
        services.AddHealthChecks().AddEventBusHealthCheck(tags: ["messaging"]);
        var healthCheckService = services.BuildServiceProvider().GetRequiredService<HealthCheckService>();

        // Act
        var report = await healthCheckService.CheckHealthAsync();

        // Assert
        report.Status.Should().Be(AspNetHealthStatus.Degraded);
        report.Entries["eventbus"].Tags.Should().Contain("messaging");
    }

    [Fact]
    public async Task AddEventBusHealthCheck_WithoutMonitor_ReportsUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks().AddEventBusHealthCheck();
        var healthCheckService = services.BuildServiceProvider().GetRequiredService<HealthCheckService>();

        // Act
        var report = await healthCheckService.CheckHealthAsync();

        // Assert
        report.Status.Should().Be(AspNetHealthStatus.Unhealthy);
    }

    [Fact]
    public async Task AddEventBusHealthCheck_KeepsMonitorRegisteredBeforehand()
    {
        // Arrange
        var monitor = new FakeRabbitMqConnectionMonitor { State = RabbitMqConnectionState.Open };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IRabbitMqConnectionMonitor>(monitor);
        services.AddHealthChecks().AddEventBusHealthCheck();
        var healthCheckService = services.BuildServiceProvider().GetRequiredService<HealthCheckService>();

        // Act
        var report = await healthCheckService.CheckHealthAsync();

        // Assert
        report.Status.Should().Be(AspNetHealthStatus.Healthy);
    }

    [Fact]
    public void AddEventBusHealthCheck_NullBuilder_ThrowsArgumentNullException()
    {
        // Act
        var act = () => ((IHealthChecksBuilder)null!).AddEventBusHealthCheck();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// A fake connection state whose value the test sets directly.
    /// </summary>
    private sealed class FakeRabbitMqConnectionMonitor : IRabbitMqConnectionMonitor
    {
        public RabbitMqConnectionState State { get; set; } = RabbitMqConnectionState.Open;
    }
}
