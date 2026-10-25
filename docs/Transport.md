# Transport

The `DotnetEventBus.Transport` namespace defines how an `EventEnvelope` leaves the publisher and reaches its destination. It contains one interface for transports (`IEventTransport`), two transport implementations (`InProcessTransport` and `WebhookTransport`), and a registry (`ITransportRegistry`, implemented by `TransportRegistry`) that holds transports, selects a default, and reports their status.

Source: `src/DotnetEventBus/Transport/`

## Types

| Type | File | Kind | Purpose |
|------|------|------|---------|
| `IEventTransport` | `IEventTransport.cs` | interface | Transport contract |
| `TransportCapabilities` | `IEventTransport.cs` | `[Flags]` enum | Capabilities a transport advertises |
| `TransportPublishResult` | `IEventTransport.cs` | sealed class | Outcome of one publish call |
| `TransportStatus` | `IEventTransport.cs` | sealed class | Health and metrics snapshot |
| `InProcessTransport` | `InProcessTransport.cs` | sealed class | Delivers to handlers in the same process |
| `WebhookTransport` | `WebhookTransport.cs` | sealed class | Delivers over HTTP to subscribed endpoints |
| `ITransportRegistry` | `ITransportRegistry.cs` | interface | Lookup, registration, and default selection |
| `TransportRegistry` | `ITransportRegistry.cs` | sealed class | Default `ITransportRegistry` implementation |

---

## IEventTransport

```csharp
public interface IEventTransport
{
    string TransportId { get; }
    string TransportType { get; }
    TransportCapabilities Capabilities { get; }
    Task<TransportPublishResult> PublishAsync(EventEnvelope envelope, CancellationToken cancellationToken = default);
    TransportStatus GetStatus();
}
```

Contract for every transport. Transports are expected to be cross-cutting-friendly: the same interface is meant to sit behind circuit breakers, retry policies, and similar wrappers, regardless of whether delivery is in-process, webhook, or a broker.

### TransportId

Stable identifier used as the key in `ITransportRegistry`. Two transports with the same `TransportId` replace each other in the registry.

### TransportType

Free-form category string, for example `"in-process"`, `"webhook"`, `"rabbitmq"`, or `"kafka"`. It is reported in `TransportStatus` and is not used for lookup.

### Capabilities

`TransportCapabilities` is a `[Flags]` enum. Each transport reports the union of the flags it supports:

| Flag | Bit | Meaning |
|------|-----|---------|
| `SupportsFireAndForget` | `1 << 0` | Publish without waiting for a reply |
| `SupportsRequestReply` | `1 << 1` | Request/reply pattern |
| `SupportsBatching` | `1 << 2` | Batch publishing |
| `SupportsPriority` | `1 << 3` | Priority-based delivery |
| `SupportsPersistence` | `1 << 4` | Messages survive process restarts |
| `IsInProcess` | `1 << 5` | Events stay inside the application |
| `IsRemote` | `1 << 6` | Events cross the network |

The flags are declarative. Nothing in the transport layer checks them before calling `PublishAsync`.

### PublishAsync

```csharp
Task<TransportPublishResult> PublishAsync(EventEnvelope envelope, CancellationToken cancellationToken = default);
```

Delivers one envelope. Implementations report delivery failures through the returned `TransportPublishResult`, not by throwing. `InProcessTransport` and `WebhookTransport` both throw `ArgumentNullException` when `envelope` is `null` and return a failed result when `envelope.IsValid()` is `false`.

### GetStatus

```csharp
TransportStatus GetStatus();
```

Returns a snapshot of health and counters. It is synchronous and does not perform I/O.

---

## TransportPublishResult

```csharp
public sealed class TransportPublishResult
{
    public bool Success { get; }
    public string? EventId { get; }
    public string? ErrorMessage { get; }
    public Exception? Exception { get; }
    public DateTime CompletedAt { get; }   // DateTime.UtcNow at construction

    public TransportPublishResult(bool success, string? eventId, string? errorMessage, Exception? exception);
    public static TransportPublishResult SuccessResult(string eventId);
    public static TransportPublishResult FailedResult(string? eventId, Exception exception, string? errorMessage = null);
}
```

- `SuccessResult(eventId)` creates a successful result with no error message or exception.
- `FailedResult(eventId, exception, errorMessage)` creates a failed result. When `errorMessage` is `null`, `ErrorMessage` is set to `exception.Message`.
- `CompletedAt` is set when the result object is constructed, not when the transport finished its work.

---

## TransportStatus

