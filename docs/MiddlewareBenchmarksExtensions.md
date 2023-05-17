# MiddlewareBenchmarksExtensions
The `MiddlewareBenchmarksExtensions` type provides a set of static methods for benchmarking and measuring the performance of middleware components in the `dotnet-event-bus` project. These methods enable developers to assess the overhead and load handling capabilities of their middleware implementations, ensuring optimal performance and scalability.

## API
* `WithCustomPayloadSize`: Configures the benchmark with a custom payload size. The purpose of this method is to allow for tailored benchmarking scenarios, where the payload size can significantly impact performance. It returns an instance of `MiddlewareBenchmarks`.
* `RunAllMiddlewareBenchmarks`: Executes all available middleware benchmarks and returns an array of `BenchmarkResult` objects, providing a comprehensive overview of the middleware's performance characteristics.
* `MeasureMiddlewareOverhead`: Calculates the overhead of the middleware, returning the result as a `double` value. This method is useful for understanding the performance impact of the middleware itself, without considering external factors.
* `RunMiddlewareLoadTest`: Asynchronously runs a load test on the middleware, returning the result as a `double` value wrapped in a `Task`. This method is designed to simulate real-world load conditions and measure the middleware's ability to handle them.
* `BenchmarkResult`: Represents the outcome of a benchmarking operation, encapsulating relevant performance metrics.

## Usage
The following examples demonstrate how to utilize the `MiddlewareBenchmarksExtensions` type:
```csharp
// Example 1: Running all middleware benchmarks
var benchmarkResults = MiddlewareBenchmarksExtensions.RunAllMiddlewareBenchmarks();
foreach (var result in benchmarkResults)
{
    Console.WriteLine($"Benchmark: {result.Name}, Result: {result.Value}");
}

// Example 2: Measuring middleware overhead with a custom payload size
var customPayloadSize = 1024; // bytes
var middlewareBenchmarks = MiddlewareBenchmarksExtensions.WithCustomPayloadSize(customPayloadSize);
var overhead = MiddlewareBenchmarksExtensions.MeasureMiddlewareOverhead();
Console.WriteLine($"Middleware overhead with {customPayloadSize} byte payload: {overhead}");
```

## Notes
When using the `MiddlewareBenchmarksExtensions` type, consider the following edge cases and thread-safety remarks:
* The `RunAllMiddlewareBenchmarks` and `RunMiddlewareLoadTest` methods may throw exceptions if the underlying benchmarking infrastructure encounters errors.
* The `MeasureMiddlewareOverhead` method assumes a stable system state and may produce inaccurate results if the system is under heavy load or experiencing significant resource contention.
* The `WithCustomPayloadSize` method returns a new instance of `MiddlewareBenchmarks`, allowing for concurrent benchmarking scenarios with different payload sizes.
* The `MiddlewareBenchmarksExtensions` type is designed to be thread-safe, enabling concurrent access to its static methods without compromising the integrity of the benchmarking results. However, the thread-safety of the underlying middleware implementations is not guaranteed and should be evaluated separately.
