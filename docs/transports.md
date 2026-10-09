# Transports

Transports deliver an `EventEnvelope` to a destination. Each transport implements `IEventTransport` and is registered in `ITransportRegistry`, which selects the default transport and reports per-transport status.

Source: `src/DotnetEventBus/Transport/`

## Contents

- `IEventTransport` - transport contract
- `TransportCapabilities` - capability flags
- `TransportPublishResult` - publish outcome
- `TransportStatus` - health and metrics snapshot
- `InProcessTransport` - delivery inside the same process
- `WebhookTransport` - HTTP delivery to external endpoints
- `ITransportRegistry` / `TransportRegistry` - lookup and default selection
- DI registration

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

| Member | Purpose |
|---|---|
| `TransportId` | Unique key used by the registry and by `ConfigureTransportRegistry`. |
| `TransportType` | Free-form category label, e.g. `in-process`, `webhook`. Planned values for brokers: `rabbitmq`, `kafka`. |
| `Capabilities` | Bit flags describing what the transport supports. |
| `PublishAsync` | Delivers one envelope. Returns a result instead of throwing for delivery failures. |
| `GetStatus` | Returns a `TransportStatus` snapshot. |

Both shipped implementations reject an invalid envelope (`envelope.IsValid()` is false) by returning a failed result with `"Invalid event envelope"`, without attempting delivery.

## TransportCapabilities

Flags, combined with `|`:

| Flag | Meaning |
|---|---|
| `SupportsFireAndForget` | Publish without waiting for a reply. |
| `SupportsRequestReply` | Request/reply pattern. |
| `SupportsBatching` | Batch publishing. |
| `SupportsPriority` | Priority-based delivery. |
| `SupportsPersistence` | Messages are persisted by the transport. |
| `IsInProcess` | Events never leave the application. |
| `IsRemote` | Events are sent over the network. |

Declared flags per shipped transport:

| Transport | Flags |
|---|---|
| `InProcessTransport` | FireAndForget, RequestReply, Batching, Priority, Persistence, IsInProcess |
| `WebhookTransport` | FireAndForget, Batching, Priority, IsRemote |

Note: the flags are declarative. `InProcessTransport` declares `SupportsPersistence`, but it holds events only for the duration of the call and does not persist them. Callers must not rely on the flag for durability.

## TransportPublishResult

| Property | Type | Description |
|---|---|---|
| `Success` | `bool` | Delivery outcome. |
| `EventId` | `string?` | Envelope `EventId`, or a generated GUID when the envelope has none. |
| `ErrorMessage` | `string?` | Error text on failure. |
| `Exception` | `Exception?` | Underlying exception on failure. |
| `CompletedAt` | `DateTime` | UTC timestamp, set at construction. |

Factories: `SuccessResult(eventId)` and `FailedResult(eventId, exception, errorMessage?)`.

## TransportStatus

| Property | Description |
|---|---|
| `TransportId`, `TransportType` | Identity of the reporting transport. |
| `IsHealthy` | Health flag. |
| `StatusMessage` | Human-readable text. |
| `CheckedAt` | UTC timestamp of the snapshot. |
| `MessagesPublished` | Counter, see known issues below. |
| `FailedPublishes` | Count of failed publishes. |
| `AveragePublishTimeMs` | Mean duration of successful publishes. |

## InProcessTransport

Delegates to `IEventBus.PublishAsync(payload, payload.GetType(), correlationId, ct)`. Events go to handlers registered in the same process.

- `TransportId`: `in-process-transport`
- `TransportType`: `in-process`
- Constructor: `InProcessTransport(IEventBus eventBus, ILogger<InProcessTransport>? logger = null)`. `eventBus` is required.
- `IsHealthy` is always `true`. The status message is `In-process transport is operational`.
- On success, `EventId` is the envelope's `EventId`, falling back to the bus result's `MessageId`.
- On exception from the bus, the exception is caught, logged, and returned as a failed result. It is not rethrown.

## WebhookTransport

Fans out one envelope to every webhook subscription that matches `envelope.EventType`, using `WebhookHandler` for HTTP delivery.

- `TransportId`: `webhook-transport`
- `TransportType`: `webhook`
- Constructor: `WebhookTransport(WebhookHandler webhookHandler, ILogger<WebhookTransport>? logger = null)`.
- Management methods (not on the interface):
  - `Subscribe(WebhookSubscription)` - registers a subscription.
  - `bool Unsubscribe(string subscriptionId)` - removes a subscription, returns `true` if found.
- Behavior of `PublishAsync`:
  1. Resolve matching subscriptions via `WebhookHandler.GetWebhooksForEvent(eventType)`.
  2. If none match, return success without any HTTP call.
  3. Otherwise deliver to all matches in parallel (`Task.WhenAll`).
  4. Any single delivery failure fails the whole publish; the result carries the exception.
- Payload sent to each endpoint is a JSON object with: `EventId`, `EventType`, `Version`, `CreatedAt`, `CorrelationId`, `CausationId`, `Source`, `Actor`, `Metadata`, `Payload`, `ProcessingAttempts`, `Priority`, `IsCritical`.
- Retries are delegated to `WebhookHandler.DeliverEventAsync`, which uses a `RetryPolicy` configured per subscription (`MaxRetryAttempts`, `InitialRetryDelay`, `RetryBackoffMultiplier`, `MaxRetryDelay`, `UseJitter`).
  - Retried: transient failures (5xx, timeouts, network errors).
  - Not retried: 4xx, except 408 (Request Timeout).
- Status message: `Webhook transport is operational`. `IsHealthy` is always `true`.

Signing: `AddWebhookTransport(signingSecret)` passes the secret to `WebhookHandler`. Receivers verify the signature with the same secret.

## ITransportRegistry / TransportRegistry

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

- Storage is a `ConcurrentDictionary` keyed by `TransportId`. Registering a transport with an existing ID replaces the previous instance.
- `DefaultTransport` throws `InvalidOperationException` ("No default transport registered") if no default was set. Check registration before calling it.
- `RegisterTransport(..., isDefault: true)` replaces the default.

## DI registration

```csharp
services.AddInProcessTransport(options => { /* EventBusOptions */ });  // also calls AddEventBus
services.AddWebhookTransport(signingSecret: "...");
services.ConfigureTransportRegistry("webhook-transport");              // default transport IDs, first match wins
```

- `AddInProcessTransport` calls `AddEventBus` and registers an `IEventTransport` singleton.
- `AddWebhookTransport` registers a `WebhookHandler` singleton and an `IEventTransport` singleton.
- `ConfigureTransportRegistry` registers `ITransportRegistry`. It adds every registered `IEventTransport` and marks the first ID in the argument list that matches a registered transport as default. If no ID matches, no default is set.

Configure transports before resolving `ITransportRegistry`, since the registry is built from the transports present at resolution time.

## Known issues

- **Double-counted successes.** In both `InProcessTransport` and `WebhookTransport`, `PublishAsync` calls `IncrementMessagesPublished()` at the start and `RecordSuccess()` on success. `RecordSuccess()` increments the same counter again. Each successful publish therefore adds 2 to `MessagesPublished`. Failed publishes add 1. Until fixed, treat `MessagesPublished` as an inflated value.
- **Non-atomic timing sum.** `ConcurrentCounter.RecordPublishTime` updates `_totalPublishTimeMs` with `+=` outside any lock. Under concurrent publishes the average can be slightly off. The count uses `Interlocked`, the sum does not.
- **Default-transport lookup.** `ConfigureTransportRegistry` silently leaves no default if the listed IDs are not registered. Callers that expect a default will get `InvalidOperationException` at first use.