```csharp
public sealed class TransportStatus
{
    public string TransportId { get; }
    public string TransportType { get; }
    public bool IsHealthy { get; }
    public string? StatusMessage { get; }
    public DateTime CheckedAt { get; }            // DateTime.UtcNow at construction
    public long MessagesPublished { get; }
    public long FailedPublishes { get; }
    public double AveragePublishTimeMs { get; }

    public TransportStatus(
        string transportId,
        string transportType,
        bool isHealthy,
        string? statusMessage = null,
        long messagesPublished = 0,
        long failedPublishes = 0,
        double averagePublishTimeMs = 0);
}
```

`transportId` and `transportType` must not be `null`; the constructor throws `ArgumentNullException` otherwise. The remaining values are copied as given.

See [Known issues](#known-issues) for how `MessagesPublished` is currently counted.

---

## InProcessTransport

```csharp
public sealed class InProcessTransport : IEventTransport
public InProcessTransport(IEventBus eventBus, ILogger<InProcessTransport>? logger = null)
```

Delivers events to handlers registered on the same `IEventBus`. This is the transport used by `AddInProcessTransport`.

| Member | Value |
|--------|-------|
| `TransportId` | `"in-process-transport"` |
| `TransportType` | `"in-process"` |
| `Capabilities` | `SupportsFireAndForget \| SupportsRequestReply \| SupportsBatching \| SupportsPriority \| SupportsPersistence \| IsInProcess` |

Constructor: `eventBus` is required and throws `ArgumentNullException` when `null`. `logger` is optional; when `null`, nothing is logged.

### PublishAsync behavior

1. Throws `ArgumentNullException` if `envelope` is `null`.
2. If `envelope.IsValid()` is `false`, returns a failed result with `"Invalid event envelope"` as the message. No delivery is attempted.
3. Calls `IEventBus.PublishAsync(payload, payload.GetType(), correlationId, cancellationToken)` using `envelope.Payload` and `envelope.CorrelationId`.
4. On success, returns `SuccessResult(envelope.EventId ?? result.MessageId)`.
5. If the bus throws, the exception is logged at error level and returned as a failed result. It is not rethrown.

### GetStatus

- `IsHealthy` is always `true`. The transport has no health check of its own beyond the bus being available.
- `StatusMessage` is `"In-process transport is operational"`.
- Counters are read from an internal thread-safe counter. `AveragePublishTimeMs` is `0` until at least one publish has timed.

---

## WebhookTransport

```csharp
public sealed class WebhookTransport : IEventTransport
public WebhookTransport(WebhookHandler webhookHandler, ILogger<WebhookTransport>? logger = null)
```

Fans one envelope out to every active `WebhookSubscription` whose event types match `envelope.EventType`. HTTP delivery and retries are handled by `WebhookHandler`. This is the transport used by `AddWebhookTransport`.

| Member | Value |
|--------|-------|
| `TransportId` | `"webhook-transport"` |
| `TransportType` | `"webhook"` |
| `Capabilities` | `SupportsFireAndForget \| SupportsBatching \| SupportsPriority \| IsRemote` |

Constructor: `webhookHandler` is required and throws `ArgumentNullException` when `null`.

### Subscription management

These members are not part of `IEventTransport`. Call them on the concrete type:

- `void Subscribe(WebhookSubscription subscription)` - registers a subscription with the underlying `WebhookHandler`. Throws `ArgumentNullException` when `null`.
- `bool Unsubscribe(string subscriptionId)` - removes a subscription. Returns `true` if one was found. Throws `ArgumentException` when the ID is `null`, empty, or whitespace.

### PublishAsync behavior

1. Throws `ArgumentNullException` if `envelope` is `null`.
2. If `envelope.IsValid()` is `false`, returns a failed result with `"Invalid event envelope"` as the message.
3. Looks up matching subscriptions with `WebhookHandler.GetWebhooksForEvent(envelope.EventType)`.
4. If none match, returns `SuccessResult` without making any HTTP call.
5. Otherwise, delivers to all matching subscriptions in parallel with `Task.WhenAll`. Each delivery calls `WebhookHandler.DeliverEventAsync`, which applies the subscription's retry settings.
6. If any single delivery fails, the delivery throws `InvalidOperationException` with the message `"Webhook delivery failed: <reason>"`. The outer `catch` logs the error and returns a failed result that carries the exception. Deliveries that already succeeded are not rolled back.

### Payload

Each endpoint receives a JSON object built from the envelope:

| Field | Source |
|-------|--------|
| `EventId` | `envelope.EventId` |
| `EventType` | `envelope.EventType` |
| `Version` | `envelope.Version` |
| `CreatedAt` | `envelope.CreatedAt` |
| `CorrelationId` | `envelope.CorrelationId` |
| `CausationId` | `envelope.CausationId` |
| `Source` | `envelope.Source` |
| `Actor` | `envelope.Actor` |
| `Metadata` | `envelope.Metadata` |
| `Payload` | `envelope.Payload` |
| `ProcessingAttempts` | `envelope.ProcessingAttempts` |
| `Priority` | `envelope.Priority` |
| `IsCritical` | `envelope.IsCritical` |

### Retry settings

Retries are configured per `WebhookSubscription`, not on the transport:

| Property | Default |
|----------|---------|
| `MaxRetryAttempts` | `3` |
| `InitialRetryDelay` | `1 s` |
| `RetryBackoffMultiplier` | `2.0` |
| `MaxRetryDelay` | `5 min` |
| `UseJitter` | `true` |

`WebhookTransport` logs each retry as a warning. Which failures are retried is decided inside `WebhookHandler`.

### Signing

The `signingSecret` passed to `AddWebhookTransport` is given to the `WebhookHandler`. Receivers verify signatures with the same secret.

### GetStatus

- `IsHealthy` is always `true`.
- `StatusMessage` is `"Webhook transport is operational"`.

---

## ITransportRegistry

```csharp
public interface ITransportRegistry
{
    IEventTransport DefaultTransport { get; }
    IEnumerable<IEventTransport> GetAllTransports();
    IEventTransport? GetTransport(string transportId);
    void RegisterTransport(IEventTransport transport, bool isDefault = false);
    Dictionary<string, TransportStatus> GetAllStatuses();
    TransportStatus? GetTransportStatus(string transportId);
}
```

Holds the set of transports known to the application and tracks which one is the default.

### DefaultTransport

Returns the transport registered with `isDefault: true`. Throws `InvalidOperationException` ("No default transport registered") if no default has been set. Check registration before reading it.

### GetAllTransports

Returns the current values of the registry's dictionary. The enumeration is over a live `ConcurrentDictionary`.

### GetTransport

Returns the transport with the given `TransportId`, or `null` if none is registered.

### RegisterTransport

Adds the transport under its `TransportId`. A transport with the same ID replaces the existing entry. Passing `isDefault: true` makes it the default, replacing any previous default. Throws `ArgumentNullException` when `transport` is `null`. Passing `isDefault: false` does not clear an existing default.

### GetAllStatuses / GetTransportStatus

Call `GetStatus()` on each transport and return the results. `GetAllStatuses` returns a dictionary keyed by `TransportId`. `GetTransportStatus` returns `null` when the ID is not registered.

### TransportRegistry

`TransportRegistry` is the only implementation in the library. It stores transports in a `ConcurrentDictionary<string, IEventTransport>`. The default is held in a plain field, so `RegisterTransport(..., isDefault: true)` is not synchronized with concurrent readers of `DefaultTransport`. Register transports at startup, before concurrent use.

---

## DI registration

```csharp
services.AddInProcessTransport(options => { /* EventBusOptions */ });   // also calls AddEventBus
services.AddWebhookTransport(signingSecret: "...");
services.ConfigureTransportRegistry("in-process-transport");           // first matching ID becomes default
```

- `AddInProcessTransport` calls `AddEventBus` and registers an `IEventTransport` singleton that wraps `IEventBus`.
- `AddWebhookTransport` registers a `WebhookHandler` singleton and an `IEventTransport` singleton that wraps it.
- `ConfigureTransportRegistry` registers `ITransportRegistry` as a singleton. When it is resolved, it builds a `TransportRegistry` from every registered `IEventTransport`, then marks the first ID in the argument list that matches a registered transport as the default.

Resolve `ITransportRegistry` after all transports are registered. The registry is built once from the transports present at resolution time.

---

## Known issues

- **Successes are counted twice.** `InProcessTransport.PublishAsync` and `WebhookTransport.PublishAsync` call `IncrementMessagesPublished()` at the start of each publish, then call `RecordSuccess()` on success. `RecordSuccess()` increments the same counter again. A successful publish adds 2 to `MessagesPublished`; a failed publish adds 1. Treat `MessagesPublished` as inflated until this is fixed.
- **Non-atomic timing sum.** `ConcurrentCounter.RecordPublishTime` updates `_totalPublishTimeMs` with `+=` without a lock, while the sample count uses `Interlocked`. Under concurrent publishes `AveragePublishTimeMs` can be slightly off.
- **Health is always true.** Neither transport checks its dependency. `IsHealthy` is `true` unconditionally, so a `TransportStatus` never reports an unhealthy transport.
- **Silent missing default.** `ConfigureTransportRegistry` leaves no default when none of the listed IDs is registered. The first call to `DefaultTransport` then throws `InvalidOperationException`.
- **Webhook fan-out is not atomic.** When one subscription fails, the others may already have been delivered, and the publish is reported as failed. Callers that retry the whole publish will deliver again to subscriptions that already succeeded.
