# Monitoring

This guide covers two monitoring utilities in DotnetEventBus:

- **`HealthCheck`** (namespace `DotnetEventBus.Monitoring`) aggregates health probes into a single status.
- **`PerformanceProfiler`** (namespace `DotnetEventBus.Performance`) measures how long operations take and reports statistics.

Both classes are plain in-process objects. You create them yourself; they are not registered with DI by default.

```csharp
using DotnetEventBus.Monitoring;
using DotnetEventBus.Performance;
```

---

## HealthCheck

`HealthCheck` runs a set of named `IHealthCheckProbe` instances and combines their results into one `HealthCheckResult`.

### Types

| Type | Kind | Purpose |
|------|------|---------|
| `HealthCheck` | `sealed class` | Holds probes, runs them, remembers the last result. |
| `IHealthCheckProbe` | `interface` | One check. Exposes `Task<ProbeResult> CheckAsync()`. |
| `ProbeResult` | `sealed class` | Result of one probe: `Status`, `Message` (`string?`), `Details` (`Dictionary<string, object>`). |
| `HealthCheckResult` | `sealed class` | Result of a full check: `OverallStatus`, `CheckedAt` (UTC), `Checks` (`Dictionary<string, ProbeResult>`). |
| `HealthStatus` | `enum` | `Healthy`, `Degraded`, `Unhealthy`, `Unknown`. |
| `BuiltInProbes` | `static class` | Factory methods for the two built-in probes. |

### HealthCheck members

#### `public void RegisterProbe(string name, IHealthCheckProbe probe)`

Adds a probe under `name`.

- **Parameters**
  - `name`: Key for the probe. It appears as the key in `HealthCheckResult.Checks`.
  - `probe`: The probe to run on each check.
- **Exceptions**
  - `ArgumentNullException`: if `name` or `probe` is `null`.
- **Remarks**: Registering the same `name` again replaces the earlier probe. No exception is thrown.

#### `public async Task<HealthCheckResult> CheckHealthAsync()`

Runs every registered probe once, in turn, and returns the combined result.

- **Returns**: A `HealthCheckResult` with `OverallStatus`, `CheckedAt` and `Checks` filled in.
- **Behavior**
  - `OverallStatus` is `Healthy` only when every probe returned `Healthy`. Any other status, including `Degraded` and `Unknown`, makes the overall status `Unhealthy`. There is no overall `Degraded` value.
  - With no probes registered, `OverallStatus` is `Healthy`.
  - If a probe throws, the exception is caught. That probe's entry in `Checks` is `Unhealthy` with a message of the form `Probe failed: <exception message>`. The other probes still run.
  - `CheckedAt` is set to `DateTime.UtcNow`.
  - The result is also stored, so `GetLastStatus()` and `GetLastCheckTime()` reflect it.
- **Remarks**: Probes run sequentially, not in parallel. Probes do not receive a `CancellationToken`, so this method cannot be cancelled.

#### `public HealthStatus GetLastStatus()`

Returns `OverallStatus` from the most recent `CheckHealthAsync` call. Returns `HealthStatus.Unknown` before the first check.

#### `public DateTime GetLastCheckTime()`

Returns the UTC time of the most recent check. Returns `DateTime.MinValue` before the first check.

#### `public override string ToString()`

Returns a string such as `HealthCheck { OverallStatus = Healthy, CheckedAt = ..., RegisteredProbes = 2 }`. It is intended for logging.

### IHealthCheckProbe

```csharp
public interface IHealthCheckProbe
{
    Task<ProbeResult> CheckAsync();
}
```

Implement this interface to add your own check, for example a database ping:

```csharp
public sealed class DatabaseProbe : IHealthCheckProbe
{
    public async Task<ProbeResult> CheckAsync()
    {
        bool ok = await PingDatabaseAsync();

        return new ProbeResult
        {
            Status = ok ? HealthStatus.Healthy : HealthStatus.Unhealthy,
            Message = ok ? "Database reachable" : "Database unreachable",
            Details = new Dictionary<string, object> { { "checkedVia", "ping" } }
        };
    }
}
```

