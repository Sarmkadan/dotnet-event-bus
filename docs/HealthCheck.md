# HealthCheck

The `HealthCheck` class provides a centralized mechanism for monitoring the health of an application by aggregating results from multiple probes. It allows registration of custom probes, executes them on demand, and exposes the combined health status, a human-readable message, and detailed per-probe results. The class also offers static factory methods to create common probes for memory and responsiveness checks.

## API

### `public void RegisterProbe(IHealthCheckProbe probe)`

Registers a probe to be included in subsequent health checks.  
**Parameters:**  
- `probe` – An instance of `IHealthCheckProbe` to register.  
**Exceptions:**  
- `ArgumentNullException` – if `probe` is `null`.  
- `InvalidOperationException` – if the probe has already been registered.

### `public async Task<HealthCheckResult> CheckHealthAsync()`

Executes all registered probes and returns a combined health result. The method updates the `Status`, `Message`, `Details`, `OverallStatus`, `CheckedAt`, and `Checks` properties.  
**Returns:** A `HealthCheckResult` containing the overall status, message, and per-probe results.  
**Exceptions:**  
- `OperationCanceledException` – if the underlying cancellation token is triggered (if applicable).  
- `AggregateException` – if one or more probes throw exceptions during execution.

### `public HealthStatus GetLastStatus()`

Returns the overall health status from the most recent health check.  
**Returns:** A `HealthStatus` value (e.g., `Healthy`, `Degraded`, `Unhealthy`).  
**Remarks:** Returns `HealthStatus.Unhealthy` if no check has been performed yet.

### `public DateTime GetLastCheckTime()`

Returns the timestamp of the most recent health check.  
**Returns:** A `DateTime` in UTC.  
**Remarks:** Returns `DateTime.MinValue` if no check has been performed.

### `public HealthStatus Status`

Gets the overall health status from the last check.  
**Remarks:** This property is updated after each call to `CheckHealthAsync`. It is equivalent to `OverallStatus`.

### `public string? Message`

Gets a human-readable message describing the overall health state.  
**Remarks:** May be `null` if no check has been performed or if no message was provided.

### `public Dictionary<string, object> Details`

Gets a dictionary of additional details from the last health check.  
**Remarks:** The dictionary is cleared and repopulated after each check. Keys are probe names or custom detail keys.

### `public HealthStatus OverallStatus`

Gets the aggregated health status from the last check.  
**Remarks:** This is the same value as `Status`. It reflects the worst status among all probes.

### `public DateTime CheckedAt`

Gets the UTC timestamp of the last health check.  
**Remarks:** Updated after each successful call to `CheckHealthAsync`.

### `public Dictionary<string, ProbeResult> Checks`

Gets a dictionary mapping probe names to their individual `ProbeResult` from the last check.  
**Remarks:** The dictionary is replaced after each check. Keys are the probe names (as returned by `IHealthCheckProbe.Name`).

### `public static IHealthCheckProbe CreateMemoryProbe(long thresholdBytes)`

Creates a probe that monitors memory usage.  
**Parameters:**  
- `thresholdBytes` – The memory usage threshold in bytes. The probe returns `Unhealthy` if the current process memory exceeds this value.  
**Returns:** An `IHealthCheckProbe` instance.  
**Exceptions:**  
- `ArgumentOutOfRangeException` – if `thresholdBytes` is less than or equal to zero.

### `public static IHealthCheckProbe CreateResponsivenessProbe(TimeSpan timeout)`

Creates a probe that checks application responsiveness by performing a lightweight operation (e.g., a simple computation or a database ping).  
**Parameters:**  
- `timeout` – The maximum time allowed for the responsiveness check.  
**Returns:** An `IHealthCheckProbe` instance.  
**Exceptions:**  
- `ArgumentOutOfRangeException` – if `timeout` is less than or equal to `TimeSpan.Zero`.

### `public MemoryHealthProbe MemoryHealthProbe { get; }`

Gets a default memory health probe instance.  
**Remarks:** This property returns a pre-configured `MemoryHealthProbe` with a default threshold. The exact threshold is implementation-defined.

### `public async Task<ProbeResult> CheckAsync()`

Executes all registered probes and returns the overall result as a single `ProbeResult`.  
**Returns:** A `ProbeResult` representing the combined health state.  
**Exceptions:** Same as `CheckHealthAsync`.

### `public async Task<ProbeResult> CheckAsync(string probeName)`

Executes only the probe with the specified name and returns its result.  
**Parameters:**  
- `probeName` – The name of the probe to execute.  
**Returns:** A `ProbeResult` for that specific probe.  
**Exceptions:**  
- `ArgumentException` – if `probeName` is `null` or empty.  
- `KeyNotFoundException` – if no probe with the given name is registered.

## Usage

### Example 1: Basic health check with memory and responsiveness probes

```csharp
using dotnet_event_bus;

var healthCheck = new HealthCheck();

// Register built-in probes
healthCheck.RegisterProbe(HealthCheck.CreateMemoryProbe(512 * 1024 * 1024)); // 512 MB
healthCheck.RegisterProbe(HealthCheck.CreateResponsivenessProbe(TimeSpan.FromSeconds(5)));

// Perform a health check
HealthCheckResult result = await healthCheck.CheckHealthAsync();

Console.WriteLine($"Overall status: {result.Status}");
Console.WriteLine($"Message: {result.Message}");

foreach (var kvp in result.Checks)
{
    Console.WriteLine($"Probe '{kvp.Key}': {kvp.Value.Status}");
}
```

### Example 2: Using per-probe check and accessing last status

```csharp
using dotnet_event_bus;

var healthCheck = new HealthCheck();
var customProbe = new MyCustomProbe(); // implements IHealthCheckProbe
healthCheck.RegisterProbe(customProbe);

// Check only a specific probe
ProbeResult probeResult = await healthCheck.CheckAsync("MyCustomProbe");
Console.WriteLine($"Custom probe status: {probeResult.Status}");

// Later, retrieve the last overall status without re-running all probes
HealthStatus lastStatus = healthCheck.GetLastStatus();
DateTime lastCheckTime = healthCheck.GetLastCheckTime();
Console.WriteLine($"Last check at {lastCheckTime}: {lastStatus}");
```

## Notes

- **Thread safety:** The `HealthCheck` class is not inherently thread-safe. Concurrent calls to `RegisterProbe` and `CheckHealthAsync` (or the `CheckAsync` overloads) may lead to inconsistent state. External synchronization (e.g., a lock) is recommended when multiple threads access the same instance.
- **First check:** Before any call to `CheckHealthAsync`, the `Status` property returns `HealthStatus.Unhealthy`, `Message` is `null`, `CheckedAt` is `DateTime.MinValue`, and `Checks` is an empty dictionary.
- **Probe registration:** Probes are identified by their `Name` property. Registering a probe with a name that already exists will throw an `InvalidOperationException`.
- **Exception handling:** If a probe throws an exception during execution, the overall check will still complete, but the probe’s result will reflect the failure. The exception is captured and stored in the `ProbeResult.Exception` property. The overall status may be degraded or unhealthy depending on the probe’s criticality.
- **Memory probe threshold:** The `CreateMemoryProbe` factory uses process-level memory counters. On platforms where these are unavailable, the probe may return `Degraded` with an appropriate message.
- **Responsiveness probe timeout:** The `CreateResponsivenessProbe` performs a synchronous operation internally. If the operation exceeds the specified timeout, the probe returns `Unhealthy`. The timeout is enforced via a `CancellationTokenSource` and may throw `OperationCanceledException` if the operation is not cooperative.
