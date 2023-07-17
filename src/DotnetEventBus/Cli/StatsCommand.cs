#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DotnetEventBus.Advanced;

namespace DotnetEventBus.Cli;

/// <summary>
/// CLI command for viewing system statistics and health.
/// Displays metrics about event processing and system status.
/// </summary>
public sealed class StatsCommand : ICommand
{
    private readonly MetricsCollector? _metrics;

    public StatsCommand()
    {
    }

    /// <summary>
    /// Creates a stats command bound to a concrete metrics collector.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="metrics"/> is null.</exception>
    public StatsCommand(MetricsCollector metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        _metrics = metrics;
    }

    public string Name => "stats";
    public string Description => "Display event bus statistics and metrics";

    public async Task<CommandResult> ExecuteAsync(string[] args)
    {
        await Task.CompletedTask; // Keep async signature

        if (_metrics is null)
        {
            return new CommandResult(false,
                "No metrics collector is attached to this CLI. Construct the CLI with a MetricsCollector instance to view statistics.");
        }

        var statsType = args.Length > 0 ? args[0].ToLower() : "system";

        return statsType switch
        {
            "system" => GetSystemStats(),
            "events" => GetEventStats(),
            "handlers" => GetHandlerStats(),
            "health" => GetHealthStats(),
            _ => new CommandResult(false, $"Unknown stats type: {statsType}")
        };
    }

    public string GetHelpText()
    {
        return @"
Usage: stats [type]

Description:
  Display event bus statistics and metrics.

Types:
  system    Show system-wide statistics (default)
  events    Show per-event-type statistics
  handlers  Show per-handler statistics
  health    Show system health status

Examples:
  stats
  stats system
  stats events
  stats health
";
    }

    private CommandResult GetSystemStats()
    {
        var system = _metrics!.GetSystemMetrics();
        var allEvents = _metrics.GetAllEventMetrics().ToList();
        var averageLatency = allEvents.Count > 0 ? allEvents.Average(m => m.AverageDurationMs) : 0;

        var stats = new
        {
            system = new
            {
                uptime = system.UpTime.ToString(@"d\.hh\:mm\:ss", CultureInfo.InvariantCulture),
                startTime = system.StartTime.ToString("o", CultureInfo.InvariantCulture),
                status = DetermineStatus(system),
                throughputPerSecond = Math.Round(system.ThroughputPerSecond, 2)
            },
            events = new
            {
                totalPublished = system.TotalEventsPublished,
                totalFailed = system.TotalEventsFailed,
                successRate = Math.Round(system.SuccessRate, 2),
                averageLatencyMs = Math.Round(averageLatency, 2)
            },
            handlers = new
            {
                registered = system.HandlersCount,
                eventTypes = system.EventTypesCount
            }
        };

        var json = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
        return new CommandResult(true, json);
    }

    private CommandResult GetEventStats()
    {
        var stats = new
        {
            topEvents = _metrics!.GetAllEventMetrics()
                .OrderByDescending(m => m.PublishCount)
                .Take(10)
                .Select(m => new
                {
                    type = m.EventType,
                    count = m.PublishCount,
                    failures = m.FailureCount,
                    avgLatencyMs = Math.Round(m.AverageDurationMs, 2)
                })
                .ToArray()
        };

        var json = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
        return new CommandResult(true, json);
    }

    private CommandResult GetHandlerStats()
    {
        var stats = new
        {
            handlers = _metrics!.GetAllHandlerMetrics()
                .Select(m => new
                {
                    name = m.HandlerName,
                    eventType = m.EventType,
                    eventsProcessed = m.ExecutionCount,
                    failures = m.FailureCount,
                    avgLatencyMs = Math.Round(m.AverageDurationMs, 2)
                })
                .ToArray()
        };

        var json = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
        return new CommandResult(true, json);
    }

    private CommandResult GetHealthStats()
    {
        var system = _metrics!.GetSystemMetrics();

        var stats = new
        {
            status = DetermineStatus(system),
            events = new
            {
                published = system.TotalEventsPublished,
                failed = system.TotalEventsFailed,
                successRate = Math.Round(system.SuccessRate, 2)
            },
            timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
        };

        var json = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
        return new CommandResult(true, json);
    }

    private static string DetermineStatus(SystemMetrics system)
    {
        if (system.TotalEventsPublished == 0)
        {
            return "Healthy";
        }

        return system.SuccessRate switch
        {
            < 50 => "Unhealthy",
            < 95 => "Degraded",
            _ => "Healthy"
        };
    }
}
