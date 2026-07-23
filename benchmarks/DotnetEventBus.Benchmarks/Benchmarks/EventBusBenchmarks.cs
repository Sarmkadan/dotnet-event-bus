using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using DotnetEventBus.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Benchmarks;

/// <summary>
/// Performance benchmarks for the DotnetEventBus event bus.
/// Measures throughput, latency, and memory allocations for critical operations.
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class EventBusBenchmarks
{
    private IServiceProvider? _serviceProvider;
    private IEventBus? _eventBus;
    private ServiceCollection? _services;
    private List<TestEvent>? _testEvents;
    private List<TestEvent>? _largeBatchEvents;

    // Test event models
    public class TestEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "TestEvent";
        public int Value { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class TestHandler : IEventHandler<TestEvent>
    {
        public Task Handle(TestEvent @event, CancellationToken cancellationToken = default)
        {
            // Simulate some processing work
            return Task.Delay(1, cancellationToken);
        }

        public string GetHandlerName() => "TestHandler";
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
            options.MaxRetryAttempts = 0;
            options.DefaultHandlerTimeout = TimeSpan.FromSeconds(30);
            options.EnableDeadLetterQueue = false;
            options.EnableMetrics = false;
        });

        _serviceProvider = _services.BuildServiceProvider();
        _eventBus = _serviceProvider.GetRequiredService<IEventBus>();

        // Register test handler
        _eventBus.Subscribe<TestEvent>(async (@event, ct) => await new TestHandler().Handle(@event, ct), "TestHandler");

        // Prepare test data
        _testEvents = Enumerable.Range(0, 1000)
            .Select(i => new TestEvent { Id = $"event-{i}", Value = i, Name = $"Event{i}" })
            .ToList();

        _largeBatchEvents = Enumerable.Range(0, 10000)
            .Select(i => new TestEvent { Id = $"large-event-{i}", Value = i, Name = $"LargeEvent{i}" })
            .ToList();
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
    /// Benchmark: Single event publish with one handler
    /// Measures baseline throughput for the most common operation
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Core Operations")]
    public async Task Publish_Single_Event()
    {
        var testEvent = new TestEvent { Id = "single-event", Value = 42, Name = "SingleEvent" };
        await _eventBus!.PublishAsync(testEvent);
    }

    /// <summary>
    /// Benchmark: Publish 100 events sequentially
    /// Measures throughput for batch publishing scenarios
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Core Operations")]
    public async Task Publish_100_Events()
    {
        foreach (var testEvent in _testEvents!.Take(100))
        {
            await _eventBus!.PublishAsync(testEvent);
        }
    }

    /// <summary>
    /// Benchmark: Publish 1000 events sequentially
    /// Measures throughput at scale
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Core Operations")]
    public async Task Publish_1000_Events()
    {
        foreach (var testEvent in _testEvents!)
        {
            await _eventBus!.PublishAsync(testEvent);
        }
    }

    /// <summary>
    /// Benchmark: Parallel handler execution with 8 concurrent handlers
    /// Measures the performance benefit of parallel processing
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Parallel Processing")]
    public async Task Publish_With_Parallel_Handlers()
    {
        // Register multiple handlers for the same event type
        for (int i = 0; i < 8; i++)
        {
            int handlerId = i;
            _eventBus!.Subscribe<TestEvent>(async (@event, ct) =>
            {
                // Simulate different processing times
                await Task.Delay(1 + handlerId, ct);
                return;
            }, $"ParallelHandler{handlerId}");
        }

        var testEvent = new TestEvent { Id = "parallel-event", Value = 99, Name = "ParallelEvent" };
        await _eventBus!.PublishAsync(testEvent);
    }

    /// <summary>
    /// Benchmark: Sequential vs Parallel comparison
    /// Measures the performance difference between sequential and parallel execution
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Parallel Processing")]
    public async Task Publish_Sequential_vs_Parallel()
    {
        // Sequential mode
        var optionsSequential = new EventBusOptions
        {
            AllowParallelHandling = false,
            MaxConcurrentHandlers = 1,
            MaxRetryAttempts = 0,
            DefaultHandlerTimeout = TimeSpan.FromSeconds(30),
            EnableDeadLetterQueue = false
        };

        var servicesSeq = new ServiceCollection();
        servicesSeq.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        servicesSeq.AddEventBus(optionsSequential);
        var spSeq = servicesSeq.BuildServiceProvider();
        var busSeq = spSeq.GetRequiredService<IEventBus>();

        // Register 4 handlers sequentially
        for (int i = 0; i < 4; i++)
        {
            busSeq.Subscribe<TestEvent>(async (@event, ct) => await Task.Delay(1, ct), $"SeqHandler{i}");
        }

        var testEvent = new TestEvent { Id = "seq-event", Value = 1, Name = "SeqEvent" };
        await busSeq.PublishAsync(testEvent);

        // Cleanup
        if (spSeq is IDisposable dispSeq)
        {
            dispSeq.Dispose();
        }
    }

    /// <summary>
    /// Benchmark: Handler with exception handling
    /// Measures overhead of retry logic and error handling
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Error Handling")]
    public async Task Publish_With_Retry()
    {
        var failingHandler = new FailingHandler();
        _eventBus!.Subscribe<TestEvent>(failingHandler.Handle, "FailingHandler");

        var testEvent = new TestEvent { Id = "retry-event", Value = 100, Name = "RetryEvent" };
        await _eventBus.PublishAsync(testEvent);
    }

    private class FailingHandler : IEventHandler<TestEvent>
    {
        private int _attempt = 0;

        public async Task Handle(TestEvent @event, CancellationToken cancellationToken = default)
        {
            _attempt++;
            if (_attempt < 3)
            {
                throw new InvalidOperationException("Simulated failure");
            }
            await Task.Delay(1, cancellationToken);
        }

        public string GetHandlerName() => "FailingHandler";
    }

    /// <summary>
    /// Benchmark: Subscription management overhead
    /// Measures cost of subscribing/unsubscribing handlers
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Subscription Management")]
    public void Subscribe_And_Unsubscribe()
    {
        var handler = new TestHandler();
        using var subscription = _eventBus!.Subscribe<TestEvent>(handler.Handle, "TempHandler");
        _eventBus.UnsubscribeAsync("TempHandler").GetAwaiter().GetResult();
    }

    /// <summary>
    /// Benchmark: Memory allocation for event publishing
    /// Measures GC pressure and memory allocations
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Memory Allocations")]
    public async Task Publish_Memory_Allocation_Test()
    {
        var testEvent = new TestEvent { Id = "memory-test", Value = 200, Name = "MemoryTest" };
        await _eventBus!.PublishAsync(testEvent);
    }
}

