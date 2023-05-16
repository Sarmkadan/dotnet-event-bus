# EventBusBenchmarksExtensions

`EventBusBenchmarksExtensions` provides a set of fluent configuration methods for `EventBusBenchmarks` instances, enabling precise control over the benchmarking environment. These extensions allow for the assignment of identifiers, injection of test values, and the registration of setup and cleanup lifecycle hooks, facilitating streamlined and reproducible benchmark scenarios for event bus implementations.

## API

### WithIdentity
Assigns a unique identifier to the `EventBusBenchmarks` instance for reporting and logging purposes.

*   **Parameters:**
    *   `benchmarks` (`EventBusBenchmarks`): The benchmark instance to configure.
    *   `identity` (`string`): The string identifier.
*   **Returns:** The updated `EventBusBenchmarks` instance.
*   **Throws:** `ArgumentNullException` if `benchmarks` is null.

### WithValue
Injects a specific value to be processed during the benchmark execution.

*   **Parameters:**
    *   `benchmarks` (`EventBusBenchmarks`): The benchmark instance to configure.
    *   `value` (`object`): The value to inject.
*   **Returns:** The updated `EventBusBenchmarks` instance.
*   **Throws:** `ArgumentNullException` if `benchmarks` is null.

### WithSetup
Registers a delegate to be executed before the benchmark performance measurement begins.

*   **Parameters:**
    *   `benchmarks` (`EventBusBenchmarks`): The benchmark instance to configure.
    *   `action` (`Action`): The setup routine to execute.
*   **Returns:** The updated `EventBusBenchmarks` instance.
*   **Throws:** `ArgumentNullException` if `benchmarks` or `action` is null.

### WithCleanup
Registers a delegate to be executed after the benchmark performance measurement concludes.

*   **Parameters:**
    *   `benchmarks` (`EventBusBenchmarks`): The benchmark instance to configure.
    *   `action` (`Action`): The cleanup routine to execute.
*   **Returns:** The updated `EventBusBenchmarks` instance.
*   **Throws:** `ArgumentNullException` if `benchmarks` or `action` is null.

## Usage

### Fluent Configuration
```csharp
var benchmarks = new EventBusBenchmarks()
    .WithIdentity("HighThroughputTest")
    .WithValue(new MessagePayload { Id = 1, Data = "Test" });
```

### Setup and Cleanup Lifecycle
```csharp
var benchmarks = new EventBusBenchmarks()
    .WithSetup(() => InitializeEnvironment())
    .WithCleanup(() => DisposeEnvironment());
```

## Notes

*   **Ordering:** These methods are intended to be chained together. While the order of configuration calls does not typically affect the outcome of the benchmark, setup actions are executed sequentially in the order registered, and cleanup actions are executed in reverse order of registration.
*   **Thread Safety:** The extension methods themselves are thread-safe as they operate on the provided instance. However, the resulting `EventBusBenchmarks` object is generally not thread-safe; configuring and executing a benchmark instance should occur within a single thread unless explicitly supported by the underlying benchmark runner.
*   **Exception Handling:** Delegates provided to `WithSetup` and `WithCleanup` should be designed to handle their own exceptions. Unhandled exceptions within these delegates will interrupt the benchmark execution flow.
