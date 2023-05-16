# EventBusBenchmarks

`EventBusBenchmarks` is a specialized type designed for evaluating the performance and operational efficiency of the `dotnet-event-bus` library. It provides a structured environment for executing micro-benchmarks that measure event publication latency, throughput, memory allocation, and handler performance under various concurrency and retry policies. This type is intended for use with benchmarking frameworks such as BenchmarkDotNet to provide accurate metrics for regression testing and performance optimization of the event bus infrastructure.

## API

### Properties
- **`Id` (string)**: Gets or sets the unique identifier associated with the benchmark scenario or event instance.
- **`Name` (string)**: Gets or sets the descriptive name of the benchmark scenario or the handler currently being evaluated.
- **`Value` (int)**: Gets or sets an integer value used for simulating event payload data.
- **`Timestamp` (DateTime)**: Gets or sets the precise timestamp associated with the event generation or benchmark execution window.

### Methods
- **`Handle` (Task)**: Represents the execution of an event handling operation.
- **`GetHandlerName` (string)**: Returns the string representation of the current handler's name.
- **`GlobalSetup` (void)**: Initializes the necessary environment and state for the benchmark execution.
- **`GlobalCleanup` (void)**: Executes finalization routines to clean up resources after benchmark completion.
- **`Publish_Single_Event` (async Task)**: Benchmarks the performance of publishing a single event through the bus.
- **`Publish_100_Events` (async Task)**: Benchmarks the throughput when publishing 100 consecutive events.
- **`Publish_1000_Events` (async Task)**: Benchmarks the throughput when publishing 1000 consecutive events.
- **`Publish_With_Parallel_Handlers` (async Task)**: Benchmarks event publication performance when configured with multiple parallel handlers.
- **`Publish_Sequential_vs_Parallel` (async Task)**: Benchmarks and compares event publication performance between sequential and parallel handler execution strategies.
- **`Publish_With_Retry` (async Task)**: Benchmarks publication performance when handling exceptions through integrated retry policies.
- **`Subscribe_And_Unsubscribe` (void)**: Benchmarks the overhead of managing handler subscriptions dynamically.
- **`Publish_Memory_Allocation_Test` (async Task)**: Benchmarks the memory allocation profile during event publication to identify garbage collection overhead.

*Note: The members `Handle`, `GetHandlerName`, `GlobalSetup`, and `GlobalCleanup` appear multiple times in the API surface, indicating implementations for different interfaces or class hierarchies. Refer to the specific usage context to determine the active implementation.*

## Usage

### Example 1: Executing a baseline publish benchmark
```csharp
var benchmarks = new EventBusBenchmarks();
benchmarks.GlobalSetup();

try
{
    await benchmarks.Publish_Single_Event();
}
finally
{
    benchmarks.GlobalCleanup();
}
```

### Example 2: Analyzing parallel handler performance
```csharp
var benchmarks = new EventBusBenchmarks();
benchmarks.GlobalSetup();

// Execute benchmarks for parallel handler configurations
await benchmarks.Publish_With_Parallel_Handlers();
await benchmarks.Publish_Sequential_vs_Parallel();

benchmarks.GlobalCleanup();
```

## Notes

- **Benchmark Environment**: `GlobalSetup` and `GlobalCleanup` are critical for ensuring isolated benchmark runs. Failure to call these methods may result in state leakage between test iterations, leading to skewed metrics.
- **Thread Safety**: The benchmark methods assume an initialized event bus. Concurrent access to the event bus during benchmarks should be carefully managed to ensure measurements reflect expected production behavior rather than locking contention not native to the bus itself.
- **Async Execution**: Most publication benchmarks are `async` and return a `Task`. Ensure that the caller awaits these tasks to correctly measure total execution time including any asynchronous overhead.
- **Edge Cases**: When benchmarking with retry policies or parallel handlers, ensure the underlying test environment has sufficient resources to avoid bottlenecking, which would obscure the performance characteristics of the `dotnet-event-bus` library itself.
