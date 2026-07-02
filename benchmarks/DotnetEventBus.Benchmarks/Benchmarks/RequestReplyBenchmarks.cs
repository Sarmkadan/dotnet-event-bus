using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotnetEventBus.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Benchmarks;

/// <summary>
/// Benchmarks for request-reply pattern performance.
/// Measures round-trip latency and throughput for synchronous request-response scenarios.
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class RequestReplyBenchmarks
{
    private IServiceProvider? _serviceProvider;
    private IEventBus? _eventBus;
    private ServiceCollection? _services;

    public class RequestEvent
    {
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
        public string Payload { get; set; } = "Test";
        public int Value { get; set; }
    }

    public class ResponseEvent
    {
        public string RequestId { get; set; }
        public string Result { get; set; } = "Success";
        public int ProcessedValue { get; set; }
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

        // Register request handler
        _eventBus.Subscribe<RequestEvent>(async (@event, ct) =>
        {
            await Task.Delay(1, ct); // Simulate processing
            return new ResponseEvent
            {
                RequestId = @event.RequestId,
                Result = "Processed",
                ProcessedValue = @event.Value * 2
            };
        }, "RequestReplyHandler");
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
    /// Benchmark: Single request-reply round-trip
    /// Measures baseline latency for the most common request-response operation
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Request-Reply")]
    public async Task Request_Reply_Single()
    {
        var request = new RequestEvent
        {
            RequestId = "req-1",
            Payload = "Test",
            Value = 42
        };

        await _eventBus!.RequestAsync<RequestEvent, ResponseEvent>(request);
    }

    /// <summary>
    /// Benchmark: Sequential request-reply (10 requests)
    /// Measures throughput for batch request scenarios
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Request-Reply")]
    public async Task Request_Reply_10_Sequential()
    {
        for (int i = 0; i < 10; i++)
        {
            var request = new RequestEvent
            {
                RequestId = $"req-{i}",
                Payload = $"Test{i}",
                Value = i
            };

            await _eventBus!.RequestAsync<RequestEvent, ResponseEvent>(request);
        }
    }

    /// <summary>
    /// Benchmark: Parallel request-reply (10 concurrent requests)
    /// Measures maximum throughput for concurrent request scenarios
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Request-Reply")]
    public async Task Request_Reply_10_Parallel()
    {
        var tasks = new List<Task<ResponseEvent>>();

        for (int i = 0; i < 10; i++)
        {
            var request = new RequestEvent
            {
                RequestId = $"req-parallel-{i}",
                Payload = $"Test{i}",
                Value = i
            };

            tasks.Add(_eventBus!.RequestAsync<RequestEvent, ResponseEvent>(request));
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Benchmark: Request-reply with timeout
    /// Measures overhead of timeout handling in request-response pattern
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Request-Reply")]
    public async Task Request_Reply_With_Timeout()
    {
        var request = new RequestEvent
        {
            RequestId = "req-timeout",
            Payload = "Test",
            Value = 100
        };

        await _eventBus!.RequestAsync<RequestEvent, ResponseEvent>(
            request,
            timeout: TimeSpan.FromMilliseconds(10)
        );
    }

    /// <summary>
    /// Benchmark: Request-reply with large payload
    /// Measures performance impact of larger event sizes
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Request-Reply")]
    public async Task Request_Reply_Large_Payload()
    {
        var largePayload = new string('x', 1024 * 10); // ~10KB payload
        var request = new RequestEvent
        {
            RequestId = "req-large",
            Payload = largePayload,
            Value = 999
        };

        await _eventBus!.RequestAsync<RequestEvent, ResponseEvent>(request);
    }
}