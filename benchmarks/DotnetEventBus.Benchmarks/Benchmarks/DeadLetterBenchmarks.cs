using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotnetEventBus.Models;
using DotnetEventBus.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Benchmarks;

/// <summary>
/// Benchmarks for dead letter queue operations.
/// Measures throughput and latency for error handling and retry scenarios.
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class DeadLetterBenchmarks
{
    private IServiceProvider? _serviceProvider;
    private IEventBus? _eventBus;
    private IDeadLetterService? _deadLetterService;
    private ServiceCollection? _services;

    public class FailingEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "FailingEvent";
        public int AttemptCount { get; set; }
    }

    public class FailingHandler : IEventHandler<FailingEvent>
    {
        private readonly bool _shouldFail;

        public FailingHandler(bool shouldFail = true)
        {
            _shouldFail = shouldFail;
        }

        public Task Handle(FailingEvent @event, CancellationToken cancellationToken = default)
        {
            if (_shouldFail && @event.AttemptCount < 3)
            {
                @event.AttemptCount++;
                throw new InvalidOperationException("Simulated failure for benchmark");
            }

            return Task.CompletedTask;
        }

        public string GetHandlerName() => "FailingHandler";
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _services = new ServiceCollection();
        _services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        _services.AddEventBus(options =>
        {
            options.AllowParallelHandling = true;
            options.MaxConcurrentHandlers = Environment.ProcessorCount;
            options.MaxRetryAttempts = 5;
            options.DefaultHandlerTimeout = TimeSpan.FromSeconds(30);
            options.EnableDeadLetterQueue = true;
            options.EnableMetrics = false;
        });

        _serviceProvider = _services.BuildServiceProvider();
        _eventBus = _serviceProvider.GetRequiredService<IEventBus>();
        _deadLetterService = _serviceProvider.GetRequiredService<IDeadLetterService>();

        // Register failing handler that will generate dead letter entries
        _eventBus.Subscribe<FailingEvent>(new FailingHandler().Handle, "FailingHandler");
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// Benchmark: Publish event that fails and gets sent to dead letter queue
    /// Measures the overhead of error handling and DLQ routing
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Dead Letter Queue")]
    public async Task Publish_To_DeadLetter()
    {
        var failingEvent = new FailingEvent
        {
            EventId = "dlq-test-1",
            Name = "TestFailingEvent"
        };

        try
        {
            await _eventBus!.PublishAsync(failingEvent);
        }
        catch
        {
            // Expected to fail
        }
    }

    /// <summary>
    /// Benchmark: Get pending dead letter entries
    /// Measures query performance for failed messages
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Dead Letter Queue")]
    public async Task Get_Pending_DeadLetter_Entries()
    {
        await _deadLetterService!.GetPendingEntriesAsync();
    }

    /// <summary>
    /// Benchmark: Reprocess 10 dead letter entries
    /// Measures the cost of reprocessing failed messages
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Dead Letter Queue")]
    public async Task Reprocess_10_DeadLetter_Entries()
    {
        // First, generate some dead letter entries
        for (int i = 0; i < 10; i++)
        {
            var failingEvent = new FailingEvent
            {
                EventId = $"dlq-gen-{i}",
                Name = $"TestFailingEvent{i}"
            };

            try
            {
                await _eventBus!.PublishAsync(failingEvent);
            }
            catch
            {
                // Expected
            }
        }

        // Now get and reprocess them
        var entries = await _deadLetterService!.GetPendingEntriesAsync();
        foreach (var entry in entries.Take(10))
        {
            await _deadLetterService.ReprocessEntryAsync(entry.Id);
        }
    }

    /// <summary>
    /// Benchmark: Get dead letter statistics
    /// Measures the overhead of statistics collection
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Dead Letter Queue")]
    public async Task Get_DeadLetter_Statistics()
    {
        await _deadLetterService!.GetStatisticsAsync();
    }

    /// <summary>
    /// Benchmark: Publish event with retry policy
    /// Measures the overhead of retry logic and exponential backoff
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Dead Letter Queue")]
    public async Task Publish_With_Retry_Policy()
    {
        var failingEvent = new FailingEvent
        {
            EventId = "retry-test",
            Name = "RetryTestEvent"
        };

        try
        {
            await _eventBus!.PublishAsync(failingEvent);
        }
        catch
        {
            // Expected to fail initially
        }
    }

    /// <summary>
    /// Benchmark: Memory allocation for dead letter operations
    /// Measures GC pressure for DLQ operations
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Dead Letter Queue")]
    public async Task DeadLetter_Memory_Allocation()
    {
        var failingEvent = new FailingEvent
        {
            EventId = "memory-dlq",
            Name = "MemoryDLQTest"
        };

        try
        {
            await _eventBus!.PublishAsync(failingEvent);
        }
        catch
        {
            // Expected
        }

        await _deadLetterService!.GetPendingEntriesAsync();
    }
}