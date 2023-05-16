# RequestReplyBenchmarks

The `RequestReplyBenchmarks` class provides a suite of performance benchmarks designed to evaluate the throughput and latency of request-reply interaction patterns within the `dotnet-event-bus` library. These benchmarks measure the efficiency of processing various request types, including single requests, sequential operations, parallel execution, timeouts, and handling of large data payloads, ensuring the reliability and scalability of the event bus infrastructure under load.

## API

*   **`RequestId` (string):** The unique identifier assigned to a request-reply transaction.
*   **`Payload` (string):** The request data content being transmitted.
*   **`Value` (int):** An integer value associated with the request for processing.
*   **`Result` (string):** The response data content returned by the handler.
*   **`ProcessedValue` (int):** The integer result value returned by the handler.
*   **`GlobalSetup()` (void):** Initializes the necessary infrastructure and event bus components prior to executing the benchmark suite.
*   **`GlobalCleanup()` (void):** Releases resources, clears event bus state, and performs garbage collection after the benchmarks have concluded.
*   **`Request_Reply_Single()` (async Task):** Measures the time taken to process a single request-reply cycle.
*   **`Request_Reply_10_Sequential()` (async Task):** Measures the time taken to process ten consecutive request-reply cycles sequentially.
*   **`Request_Reply_10_Parallel()` (async Task):** Measures the throughput of processing ten request-reply cycles concurrently.
*   **`Request_Reply_With_Timeout()` (async Task):** Measures the performance impact of handling request-reply cycles when a timeout threshold is applied.
*   **`Request_Reply_Large_Payload()` (async Task):** Measures the performance impact and overhead associated with transmitting large data payloads within a request-reply cycle.

## Usage

### Running via BenchmarkDotNet
The primary use of this class is integration with the BenchmarkDotNet harness to generate performance reports:

```csharp
using BenchmarkDotNet.Running;

// Execute the benchmarks in the project
var summary = BenchmarkRunner.Run<RequestReplyBenchmarks>();
```

### Manual Execution of Benchmark Logic
In testing scenarios where automated performance instrumentation is not required, benchmark methods can be invoked directly:

```csharp
var benchmarks = new RequestReplyBenchmarks();
benchmarks.GlobalSetup();

try
{
    // Execute a specific scenario
    await benchmarks.Request_Reply_Single();
}
finally
{
    benchmarks.GlobalCleanup();
}
```

## Notes

*   **Benchmark Lifecycle:** `GlobalSetup` and `GlobalCleanup` are essential for ensuring consistent environmental states between benchmark runs; failure to call these in custom implementations may lead to skewed performance metrics or resource leaks.
*   **Thread Safety:** The `Request_Reply_10_Parallel` benchmark assumes the underlying event bus and its registered handlers are thread-safe and capable of handling concurrent requests without race conditions or shared state corruption.
*   **Memory Considerations:** The `Request_Reply_Large_Payload` benchmark may exert significant pressure on the heap. Ensure the test environment has sufficient memory allocation to prevent OutOfMemory exceptions during high-frequency iterations.
*   **Async/Await:** All `Request_Reply_*` methods are asynchronous. When extending these benchmarks, ensure that handlers are properly awaited to accurately capture the full lifecycle duration of the request.
