# EventBusApiController

`EventBusApiController` serves as the primary HTTP-facing controller for publishing events and querying operational state in the `dotnet-event-bus` system. It exposes asynchronous endpoints for single and batch event publication, synchronous retrieval of runtime statistics, and health-check probing. The controller also surfaces a generic `ApiResponse<T>` envelope used across all its public operations, along with specialized result types that carry event- and batch-specific metadata.

## API

### Instance Methods

#### `PublishEventAsync`
```csharp
public virtual async Task<ApiResponse<EventPublishResult>> PublishEventAsync(/* event payload */)
```
Publishes a single event to the event bus. Accepts an event payload (the exact parameter signature is determined by the underlying implementation but typically includes event type, routing information, and a data body). Returns an `ApiResponse<EventPublishResult>` whose `Data` property contains the outcome details—`EventId`, `EventType`, `PublishedAt`, and a `Success` flag—when the operation completes. Throws no documented exceptions directly; transport or serialization failures are captured in the `ApiResponse` error envelope rather than propagated as unhandled exceptions.

#### `PublishBatchAsync`
```csharp
public virtual async Task<ApiResponse<BatchPublishResult>> PublishBatchAsync(/* collection of events */)
```
Publishes multiple events in a single request. Accepts a batch of event payloads. Returns an `ApiResponse<BatchPublishResult>` whose `Data` property carries the batch identifier (`BatchId`), the number of events processed (`EventCount`), the batch-level `PublishedAt` timestamp, and a `Success` flag indicating whether the entire batch was accepted. As with single-event publishing, failures are surfaced through the response envelope rather than via thrown exceptions.

#### `GetStats`
```csharp
public virtual ApiResponse<EventBusStats> GetStats()
```
Synchronously retrieves current event-bus statistics. Returns an `ApiResponse<EventBusStats>` whose `Data` property exposes `TotalEventsPublished` (a cumulative count of events published since the bus started) and a `Status` string describing the operational state. This is a non-blocking, synchronous call that reads from in-memory counters; it does not throw.

#### `GetHealthAsync`
```csharp
public virtual ApiResponse<HealthStatus> GetHealthAsync()
```
Performs an asynchronous health check against the event bus and its dependencies. Returns an `ApiResponse<HealthStatus>` indicating whether the bus is reachable and functioning correctly. The `Data` property contains the health status payload. Failures such as timeouts or dependency unavailability are returned in the error envelope; the method itself does not throw in normal operation.

### ApiResponse\<T\> Members

All controller methods return `ApiResponse<T>`, a generic envelope with the following shape.

#### `IsSuccess`
```csharp
public bool IsSuccess { get; }
```
Indicates whether the operation completed successfully. `true` when the response contains valid `Data` and no error information; `false` when `ErrorMessage` is populated.

#### `Data`
```csharp
public T? Data { get; }
```
The payload of a successful operation. Null when `IsSuccess` is `false`.

#### `ErrorMessage`
```csharp
public string? ErrorMessage { get; }
```
A human-readable error description. Non-null only when `IsSuccess` is `false`.

#### `Timestamp`
```csharp
public DateTime Timestamp { get; }
```
The UTC instant at which the response was constructed.

#### `Success` (static)
```csharp
public static ApiResponse<T> Success(T data)
```
Factory method that creates a successful response envelope wrapping the supplied `data`. Sets `IsSuccess` to `true`, populates `Data`, and stamps `Timestamp`.

#### `Error` (static)
```csharp
public static ApiResponse<T> Error(string errorMessage)
```
Factory method that creates a failed response envelope with the given `errorMessage`. Sets `IsSuccess` to `false`, leaves `Data` as default, and stamps `Timestamp`.

### EventPublishResult Members

Returned inside `ApiResponse<T>` from `PublishEventAsync`.

#### `EventId`
```csharp
public string? EventId { get; }
```
The unique identifier assigned to the published event. Null if publication failed before an ID could be generated.

#### `EventType`
```csharp
public string? EventType { get; }
```
The routed event type string. Null on failure.

#### `PublishedAt`
```csharp
public DateTime PublishedAt { get; }
```
The UTC timestamp at which the event was accepted by the bus.

#### `Success`
```csharp
public bool Success { get; }
```
Indicates whether this individual event was successfully published.

