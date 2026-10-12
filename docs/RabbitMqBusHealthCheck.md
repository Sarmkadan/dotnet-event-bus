# RabbitMqBusHealthCheck

`RabbitMqBusHealthCheck` is an ASP.NET Core `IHealthCheck` that reports the health of a RabbitMQ event bus from its connection state. It is registered with `AddEventBusHealthCheck()`.

> **Status:** no RabbitMQ bus is implemented yet, so no connection state is reported by default. Until an `IRabbitMqConnectionMonitor` is registered, the check reports `Unhealthy`. See the README section "Using RabbitMQ".

## Status mapping

| `RabbitMqConnectionState` | `HealthStatus` |
|---|---|
| `Open` | `Healthy` |
| `Reconnecting` | `Degraded` |
| `Closed`, or any other value | `Unhealthy` |

The result's `Data` dictionary contains `connectionState` with the state name.

## Constructor

### `public RabbitMqBusHealthCheck(IRabbitMqConnectionMonitor monitor)`

Parameters:
- `monitor` – The source of the current connection state. Must not be null.

Throws `ArgumentNullException` if `monitor` is null.

## Methods

### `public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)`

Reads `IRabbitMqConnectionMonitor.State` and maps it to a result as described above. It does not wait on the network.

## Extension

### `public static IHealthChecksBuilder AddEventBusHealthCheck(this IHealthChecksBuilder builder, string name = "eventbus", IEnumerable<string>? tags = null)`

Registers `RabbitMqBusHealthCheck` under `name`. If no `IRabbitMqConnectionMonitor` is registered when it is called, it registers a stand-in that reports `Closed`, so the check is `Unhealthy`. A monitor registered before or after this call is used instead.

```csharp
builder.Services.AddHealthChecks()
    .AddEventBusHealthCheck(tags: ["messaging"]);
```

## See also

- `IRabbitMqConnectionMonitor` – the interface a RabbitMQ bus implements to report its state.
- `RabbitMqConnectionState` – the `Open`, `Reconnecting` and `Closed` states.
