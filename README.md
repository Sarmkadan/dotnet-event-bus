// ... (rest of the README.md content remains the same)

## MiddlewareBenchmarks

The `MiddlewareBenchmarks` class measures performance characteristics of middleware components in the event bus pipeline, including logging, error handling, and pipeline construction overhead. It evaluates both individual middleware components and their cumulative impact on event processing.

Example usage:
```csharp
var benchmarks = new MiddlewareBenchmarks();
benchmarks.GlobalSetup();

// Benchmark: Event publishing with logging middleware
await benchmarks.Publish_With_Logging_Middleware();

// Benchmark: Event publishing with error handling middleware
await benchmarks.Publish_With_ErrorHandling_Middleware();

// Benchmark: Event publishing with all middleware enabled
await benchmarks.Publish_With_All_Middleware();

// Benchmark: Pipeline construction overhead
benchmarks.Create_Middleware_Pipeline();

// Benchmark: Handler invocation with middleware chain
await benchmarks.Handler_Invocation_With_Middleware();

benchmarks.GlobalCleanup();
```

``` 