`ProbeResult.Details` defaults to an empty dictionary. `ProbeResult.Message` defaults to `null`.

### BuiltInProbes

#### `public static IHealthCheckProbe CreateMemoryProbe(long warningThresholdBytes = 1_073_741_824)`

Creates a probe that reads `GC.GetTotalMemory(false)` on each check.

- **Parameters**
  - `warningThresholdBytes`: Threshold in bytes. The default is 1 GiB.
- **Result**
  - `Healthy` when managed heap size is at or below the threshold.
  - `Degraded` when it is above the threshold.
  - `Message`: `Memory usage: <N>MB`.
  - `Details`: `memoryBytes` (the measured value) and `warningThresholdBytes`.
- **Remarks**: This measures managed heap only, not total process memory. The probe never returns `Unhealthy`.

#### `public static IHealthCheckProbe CreateResponsivenessProbe()`

Creates a probe that measures how long `Task.Delay(10)` takes.

- **Result**
  - `Healthy` when the elapsed time is 100 ms or less.
  - `Degraded` when it is more than 100 ms.
  - `Message`: `Responsiveness: <N>ms`.
  - `Details`: `latencyMs`.
- **Remarks**: The 100 ms limit is fixed and cannot be changed. The probe never returns `Unhealthy`. It measures scheduler and timer latency only. It does not check any event bus component.

### HealthCheck example

```csharp
var health = new HealthCheck();

health.RegisterProbe("memory", BuiltInProbes.CreateMemoryProbe(512L * 1024 * 1024));
health.RegisterProbe("responsiveness", BuiltInProbes.CreateResponsivenessProbe());
health.RegisterProbe("database", new DatabaseProbe());

HealthCheckResult result = await health.CheckHealthAsync();

Console.WriteLine($"Overall: {result.OverallStatus} at {result.CheckedAt:O}");

foreach (var (name, probe) in result.Checks)
{
    Console.WriteLine($"  {name}: {probe.Status} {probe.Message}");
}

Console.WriteLine(health.GetLastStatus()); // same as result.OverallStatus
```

### HealthCheck notes

- **Thread safety**: `HealthCheck` is not thread-safe. Probes are stored in a `Dictionary`, and the last-result fields are plain fields. Serialize calls to `RegisterProbe` and `CheckHealthAsync` yourself if several threads share an instance.
- **Built-in probes never report `Unhealthy`**: Their worst result is `Degraded`. Because any non-`Healthy` probe makes the overall status `Unhealthy`, one built-in probe over its threshold will make the overall status `Unhealthy`.
- **Probe order**: Probes run in the dictionary's enumeration order. Do not rely on a specific order.

---

## PerformanceProfiler

`PerformanceProfiler` times named operations. For each name it keeps every elapsed time, then computes statistics on request.

### Types

| Type | Kind | Purpose |
|------|------|---------|
| `PerformanceProfiler` | `sealed class` | Times operations and produces statistics. |
| `OperationStats` | `sealed class` | Statistics for one operation name. |
| `ProfilingSessionSummary` | `sealed class` | Totals across all operation names in the session. |

### PerformanceProfiler members

#### `public T Profile<T>(string operationName, Func<T> operation)`

Runs `operation`, records its elapsed time under `operationName`, and returns its result.

- **Exceptions**: `ArgumentNullException` if `operationName` or `operation` is `null`.
- **Remarks**: Timing is recorded in a `finally` block, so failed operations are timed too. The exception is rethrown unchanged.

#### `public async Task<T> ProfileAsync<T>(string operationName, Func<Task<T>> operation)`

Awaits `operation`, records the time from start until the task completes, and returns the result.

- **Exceptions**: `ArgumentNullException` if `operationName` or `operation` is `null`.
- **Remarks**: The time includes any awaiting inside `operation`. As with `Profile<T>`, failures are timed and the exception is rethrown.

#### `public void Profile(string operationName, Action operation)`

Same as `Profile<T>`, for an operation that returns nothing.

