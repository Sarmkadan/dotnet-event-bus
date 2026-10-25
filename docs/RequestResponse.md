# Request/Response over the Event Bus

Reference for `DotnetEventBus.Advanced.RequestResponseBus`, `RequestResponseHandler<TRequest, TResponse>`, and the `RequestMessage<T>` / `ResponseMessage<T>` message types, defined in `src/DotnetEventBus/Advanced/RequestResponsePattern.cs`.

The event bus itself is publish/subscribe. This pattern adds a synchronous-looking call on top of it: a caller sends a request and awaits a typed response. Correlation happens through a request id that the bus generates and the responder echoes back.

The `RequestResponseBus` does not know about `IEventBus`. You connect the two yourself through the publish delegate (see [Wiring to the event bus](#wiring-to-the-event-bus)).

## How it works

1. `RequestAsync` generates a request id (a `Guid` string) and registers a `TaskCompletionSource` under that id in a `ConcurrentDictionary`.
2. If a publish delegate was supplied, the bus wraps the payload in a `RequestMessage<object>` (with `RequestId` and `Timeout` set) and passes it, with the event type, to that delegate.
3. The responder receives the message and calls `SendResponse(requestId, response)` or `FailRequest(requestId, exception)` on the same `RequestResponseBus` instance.
4. The pending task completes. The entry is removed from the dictionary in a `finally` block, so a completed, failed, timed-out, or cancelled request never leaks an entry.

The entry is registered before the publish happens, so a responder that answers synchronously inside the publish call is not lost.

## API

### `RequestResponseBus`

Coordinates pending requests. Thread-safe: the pending map is a `ConcurrentDictionary`.

#### Constructors

- `RequestResponseBus(TimeSpan? defaultTimeout = null)`: creates a bus with no transport. `RequestAsync` registers the request and waits. Something else must call `SendResponse` or `FailRequest` for that id, otherwise the request times out. The default timeout is 30 seconds.
- `RequestResponseBus(Func<string, RequestMessage<object>, Task> publishRequest, TimeSpan? defaultTimeout = null)`: creates a bus that delivers each outgoing request through `publishRequest`. The first argument is the event type string passed to `RequestAsync`; the second is the message to send. Throws `ArgumentNullException` if `publishRequest` is `null`.

#### `RequestAsync`

```csharp
public async Task<TResponse?> RequestAsync<TRequest, TResponse>(
    string eventType,
    TRequest request,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default)
    where TRequest : class
    where TResponse : class
```

Sends a request and waits for the matching response.

- `eventType`: passed through to the publish delegate. The bus does not interpret it.
- `request`: the request payload. It is stored as `object` in the outgoing message.
- `timeout`: overrides the bus default for this call. It is also copied into `RequestMessage<object>.Timeout`.
- `cancellationToken`: cancels the wait. The request is linked to the timeout, so whichever fires first wins.
- Returns `TResponse?`. The response is converted with `as`, so a response of a different type yields `null` rather than an exception.
- Throws `ArgumentNullException` if `eventType` or `request` is `null`.
- Throws `TimeoutException` if no response arrives in time. The message is `Request {id} timed out after {seconds}s`.
- Throws `OperationCanceledException` (`TaskCanceledException` for the caller's own token) if the token is cancelled or `CancelAllRequests` is called.
- Rethrows any exception the publish delegate throws. The pending entry is removed first.
- Rethrows the exception passed to `FailRequest`, unwrapped.

#### `SendResponse`

```csharp
public bool SendResponse(string requestId, object response)
```

Completes the pending request with `response`. Removes the entry from the pending map.

- Returns `true` if the response was set. Returns `false` if the id is unknown or the request already completed (for example, because it timed out).
- Throws `ArgumentNullException` if `requestId` or `response` is `null`. A `null` response is rejected, so a caller never receives `null` from a successful response.

#### `FailRequest`

```csharp
public bool FailRequest(string requestId, Exception exception)
```

Completes the pending request with an exception. The caller's `RequestAsync` throws that exception.

- Returns `true` if the exception was set; `false` if the id is unknown or the request already completed.
- Throws `ArgumentNullException` if `requestId` or `exception` is `null`.

#### `GetPendingRequestCount`

```csharp
public int GetPendingRequestCount()
```

Returns the number of entries currently in the pending map. Useful for metrics and for tests that check for leaks.

#### `CancelAllRequests`

```csharp
public void CancelAllRequests(string reason = "Bus shutting down")
```

Fails every pending request with an `OperationCanceledException(reason)` and clears the map. Note that the exception is set through `TrySetException`, so callers see `OperationCanceledException`, not `TaskCanceledException`. Catch `OperationCanceledException` to handle both cases.

#### `AuditPendingRequests`

```csharp
public Dictionary<string, string> AuditPendingRequests()
```

Returns a snapshot of request id to task status name (`Completed`, `Faulted`, `Canceled`, `Running`, `Waiting`, and so on). Intended for diagnostics. Entries normally disappear as soon as their task completes, so a non-empty result with completed statuses points to a bug.

#### `CleanupCompletedRequests`

```csharp
public int CleanupCompletedRequests()
```

Removes entries whose task has already completed and returns how many were removed. This is a defensive operation; the normal flow already removes entries in `finally`, so it should rarely remove anything.

### `RequestResponseHandler<TRequest, TResponse>`

```csharp
public abstract class RequestResponseHandler<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
```

Base class for responders. Subclasses implement `HandleAsync`; `ProcessRequestAsync` takes care of sending the result back.

- `protected string? RequestId { get; set; }`: the id of the request being processed. Set by `ProcessRequestAsync`.
- `protected RequestResponseBus? Bus { get; set; }`: the bus used to answer. Set by `ProcessRequestAsync`.
- `public abstract Task<TResponse> HandleAsync(TRequest request)`: produce the response.
- `public async Task ProcessRequestAsync(TRequest request, string requestId, RequestResponseBus bus)`: calls `HandleAsync`, then `SendResponse`. If `HandleAsync` throws, or returns `null`, the exception is passed to `FailRequest`, so the caller's `RequestAsync` throws it. Throws `ArgumentNullException` for any `null` argument before doing anything else.

`RequestId` and `Bus` are instance properties that `ProcessRequestAsync` overwrites on every call. Create a new handler instance for each incoming message (or resolve one per message from DI). Sharing one instance between concurrent messages lets one request's id overwrite another's.

### `RequestMessage<T>`

```csharp
public sealed class RequestMessage<T> where T : class
```

The envelope the bus sends to the responder.

| Member | Type | Notes |
|---|---|---|
| `RequestId` | `string?` | Set by the bus. Use it when calling `SendResponse` or `FailRequest`. |
| `Payload` | `T` (required) | The request payload. With the delegate constructor, `T` is `object`. |
| `Metadata` | `Dictionary<string, object>` | Empty. `RequestAsync` does not populate it, and there is no public way to add entries through the bus. |
| `CreatedAt` | `DateTime` | Set to `DateTime.UtcNow` when the message is constructed. |
| `Timeout` | `TimeSpan` | Set by the bus to the effective timeout for this request. |

### `ResponseMessage<T>`

```csharp
public sealed class ResponseMessage<T> where T : class
```

A response DTO with `RequestId`, `Payload` (required), `Success` (default `true`), `ErrorMessage`, and `CreatedAt`. `RequestResponseBus` does not use it; the bus completes requests with the raw response object. Use it only if your transport needs a serializable reply shape.

## Wiring to the event bus

The request travels as a `RequestMessage<object>` published as an ordinary event. The responder subscribes to that type and answers through the same `RequestResponseBus` instance.

```csharp
using DotnetEventBus.Advanced;
using DotnetEventBus.Services;

public sealed record PriceQuery(string Sku);
public sealed record PriceQuote(string Sku, decimal Price);

// 1. Create the bus with a publish delegate that sends requests onto the event bus.
var requestBus = new RequestResponseBus(
    (eventType, message) => eventBus.PublishAsync(message, typeof(RequestMessage<object>)),
    defaultTimeout: TimeSpan.FromSeconds(5));

// 2. Responder: subscribe to the request envelope and answer via the same bus.
eventBus.Subscribe<RequestMessage<object>>((message, _) =>
{
    if (message.RequestId is null)
    {
        return Task.CompletedTask;
    }

    if (message.Payload is PriceQuery query)
    {
        requestBus.SendResponse(message.RequestId, new PriceQuote(query.Sku, 19.99m));
    }
    else
    {
        requestBus.FailRequest(message.RequestId, new InvalidOperationException("Unexpected request type."));
    }

    return Task.CompletedTask;
});

// 3. Caller: send a request and await the typed response.
try
{
    var quote = await requestBus.RequestAsync<PriceQuery, PriceQuote>(
        "pricing.quote",
        new PriceQuery("SKU-42"),
        timeout: TimeSpan.FromSeconds(2),
        cancellationToken: ct);

    // quote is null only if the response was not a PriceQuote.
}
catch (TimeoutException)
{
    // No responder answered in time.
}
```

Notes on the example:

- The delegate ignores `eventType`. Use it if you route different request kinds to different event types, mapping the string to a `Type` yourself.
- `PublishAsync(object, Type, ...)` is used so the message is published under `RequestMessage<object>`, which matches the subscription.
- The responder must call `SendResponse` or `FailRequest`. A handler that returns without answering leaves the caller waiting until the timeout.
- The bus is in-process. Both sides must share the same `RequestResponseBus` instance. For cross-process use you need a transport that carries `RequestId`, `Payload`, and the response back to the caller, and you must write that part yourself.

### Using a handler class

`RequestResponseHandler` moves the send-back logic into a base class:

```csharp
public sealed class PriceQuoteHandler : RequestResponseHandler<PriceQuery, PriceQuote>
{
    public override Task<PriceQuote> HandleAsync(PriceQuery request) =>
        Task.FromResult(new PriceQuote(request.Sku, 19.99m));
}

eventBus.Subscribe<RequestMessage<object>>(async (message, ct) =>
{
    if (message.RequestId is null || message.Payload is not PriceQuery query)
    {
        return;
    }

    // A new handler per message: RequestId and Bus are set on the instance.
    var handler = new PriceQuoteHandler();
    await handler.ProcessRequestAsync(query, message.RequestId, requestBus);
});
```

## Behaviour summary

| Situation | Caller sees | Entry removed |
|---|---|---|
| `SendResponse` with a matching `TResponse` | the response | yes |
| `SendResponse` with an unrelated type | `null` | yes |
| `FailRequest(id, ex)` | `ex` | yes |
| Handler throws inside `ProcessRequestAsync` | the handler's exception | yes |
| Timeout elapses | `TimeoutException` | yes |
| Caller's token cancelled | `TaskCanceledException` | yes |
| `CancelAllRequests(reason)` | `OperationCanceledException(reason)` | yes |
| Publish delegate throws | the delegate's exception | yes |
| Response arrives after timeout | nothing (`SendResponse` returns `false`) | already removed |

## Limitations

- Only one response per request. A second `SendResponse` or `FailRequest` for the same id returns `false`.
- `RequestAsync` is not a broker. It does not retry, and it does not persist requests. A process restart drops every pending request.
- `Metadata` on the envelope is always empty when created by the bus.
- The request id is a `Guid` string generated per call. Do not assume any ordering between requests.

## See also

- [RequestResponseBus](RequestResponseBus.md): an older, separate write-up. Its signatures (`RequestContext`, the `RequestAsync(payload, timeout, metadata)` overload) do not match the code in `RequestResponsePattern.cs`, so treat this document as the reference for current behaviour.
- [EventBus](EventBus.md): the publish/subscribe core this pattern runs on.
- `tests/DotnetEventBus.Tests/RequestResponsePatternTests.cs`: unit tests for the behaviour described here.
