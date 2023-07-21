// ... (rest of the README.md content remains the same)

## EventBusBenchmarks

The `EventBusBenchmarks` class provides performance benchmarks for the DotnetEventBus event bus, measuring throughput, latency, and memory allocations for critical operations. It evaluates the performance of various event publishing scenarios, including single event publishing, batch publishing, parallel handler execution, and error handling.

Here's an example usage:

```csharp
var benchmarks = new EventBusBenchmarks();
benchmarks.GlobalSetup();

// Benchmark: Single event publish with one handler
await benchmarks.Publish_Single_Event();

// Benchmark: Publish 100 events sequentially
await benchmarks.Publish_100_Events();

// Benchmark: Publish 1000 events sequentially
await benchmarks.Publish_1000_Events();

// Benchmark: Parallel handler execution with 8 concurrent handlers
await benchmarks.Publish_With_Parallel_Handlers();

// Benchmark: Sequential vs Parallel comparison
await benchmarks.Publish_Sequential_vs_Parallel();

// Benchmark: Handler with exception handling
await benchmarks.Publish_With_Retry();

// Benchmark: Subscription management overhead
benchmarks.Subscribe_And_Unsubscribe();

// Benchmark: Memory allocation for event publishing
await benchmarks.Publish_Memory_Allocation_Test();

benchmarks.GlobalCleanup();
```

This example demonstrates how to run various benchmarks using the `EventBusBenchmarks` class, providing insights into the performance characteristics of the DotnetEventBus event bus.
```