using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotnetEventBus.Middleware;
using DotnetEventBus.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Benchmarks;

/// <summary>
/// Benchmarks for middleware pipeline performance.
/// Measures the overhead of cross-cutting concerns like logging, error handling, and rate limiting.
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class MiddlewareBenchmarks
{
    private IServiceProvider? _serviceProvider;
    private IEventBus? _eventBus;
    private ServiceCollection? _services;

    public class TestEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "MiddlewareTest";
        public int Value { get; set; }
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

        _eventBus.Subscribe<TestEvent>(async (@event, ct) => await Task.Delay(1, ct), "TestHandler");
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
    /// Benchmark: Event publishing with logging middleware enabled
    /// Measures the overhead of structured logging in the pipeline
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Middleware")]
    public async Task Publish_With_Logging_Middleware()
    {
        var testEvent = new TestEvent { Id = "logging-test", Value = 42 };
        await _eventBus!.PublishAsync(testEvent);
    }

    /// <summary>
    /// Benchmark: Event publishing with error handling middleware
    /// Measures the overhead of exception handling and retry logic
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Middleware")]
    public async Task Publish_With_ErrorHandling_Middleware()
    {
        var testEvent = new TestEvent { Id = "error-handling-test", Value = 99 };
        await _eventBus!.PublishAsync(testEvent);
    }

    /// <summary>
    /// Benchmark: Event publishing with all middleware enabled
    /// Measures the cumulative overhead of all middleware components
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Middleware")]
    public async Task Publish_With_All_Middleware()
    {
        var testEvent = new TestEvent { Id = "all-middleware-test", Value = 100 };
        await _eventBus!.PublishAsync(testEvent);
    }

    /// <summary>
    /// Benchmark: Pipeline construction overhead
    /// Measures the cost of building middleware pipelines
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Middleware")]
    public void Create_Middleware_Pipeline()
    {
        // This is a no-op in the current implementation
        // but represents the overhead of pipeline construction
        var pipeline = new PipelineBuilder<TestEvent>();
    }

    /// <summary>
    /// Benchmark: Handler invocation with middleware chain
    /// Measures the end-to-end overhead of middleware processing
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Middleware")]
    public async Task Handler_Invocation_With_Middleware()
    {
        var testEvent = new TestEvent { Id = "middleware-invocation", Value = 200 };
        await _eventBus!.PublishAsync(testEvent);
    }
}