/// <summary>
/// Benchmarks for BatchEventPublisher
/// </summary>
[MemoryDiagnoser]
public class BatchEventPublisherBenchmarks
{
    private IServiceProvider? _serviceProvider;
    private BatchEventPublisher? _batchPublisher;
    private List<EventEnvelope>? _batchEvents;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        var sp = services.BuildServiceProvider();

        _batchPublisher = new BatchEventPublisher(
            sp.GetRequiredService<ILogger<BatchEventPublisher>>(),
            batchSize: 100,
            flushInterval: TimeSpan.FromSeconds(30)
        );

        _batchEvents = Enumerable.Range(0, 1000)
            .Select(i => new EventEnvelope
            {
                EventId = Guid.NewGuid().ToString(),
                EventType = typeof(EventBusBenchmarks.TestEvent).FullName!,
                Payload = System.Text.Json.JsonSerializer.Serialize(new EventBusBenchmarks.TestEvent
                {
                    Id = $"batch-{i}",
                    Value = i,
                    Name = $"BatchEvent{i}"
                }),
                Timestamp = DateTime.UtcNow
            })
            .ToList();

        _serviceProvider = sp;
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
    /// Benchmark: Add single event to batch
    /// Measures the overhead of batching
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Batch Operations")]
    public async Task Batch_Add_Single_Event()
    {
        var envelope = new EventEnvelope
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = typeof(EventBusBenchmarks.TestEvent).FullName!,
            Payload = System.Text.Json.JsonSerializer.Serialize(new EventBusBenchmarks.TestEvent
            {
                Id = "single-batch-event",
                Value = 1,
                Name = "SingleBatchEvent"
            }),
            Timestamp = DateTime.UtcNow
        };
        await _batchPublisher!.AddEventAsync(envelope);
    }

    /// <summary>
    /// Benchmark: Add 100 events to batch (fills one batch)
    /// Measures batch accumulation performance
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Batch Operations")]
    public async Task Batch_Add_100_Events()
    {
        for (int i = 0; i < 100; i++)
        {
            var envelope = new EventEnvelope
            {
                EventId = Guid.NewGuid().ToString(),
                EventType = typeof(EventBusBenchmarks.TestEvent).FullName!,
                Payload = System.Text.Json.JsonSerializer.Serialize(new EventBusBenchmarks.TestEvent
                {
                    Id = $"batch-{i}",
                    Value = i,
                    Name = $"BatchEvent{i}"
                }),
                Timestamp = DateTime.UtcNow
            };
            await _batchPublisher!.AddEventAsync(envelope);
        }
    }

    /// <summary>
    /// Benchmark: Add 1000 events to batch
    /// Measures large batch performance
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Batch Operations")]
    public async Task Batch_Add_1000_Events()
    {
        foreach (var envelope in _batchEvents!)
        {
            await _batchPublisher!.AddEventAsync(envelope);
        }
    }

    /// <summary>
    /// Benchmark: Manual flush vs automatic flush
    /// Measures the cost of explicit flush operations
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Batch Operations")]
    public async Task Batch_Flush_Explicit()
    {
        for (int i = 0; i < 50; i++)
        {
            var envelope = new EventEnvelope
            {
                EventId = Guid.NewGuid().ToString(),
                EventType = typeof(EventBusBenchmarks.TestEvent).FullName!,
                Payload = System.Text.Json.JsonSerializer.Serialize(new EventBusBenchmarks.TestEvent
                {
                    Id = $"flush-{i}",
                    Value = i,
                    Name = $"FlushEvent{i}"
                }),
                Timestamp = DateTime.UtcNow
            };
            await _batchPublisher!.AddEventAsync(envelope);
        }
        await _batchPublisher!.FlushAsync();
    }
}

