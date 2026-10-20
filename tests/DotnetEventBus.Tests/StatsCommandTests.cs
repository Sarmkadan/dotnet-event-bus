#nullable enable

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using DotnetEventBus.Advanced;
using DotnetEventBus.Cli;

namespace DotnetEventBus.Tests;

/// <summary>
/// Unit tests for the <see cref="StatsCommand"/> class.
/// </summary>
public sealed class StatsCommandTests
{
    [Fact]
    public void Ctor_NullMetrics_ThrowsArgumentNullException()
    {
        var act = () => new StatsCommand(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("metrics");
    }

    [Fact]
    public async Task ExecuteAsync_NoMetricsCollector_ReturnsFailure()
    {
        var command = new StatsCommand();
        var result = await command.ExecuteAsync(Array.Empty<string>());

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No metrics collector");
    }

    [Fact]
    public async Task ExecuteAsync_SystemStats_ReturnsSuccess()
    {
        var metrics = new MetricsCollector();
        metrics.RecordEventPublished("OrderCreated", 42);
        var command = new StatsCommand(metrics);

        var result = await command.ExecuteAsync(new[] { "system" });

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("totalPublished");
        result.Message.Should().Contain("successRate");
    }

    [Fact]
    public async Task ExecuteAsync_DefaultIsSystem_ReturnsSuccess()
    {
        var metrics = new MetricsCollector();
        var command = new StatsCommand(metrics);

        var result = await command.ExecuteAsync(Array.Empty<string>());

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("system");
    }

    [Fact]
    public async Task ExecuteAsync_EventsStats_ReturnsTopEvents()
    {
        var metrics = new MetricsCollector();
        metrics.RecordEventPublished("OrderCreated", 10);
        metrics.RecordEventPublished("OrderCreated", 20);
        metrics.RecordEventPublished("PaymentProcessed", 30);
        var command = new StatsCommand(metrics);

        var result = await command.ExecuteAsync(new[] { "events" });

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("OrderCreated");
        result.Message.Should().Contain("PaymentProcessed");
    }

    [Fact]
    public async Task ExecuteAsync_HandlersStats_ReturnsHandlerInfo()
    {
        var metrics = new MetricsCollector();
        metrics.RecordHandlerExecution("OrderHandler", "OrderCreated", 15, true);
        var command = new StatsCommand(metrics);

        var result = await command.ExecuteAsync(new[] { "handlers" });

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("OrderHandler");
    }

    [Fact]
    public async Task ExecuteAsync_HealthStats_ReturnsHealthStatus()
    {
        var metrics = new MetricsCollector();
        var command = new StatsCommand(metrics);

        var result = await command.ExecuteAsync(new[] { "health" });

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Healthy");
    }

    [Fact]
    public async Task ExecuteAsync_UnknownType_ReturnsFailure()
    {
        var metrics = new MetricsCollector();
        var command = new StatsCommand(metrics);

        var result = await command.ExecuteAsync(new[] { "nonexistent" });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Unknown stats type");
    }

    [Fact]
    public void GetHelpText_ReturnsUsageInfo()
    {
        var command = new StatsCommand();
        var help = command.GetHelpText();

        help.Should().Contain("stats");
        help.Should().Contain("system");
        help.Should().Contain("events");
        help.Should().Contain("handlers");
        help.Should().Contain("health");
    }
}