- **Exceptions**: `ArgumentNullException` if `operationName` or `operation` is `null`.

#### `public OperationStats? GetStats(string operationName)`

Returns statistics for one operation.

- **Returns**: `null` if the name was never recorded.
- **Remarks**: Each call sorts the recorded timings again, so the cost grows with the number of executions.

#### `public IEnumerable<OperationStats> GetAllStats()`

Returns statistics for every recorded operation name.

- **Remarks**: The result is lazy. It reads the profiler's internal collection while you enumerate it. Call `.ToList()` before recording more operations, or the enumeration can fail.

#### `public ProfilingSessionSummary GetSummary()`

Returns totals for the session. The session starts when the profiler is created, or when `Reset()` is called.

#### `public void Reset()`

Clears all recorded timings and restarts the session clock.

#### `public string GenerateReport()`

Returns a plain-text report. It starts with session totals, then lists each operation ordered by `TotalTimeMs`, highest first. Use the `OperationStats` and `ProfilingSessionSummary` properties when you need values to process; the report format is for people to read.

### OperationStats

| Property | Type | Meaning |
|----------|------|---------|
| `OperationName` | `string?` | The name passed to `Profile`, `ProfileAsync`, or `GetStats`. |
| `ExecutionCount` | `int` | Number of recorded executions. |
| `TotalTimeMs` | `long` | Sum of all elapsed times, in milliseconds. |
| `AverageTimeMs` | `double` | `TotalTimeMs / ExecutionCount`. |
| `MinTimeMs` | `long` | Smallest elapsed time. |
| `MaxTimeMs` | `long` | Largest elapsed time. |
| `MedianTimeMs` | `double` | Median. For an even count, the mean of the two middle values. |
| `P95TimeMs` | `double` | 95th percentile, nearest-rank method. |
| `P99TimeMs` | `double` | 99th percentile, nearest-rank method. |

### ProfilingSessionSummary

| Property | Type | Meaning |
|----------|------|---------|
| `SessionDuration` | `TimeSpan` | Time since the profiler was created or last reset. |
| `OperationCount` | `int` | Number of distinct operation names. |
| `TotalExecutions` | `int` | Number of recorded executions across all names. |
| `TotalTimeMs` | `long` | Sum of all elapsed times, in milliseconds. |
| `AverageTimeMs` | `double` | Mean elapsed time across all executions. |
| `ThroughputPerSecond` | `double` | `TotalExecutions / SessionDuration.TotalSeconds`. Includes idle time. |

### PerformanceProfiler example

```csharp
var profiler = new PerformanceProfiler();

for (int i = 0; i < 100; i++)
{
    await profiler.ProfileAsync("publish", async () =>
    {
        await bus.PublishAsync(new OrderCreatedEvent(i));
        return true;
    });
}

OperationStats? publish = profiler.GetStats("publish");
if (publish is not null)
{
    Console.WriteLine($"publish: avg {publish.AverageTimeMs:F2} ms, p95 {publish.P95TimeMs} ms");
}

Console.WriteLine(profiler.GenerateReport());
```

### PerformanceProfiler notes

- **Resolution**: Elapsed times are stored as whole milliseconds (`Stopwatch.ElapsedMilliseconds`). Operations shorter than 1 ms are recorded as `0`. For fine-grained measurements, profile a loop of many calls.
- **Wall-clock time**: The profiler measures elapsed time, not CPU time. Other load on the machine affects the results.
- **Thread safety**: `PerformanceProfiler` is not thread-safe. Its timings are stored in an unsynchronized `Dictionary<string, List<long>>`. Use one profiler per thread, or wrap calls in a lock.
- **Memory**: Every execution is stored in memory until `Reset()` is called. Long-running processes that profile hot paths should reset periodically.
- **Percentiles**: With only a few executions, P95 and P99 mostly equal the maximum.

---

## See also

- [HealthCheck.md](HealthCheck.md) and [PerformanceProfiler.md](PerformanceProfiler.md) are the per-class reference pages. Where they differ from the source, this guide follows the source.
