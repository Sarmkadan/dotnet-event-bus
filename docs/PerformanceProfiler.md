# PerformanceProfiler

The `PerformanceProfiler` is a utility class designed to measure and analyze the execution performance of operations within .NET applications. It tracks timing metrics, execution counts, and statistical distributions for profiled operations, providing insights into performance bottlenecks and trends. The profiler supports both synchronous and asynchronous operations and can generate detailed reports for analysis.

## API

### `public T Profile<T>(Func<T> operation)`
Executes the provided synchronous operation while measuring its performance. Returns the result of the operation.

- **Parameters**:
  - `operation`: The synchronous operation to profile.
- **Return value**: The result of the operation.
- **Exceptions**: Throws `ArgumentNullException` if `operation` is `null`.

### `public async Task<T> ProfileAsync<T>(Func<Task<T>> operation)`
Executes the provided asynchronous operation while measuring its performance. Returns the result of the operation.

- **Parameters**:
  - `operation`: The asynchronous operation to profile.
- **Return value**: A `Task<T>` representing the asynchronous operation and its result.
- **Exceptions**: Throws `ArgumentNullException` if `operation` is `null`.

### `public void Profile(Action operation)`
Executes the provided synchronous action while measuring its performance. No return value is produced.

- **Parameters**:
  - `operation`: The synchronous action to profile.
- **Exceptions**: Throws `ArgumentNullException` if `operation` is `null`.

### `public OperationStats? GetStats(string operationName)`
Retrieves the performance statistics for a specific operation by name.

- **Parameters**:
  - `operationName`: The name of the operation to query.
- **Return value**: An `OperationStats` object containing performance metrics, or `null` if the operation has not been profiled.
- **Exceptions**: Throws `ArgumentNullException` if `operationName` is `null`.

### `public IEnumerable<OperationStats> GetAllStats()`
Retrieves performance statistics for all profiled operations.

- **Return value**: An enumerable collection of `OperationStats` objects, one for each profiled operation.

### `public ProfilingSessionSummary GetSummary()`
Generates a summary of the current profiling session, including aggregated metrics across all operations.

- **Return value**: A `ProfilingSessionSummary` object containing session-level statistics.

### `public void Reset()`
Clears all recorded profiling data, resetting the profiler to its initial state.

### `public string GenerateReport()`
Generates a human-readable report of all profiled operations and their performance statistics.

- **Return value**: A string containing the formatted report.

### `public string? OperationName` (Property)
Gets the name of the currently profiled operation.

- **Return value**: The name of the operation, or `null` if no operation is being profiled.

### `public int ExecutionCount` (Property)
Gets the total number of times the currently profiled operation has been executed.

- **Return value**: The execution count.

### `public long TotalTimeMs` (Property)
Gets the total execution time of the currently profiled operation in milliseconds.

- **Return value**: The total time in milliseconds.

### `public double AverageTimeMs` (Property)
Gets the average execution time of the currently profiled operation in milliseconds.

- **Return value**: The average time in milliseconds.

### `public long MinTimeMs` (Property)
Gets the minimum execution time of the currently profiled operation in milliseconds.

- **Return value**: The minimum time in milliseconds.

### `public long MaxTimeMs` (Property)
Gets the maximum execution time of the currently profiled operation in milliseconds.

- **Return value**: The maximum time in milliseconds.

### `public double MedianTimeMs` (Property)
Gets the median execution time of the currently profiled operation in milliseconds.

- **Return value**: The median time in milliseconds.

### `public double P95TimeMs` (Property)
Gets the 95th percentile execution time of the currently profiled operation in milliseconds.

- **Return value**: The 95th percentile time in milliseconds.

### `public double P99TimeMs` (Property)
Gets the 99th percentile execution time of the currently profiled operation in milliseconds.

- **Return value**: The 99th percentile time in milliseconds.

### `public TimeSpan SessionDuration` (Property)
Gets the total duration of the current profiling session.

- **Return value**: The session duration as a `TimeSpan`.

### `public int OperationCount` (Property)
Gets the number of unique operations that have been profiled.

- **Return value**: The count of unique operations.

### `public int TotalExecutions` (Property)
Gets the total number of executions across all profiled operations.

- **Return value**: The total execution count.

## Usage

### Example 1: Profiling a Synchronous Operation
```csharp
var profiler = new PerformanceProfiler();

// Profile a synchronous operation
var result = profiler.Profile(() =>
{
    // Simulate work
    Thread.Sleep(100);
    return "Operation completed";
});

Console.WriteLine($"Result: {result}");
Console.WriteLine(profiler.GenerateReport());
```

### Example 2: Profiling an Asynchronous Operation
```csharp
var profiler = new PerformanceProfiler();

// Profile an asynchronous operation
var result = await profiler.ProfileAsync(async () =>
{
    // Simulate async work
    await Task.Delay(150);
    return 42;
});

Console.WriteLine($"Result: {result}");
Console.WriteLine(profiler.GenerateReport());
```

## Notes

- The profiler is **thread-safe** and can be used concurrently from multiple threads without additional synchronization.
- Profiling data is stored in memory and will be lost when the `PerformanceProfiler` instance is garbage-collected or reset.
- The `Profile` and `ProfileAsync` methods measure wall-clock time, not CPU time. External factors like system load may affect results.
- Statistical properties (e.g., `MedianTimeMs`, `P95TimeMs`) are calculated only after multiple executions; single executions will return default values (e.g., `0` for `TotalTimeMs`).
- The `OperationName` property reflects the name of the most recently profiled operation. To retrieve stats for a specific operation, use `GetStats`.
- The `GenerateReport` method formats data for human readability and may change in future versions. For programmatic access to metrics, use the properties of `OperationStats` or `ProfilingSessionSummary`.
