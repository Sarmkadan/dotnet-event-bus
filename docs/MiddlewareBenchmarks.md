# MiddlewareBenchmarks

`MiddlewareBenchmarks` provides a benchmarking suite designed to measure the performance impact of various middleware components within the `dotnet-event-bus` infrastructure. It enables developers to quantify latency and overhead introduced by logging, error handling, and complex pipeline configurations during message publishing and handler execution scenarios.

## API

### Id
`public string Id`
The unique identifier associated with this benchmark instance.

### Name
`public string Name`
A descriptive name for the benchmark scenario.

### Value
`public int Value`
A configurable integer value used to parameterize performance test scenarios.

### GlobalSetup
`public void GlobalSetup()`
Performs necessary initialization for the benchmark suite, such as configuring dependency injection containers, preparing service providers, or pre-allocating message objects. Throws an `InvalidOperationException` if the setup is called multiple times without a corresponding cleanup.

### GlobalCleanup
`public void GlobalCleanup()`
Cleans up resources, containers, or test data initialized during the `GlobalSetup` phase.

### Publish_With_Logging_Middleware
`public async Task Publish_With_Logging_Middleware()`
Benchmarks the `Publish` operation when only the logging middleware is registered in the pipeline.

### Publish_With_ErrorHandling_Middleware
`public async Task Publish_With_ErrorHandling_Middleware()`
Benchmarks the `Publish` operation when only the error handling middleware is registered in the pipeline.

### Publish_With_All_Middleware
`public async Task Publish_With_All_Middleware()`
Benchmarks the `Publish` operation when all available middleware components are registered in the pipeline.

### Create_Middleware_Pipeline
`public void Create_Middleware_Pipeline()`
Configures and constructs the specific middleware pipeline arrangement required for subsequent test executions.

### Handler_Invocation_With_Middleware
`public async Task Handler_Invocation_With_Middleware()`
Benchmarks the execution time of a message handler when it is invoked through a fully configured middleware pipeline.

## Usage

```csharp
// Example 1: Running the benchmarks using BenchmarkDotNet
[MemoryDiagnoser]
public class PerformanceTests
{
    private MiddlewareBenchmarks _benchmarks;

    [GlobalSetup]
    public void Setup()
    {
        _benchmarks = new MiddlewareBenchmarks();
        _benchmarks.GlobalSetup();
        _benchmarks.Create_Middleware_Pipeline();
    }

    [Benchmark]
    public async Task TestFullPipeline()
    {
        await _benchmarks.Publish_With_All_Middleware();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _benchmarks.GlobalCleanup();
    }
}
```

```csharp
// Example 2: Configuring pipeline parameters
var benchmarks = new MiddlewareBenchmarks();
benchmarks.GlobalSetup();
benchmarks.Value = 1000; // Configure the test parameter
benchmarks.Create_Middleware_Pipeline();

// Execute specific benchmark
await benchmarks.Handler_Invocation_With_Middleware();

benchmarks.GlobalCleanup();
```

## Notes

- **Thread Safety:** The `MiddlewareBenchmarks` class is not thread-safe. Concurrent execution of benchmark methods or setup routines is not supported and will result in invalid performance metrics. The benchmark framework is expected to handle the orchestration of these tests sequentially within a single thread per run.
- **Initialization:** It is mandatory to invoke `GlobalSetup` prior to executing any benchmark methods or `Create_Middleware_Pipeline`. Failure to initialize the state will result in a `NullReferenceException` or `InvalidOperationException`.
- **Pipeline Configuration:** `Create_Middleware_Pipeline` must be called to properly instantiate the middleware chain before invoking `Publish` or `Handler` benchmark methods. Re-invoking `Create_Middleware_Pipeline` mid-test is not supported and will lead to unpredictable behavior.