/// <summary>
/// Benchmarks for different configuration scenarios
/// </summary>
[MemoryDiagnoser]
public class ConfigurationBenchmarks
{
    [Benchmark]
    [BenchmarkCategory("Configuration")]
    public IEventBus Create_EventBus_Default()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddEventBus();
        return services.BuildServiceProvider().GetRequiredService<IEventBus>();
    }

    [Benchmark]
    [BenchmarkCategory("Configuration")]
    public IEventBus Create_EventBus_Parallel()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddEventBus(options =>
        {
            options.AllowParallelHandling = true;
            options.MaxConcurrentHandlers = Environment.ProcessorCount * 2;
        });
        return services.BuildServiceProvider().GetRequiredService<IEventBus>();
    }

    [Benchmark]
    [BenchmarkCategory("Configuration")]
    public IEventBus Create_EventBus_No_Parallel()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddEventBus(options =>
        {
            options.AllowParallelHandling = false;
            options.MaxConcurrentHandlers = 1;
        });
        return services.BuildServiceProvider().GetRequiredService<IEventBus>();
    }
}

/// <summary>
/// Benchmarks for predicate-based subscriptions to measure filtering performance.
/// Tests the optimization of compiling multiple predicates into a single function.
/// </summary>
[MemoryDiagnoser]
public class PredicateSubscriptionBenchmarks
{
    private IServiceProvider? _serviceProvider;
    private IEventBus? _eventBus;
    private ServiceCollection? _services;

    public class TestEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "TestEvent";
        public int Value { get; set; }
        public string Category { get; set; } = "default";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _services = new ServiceCollection();
        _services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        _services.AddEventBus(options =>
        {
            options.AllowParallelHandling = false;
            options.MaxConcurrentHandlers = 1;
            options.MaxRetryAttempts = 0;
            options.DefaultHandlerTimeout = TimeSpan.FromSeconds(30);
        });

        _serviceProvider = _services.BuildServiceProvider();
        _eventBus = _serviceProvider.GetRequiredService<IEventBus>();
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
    /// Benchmark: Single predicate subscription with simple condition
    /// Measures baseline performance for predicate filtering with one condition
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Predicate Filtering")]
    public async Task Publish_With_Single_Predicate()
    {
        _eventBus!.CreatePredicateSubscription<TestEvent>()
            .Where(e => e.Value > 50)
            .WithHandler(async (@event, ct) => await Task.CompletedTask)
            .Register();

        var testEvent = new TestEvent { Id = "predicate-test", Value = 100, Name = "PredicateTest" };
        await _eventBus.PublishAsync(testEvent);
    }

