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
/// Batch Publishing Optimization: Demonstrates efficient publishing of multiple events
/// using batch operations for better throughput and resource utilization.
/// </summary>
public static class BatchPublishingOptimizationExample
{
    public sealed class LogEntryEvent
    {
        public string LogId { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public sealed class AnalyticsEvent
    {
        public string EventType { get; set; }
        public string UserId { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }

    public sealed class LogAggregatorHandler : EventHandlerBase<LogEntryEvent>
    {
        private static int _processedCount = 0;

        public override async Task Handle(LogEntryEvent @event, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _processedCount);
            await Task.Delay(5); // Simulate processing
        }

        public static int GetProcessedCount() => _processedCount;
    }

    public sealed class AnalyticsProcessorHandler : EventHandlerBase<AnalyticsEvent>
    {
        private static int _processedCount = 0;

        public override async Task Handle(AnalyticsEvent @event, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _processedCount);
            await Task.Delay(10); // Simulate analytics processing
        }

        public static int GetProcessedCount() => _processedCount;
    }

    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== DotnetEventBus: Batch Publishing Optimization ===\n");

        var services = new ServiceCollection();
        services.AddEventBus(options =>
        {
            options.AllowParallelHandling = true;
            options.MaxConcurrentHandlers = Environment.ProcessorCount;
        });

        var serviceProvider = services.BuildServiceProvider();
        var eventBus = serviceProvider.GetRequiredService<IEventBus>();
        var batchPublisher = serviceProvider.GetRequiredService<IBatchEventPublisher>();

        // Method 1: Individual Publishing (Slow)
        Console.WriteLine("--- Method 1: Individual Event Publishing ---\n");
        Console.WriteLine("Publishing 1000 log entries individually...\n");

        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < 1000; i++)
        {
            var logEvent = new LogEntryEvent
            {
                LogId = $"LOG-{i:D5}",
                Level = i % 5 switch { 0 => "Error", 1 => "Warning", 2 => "Info", 3 => "Debug", _ => "Trace" },
                Message = $"Application event {i}",
                Timestamp = DateTime.UtcNow.AddSeconds(i)
            };

            await eventBus.PublishAsync(logEvent);
        }

        stopwatch.Stop();
        var individualDuration = stopwatch.ElapsedMilliseconds;

        Console.WriteLine($"✓ Publishing completed in {individualDuration}ms");
        Console.WriteLine($"  Throughput: {(1000.0 / stopwatch.Elapsed.TotalSeconds):F2} events/sec\n");

        // Method 2: Batch Publishing (Fast)
        Console.WriteLine("--- Method 2: Batch Event Publishing ---\n");
        Console.WriteLine("Publishing 1000 analytics events in batch...\n");

        stopwatch.Restart();

        // Accumulate events
        for (int i = 0; i < 1000; i++)
        {
            var analyticsEvent = new AnalyticsEvent
            {
                EventType = i % 3 switch { 0 => "PageView", 1 => "Click", _ => "Purchase" },
                UserId = $"USER-{i % 100:D3}",
                Properties = new Dictionary<string, object>
                {
                    { "page", $"/page{i % 10}" },
                    { "timestamp", DateTime.UtcNow },
                    { "duration", Random.Shared.Next(100, 5000) }
                }
            };

            await batchPublisher.AddEventAsync(analyticsEvent);

            // Flush every 100 events for demonstration
            if ((i + 1) % 100 == 0)
            {
                Console.WriteLine($"  Flushing batch at {i + 1} events...");
                await batchPublisher.FlushAsync();
            }
        }

        // Final flush
        var remainingSize = batchPublisher.GetBatchSize();
        if (remainingSize > 0)
        {
            Console.WriteLine($"  Flushing remaining {remainingSize} events...");
            await batchPublisher.FlushAsync();
        }

        stopwatch.Stop();
        var batchDuration = stopwatch.ElapsedMilliseconds;

        Console.WriteLine($"\n✓ Batch publishing completed in {batchDuration}ms");
        Console.WriteLine($"  Throughput: {(1000.0 / stopwatch.Elapsed.TotalSeconds):F2} events/sec\n");

        // Performance comparison
        Console.WriteLine("--- Performance Comparison ---\n");
        var improvement = ((double)(individualDuration - batchDuration) / individualDuration) * 100;

        Console.WriteLine($"Individual Publishing: {individualDuration}ms");
        Console.WriteLine($"Batch Publishing:      {batchDuration}ms");
        Console.WriteLine($"Improvement:           {improvement:F1}% faster");
        Console.WriteLine($"Speed Ratio:           {(double)individualDuration / batchDuration:F2}x\n");

        // Demonstrate batching strategies
        Console.WriteLine("--- Batching Strategies ---\n");

        var strategies = new (string Name, int BatchSize, int EventCount)[]
        {
            ("Small batches (10)", 10, 100),
            ("Medium batches (50)", 50, 100),
            ("Large batches (500)", 500, 100)
        };

        foreach (var (name, batchSize, eventCount) in strategies)
        {
            Console.WriteLine($"Testing: {name}");
            stopwatch.Restart();

            for (int i = 0; i < eventCount; i++)
            {
                await batchPublisher.AddEventAsync(new LogEntryEvent
                {
                    LogId = $"LOG-BATCH-{i}",
                    Level = "Info",
                    Message = $"Batch test event",
                    Timestamp = DateTime.UtcNow
                });

                if ((i + 1) % batchSize == 0)
                    await batchPublisher.FlushAsync();
            }

            var remaining = batchPublisher.GetBatchSize();
            if (remaining > 0)
                await batchPublisher.FlushAsync();

            stopwatch.Stop();
            var throughput = (eventCount * 1000.0) / stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"  Duration: {stopwatch.ElapsedMilliseconds}ms, Throughput: {throughput:F0} events/sec\n");
        }

        // Memory efficiency demonstration
        Console.WriteLine("--- Memory Efficiency ---\n");

        var beforeMemory = GC.GetTotalMemory(true);

        // Batch operation
        for (int i = 0; i < 10000; i++)
        {
            await batchPublisher.AddEventAsync(new LogEntryEvent
            {
                LogId = $"LOG-MEM-{i}",
                Level = "Info",
                Message = "Memory test",
                Timestamp = DateTime.UtcNow
            });

            if ((i + 1) % 1000 == 0)
                await batchPublisher.FlushAsync();
        }

        var afterMemory = GC.GetTotalMemory(true);
        var memoryUsed = (afterMemory - beforeMemory) / 1024.0;

        Console.WriteLine($"Memory used for 10000 events: {memoryUsed:F2} KB");
        Console.WriteLine($"Per-event memory: {(memoryUsed / 10000):F4} KB\n");

        Console.WriteLine("=== Example completed successfully ===");
    }
}