### BatchPublishResult Members

Returned inside `ApiResponse<T>` from `PublishBatchAsync`.

#### `BatchId`
```csharp
public string? BatchId { get; }
```
The unique identifier assigned to the entire batch operation. Null if the batch request was rejected before an ID was assigned.

#### `EventCount`
```csharp
public int EventCount { get; }
```
The number of events included in the batch.

#### `PublishedAt`
```csharp
public DateTime PublishedAt { get; }
```
The UTC timestamp at which the batch was accepted.

#### `Success`
```csharp
public bool Success { get; }
```
Indicates whether the batch as a whole was successfully published.

### EventBusStats Members

Returned inside `ApiResponse<T>` from `GetStats`.

#### `Status`
```csharp
public string? Status { get; }
```
A string representing the current operational status (e.g., `"Running"`, `"Degraded"`).

#### `TotalEventsPublished`
```csharp
public long TotalEventsPublished { get; }
```
The cumulative count of events published through the bus since startup.

## Usage

### Example 1: Publish a single event and inspect the result

```csharp
var controller = new EventBusApiController(/* dependencies */);

ApiResponse<EventPublishResult> response = await controller.PublishEventAsync(
    eventType: "order.created",
    body: new { OrderId = 12345, Amount = 99.95m }
);

if (response.IsSuccess && response.Data?.Success == true)
{
    Console.WriteLine(
        $"Event {response.Data.EventId} of type {response.Data.EventType} " +
        $"published at {response.Data.PublishedAt:O}.");
}
else
{
    Console.WriteLine($"Publish failed: {response.ErrorMessage}");
}
```

### Example 2: Publish a batch and check health

```csharp
var controller = new EventBusApiController(/* dependencies */);

// Publish a batch of events
var batchResponse = await controller.PublishBatchAsync(new[]
{
    new { EventType = "user.registered", Body = new { UserId = 1 } },
    new { EventType = "user.verified", Body = new { UserId = 1 } }
});

if (batchResponse.IsSuccess && batchResponse.Data?.Success == true)
{
    Console.WriteLine(
        $"Batch {batchResponse.Data.BatchId} with {batchResponse.Data.EventCount} events " +
        $"published at {batchResponse.Data.PublishedAt:O}.");
}

// Verify system health afterward
ApiResponse<HealthStatus> health = await controller.GetHealthAsync();
Console.WriteLine(
    $"Health check: {(health.IsSuccess ? "Healthy" : health.ErrorMessage)}");

// Read cumulative stats
ApiResponse<EventBusStats> stats = controller.GetStats();
if (stats.IsSuccess)
{
    Console.WriteLine(
        $"Status: {stats.Data?.Status}, Total events: {stats.Data?.TotalEventsPublished}");
}
```

## Notes

- **Error handling**: `PublishEventAsync`, `PublishBatchAsync`, and `GetHealthAsync` do not throw for typical failures such as broker unavailability, serialization errors, or timeouts. These conditions are returned as `ApiResponse<T>` instances with `IsSuccess == false` and a populated `ErrorMessage`. Callers should always check `IsSuccess` before accessing `Data`.
- **Synchronous statistics**: `GetStats` reads from in-memory counters and returns immediately. It does not perform I/O and is safe to call from synchronous contexts without risk of deadlock.
- **Thread safety**: The static factory methods `ApiResponse<T>.Success` and `ApiResponse<T>.Error` are pure functions that allocate new instances and are safe to call from any thread. Instance methods on the controller are not guaranteed thread-safe by their signatures; concurrent calls to `PublishEventAsync` or `PublishBatchAsync` should be serialized or protected by the caller if shared mutable state is involved in the underlying bus implementation.
- **Nullability**: `Data`, `EventId`, `EventType`, `BatchId`, and `Status` are nullable reference types. They carry meaningful values only when the corresponding `Success` or `IsSuccess` flag is `true`. Accessing them on a failed response yields `null`.
- **Timestamps**: All `PublishedAt` and `Timestamp` values are recorded in UTC. Consumers performing local-time comparisons must apply the appropriate offset.
- **Batch semantics**: `BatchPublishResult.Success` reflects the acceptance of the entire batch by the bus. Depending on the underlying transport, individual events within a successfully accepted batch may still fail downstream; the batch result does not provide per-event granularity.