    /// <summary>
    /// Benchmark: Multiple predicate subscriptions (10) for the same event
    /// Measures the overhead of multiple predicate evaluations per publish
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Predicate Filtering")]
    public async Task Publish_With_10_Predicate_Subscribers()
    {
        // Register 10 predicate-based subscriptions
        for (int i = 0; i < 10; i++)
        {
            int threshold = i * 10; // Different threshold for each subscription
            _eventBus!.CreatePredicateSubscription<TestEvent>()
                .Where(e => e.Value > threshold)
                .WithHandler(async (@event, ct) => await Task.CompletedTask)
                .Register();
        }

        // Publish an event that matches all predicates
        var testEvent = new TestEvent { Id = "multi-predicate-test", Value = 150, Name = "MultiPredicateTest" };
        await _eventBus.PublishAsync(testEvent);
    }

    /// <summary>
    /// Benchmark: Predicate subscription with complex conditions
    /// Measures performance with multiple conditions combined
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Predicate Filtering")]
    public async Task Publish_With_Complex_Predicate()
    {
        _eventBus!.CreatePredicateSubscription<TestEvent>()
            .Where(e => e.Value > 50)
            .Where(e => e.Category == "important")
            .Where(e => e.Name.StartsWith("Test"))
            .WithHandler(async (@event, ct) => await Task.CompletedTask)
            .Register();

        var testEvent = new TestEvent { Id = "complex-test", Value = 100, Name = "TestEvent", Category = "important" };
        await _eventBus.PublishAsync(testEvent);
    }

    /// <summary>
    /// Benchmark: Predicate subscription with expression-based predicates
    /// Tests the optimization of compiling Expression trees to Func delegates
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Predicate Filtering")]
    public async Task Publish_With_Expression_Predicate()
    {
        _eventBus!.CreatePredicateSubscription<TestEvent>()
            .Where(e => e.Value > 50 && e.Category == "important")
            .WithHandler(async (@event, ct) => await Task.CompletedTask)
            .Register();

        var testEvent = new TestEvent { Id = "expression-test", Value = 100, Name = "TestEvent", Category = "important" };
        await _eventBus.PublishAsync(testEvent);
    }

    /// <summary>
    /// Benchmark: Predicate subscription with parallel handling enabled
    /// Measures predicate performance with concurrent handler execution
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Predicate Filtering with Parallel")]
    public async Task Publish_With_Predicate_And_Parallel_Handling()
    {
        _eventBus!.CreatePredicateSubscription<TestEvent>()
            .Where(e => e.Value > 50)
            .WithHandler(async (@event, ct) => await Task.Delay(1, ct))
            .Register();

        var testEvent = new TestEvent { Id = "parallel-predicate-test", Value = 100, Name = "ParallelPredicateTest" };
        await _eventBus.PublishAsync(testEvent);
    }

    /// <summary>
    /// Benchmark: Predicate subscription that filters out events
    /// Measures performance when predicates reject events (no handler invocation)
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Predicate Filtering")]
    public async Task Publish_With_Predicate_That_Filters_Out()
    {
        _eventBus!.CreatePredicateSubscription<TestEvent>()
            .Where(e => e.Value > 1000) // Event with Value=100 will be filtered out
            .WithHandler(async (@event, ct) => await Task.CompletedTask)
            .Register();

        var testEvent = new TestEvent { Id = "filtered-out", Value = 100, Name = "FilteredOut" };
        await _eventBus.PublishAsync(testEvent);
    }

    /// <summary>
    /// Benchmark: Multiple predicate subscriptions with mixed matching
    /// Tests performance when some predicates match and some don't
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Predicate Filtering")]
    public async Task Publish_With_10_Mixed_Predicate_Subscribers()
    {
        // Register 10 predicate-based subscriptions with different thresholds
        for (int i = 0; i < 10; i++)
        {
            int threshold = i * 20;
            _eventBus!.CreatePredicateSubscription<TestEvent>()
                .Where(e => e.Value > threshold)
                .WithHandler(async (@event, ct) => await Task.CompletedTask)
                .Register();
        }

        // Publish an event that only matches some predicates
        var testEvent = new TestEvent { Id = "mixed-match-test", Value = 125, Name = "MixedMatchTest" };
        await _eventBus.PublishAsync(testEvent);
    }
}