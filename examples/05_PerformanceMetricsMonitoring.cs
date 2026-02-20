#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotnetEventBus;
using DotnetEventBus.Handlers;
using System.Diagnostics;

namespace DotnetEventBus.Examples;

/// <summary>
/// Performance Metrics and Monitoring: Demonstrates system metrics collection,
/// handler performance profiling, and real-time monitoring capabilities.
/// </summary>
public static class PerformanceMetricsMonitoringExample
{
    public sealed class DataProcessingEvent
    {
        public string ProcessId { get; set; }
        public int DataSize { get; set; }
        public string ProcessType { get; set; }
    }

    // Fast handler
    public sealed class FastProcessorHandler : EventHandlerBase<DataProcessingEvent>
    {
        public override async Task Handle(DataProcessingEvent @event, CancellationToken cancellationToken = default)
        {
            await Task.Delay(10); // Quick processing
        }
    }

    // Medium handler
    public sealed class MediumProcessorHandler : EventHandlerBase<DataProcessingEvent>
    {
        public override async Task Handle(DataProcessingEvent @event, CancellationToken cancellationToken = default)
        {
            await Task.Delay(50); // Medium processing
        }
    }

    // Slow handler
    public sealed class SlowProcessorHandler : EventHandlerBase<DataProcessingEvent>
    {
        public override async Task Handle(DataProcessingEvent @event, CancellationToken cancellationToken = default)
        {
            await Task.Delay(100); // Slower processing
        }
    }

    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== DotnetEventBus: Performance Metrics & Monitoring ===\n");

        var services = new ServiceCollection();
        services.AddEventBus(options =>
        {
            options.AllowParallelHandling = false;
            options.EnableMetrics = true;
            options.EnableDetailedLogging = true;
        });

        var serviceProvider = services.BuildServiceProvider();
        var eventBus = serviceProvider.GetRequiredService<IEventBus>();
        var metricsCollector = serviceProvider.GetRequiredService<IMetricsCollector>();
        var performanceProfiler = serviceProvider.GetRequiredService<IPerformanceProfiler>();

        // Simulate multiple event publish operations
        Console.WriteLine("--- Publishing Events for Metrics Collection ---\n");

        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < 10; i++)
        {
            var @event = new DataProcessingEvent
            {
                ProcessId = $"PROC-{i:D3}",
                DataSize = 1000 + (i * 100),
                ProcessType = i % 3 switch
                {
                    0 => "Fast",
                    1 => "Medium",
                    _ => "Slow"
                }
            };

            var result = await eventBus.PublishAsync(@event);
        }

        stopwatch.Stop();

        // Collect and display metrics
        Console.WriteLine("--- System Metrics ---\n");
        var systemMetrics = metricsCollector.GetSystemMetrics();

        Console.WriteLine($"Total Events Published: {systemMetrics.TotalEventsPublished}");
        Console.WriteLine($"Total Events Failed: {systemMetrics.TotalEventsFailed}");
        Console.WriteLine($"Success Rate: {systemMetrics.SuccessRate:P2}");
        Console.WriteLine($"Average Latency: {systemMetrics.AverageLatency:F2}ms");
        Console.WriteLine($"Min Latency: {systemMetrics.MinLatency:F2}ms");
        Console.WriteLine($"Max Latency: {systemMetrics.MaxLatency:F2}ms");
        Console.WriteLine($"P95 Latency: {systemMetrics.P95Latency:F2}ms");
        Console.WriteLine($"P99 Latency: {systemMetrics.P99Latency:F2}ms");
        Console.WriteLine($"Throughput: {systemMetrics.AverageThroughput:F2} events/sec\n");

        // Get handler-specific metrics
        Console.WriteLine("--- Handler Metrics ---\n");
        var handlerMetrics = metricsCollector.GetHandlerMetrics("FastProcessorHandler");

        if (handlerMetrics is not null)
        {
            Console.WriteLine("FastProcessorHandler:");
            Console.WriteLine($"  Execution Count: {handlerMetrics.ExecutionCount}");
            Console.WriteLine($"  Success Count: {handlerMetrics.SuccessCount}");
            Console.WriteLine($"  Failure Count: {handlerMetrics.FailureCount}");
            Console.WriteLine($"  Average Duration: {handlerMetrics.AverageDuration:F2}ms");
            Console.WriteLine($"  Min Duration: {handlerMetrics.MinDuration:F2}ms");
            Console.WriteLine($"  Max Duration: {handlerMetrics.MaxDuration:F2}ms");
        }

        Console.WriteLine();

        // Performance profiler report
        Console.WriteLine("--- Performance Profile Report ---\n");
        var profileReport = performanceProfiler.GenerateReport();
        Console.WriteLine(profileReport);

        // Memory usage statistics
        Console.WriteLine("--- Memory Statistics ---\n");
        var memoryBefore = GC.GetTotalMemory(false);
        GC.Collect();
        var memoryAfter = GC.GetTotalMemory(false);

        Console.WriteLine($"Memory Used: {(memoryAfter / 1024.0):F2} KB");
        Console.WriteLine($"Total Execution Time: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Events/Second: {(10.0 / stopwatch.Elapsed.TotalSeconds):F2}\n");

        // Health check
        Console.WriteLine("--- Health Check ---\n");
        var healthCheck = serviceProvider.GetRequiredService<IHealthCheck>();
        var healthStatus = await healthCheck.CheckHealthAsync();

        Console.WriteLine($"Overall Status: {healthStatus.Status}");
        Console.WriteLine("Component Checks:");
        foreach (var check in healthStatus.Checks)
        {
            var symbol = check.Status == "Healthy" ? "✓" : "✗";
            Console.WriteLine($"  {symbol} {check.Name}: {check.Status}");
        }

        Console.WriteLine("\n=== Example completed successfully ===");
    }
}
