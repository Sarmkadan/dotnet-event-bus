# EventBus

`EventBus` is the central publish-subscribe and request-response mediator in the `dotnet-event-bus` library. It provides a decoupled communication channel where publishers emit events or requests without knowing the consumers, and subscribers handle those messages asynchronously or synchronously. The bus supports both in-process typed events and raw distributed event processing, with built-in subscription lifecycle management.

## API

### Constructors

```csharp
public EventBus()
public EventBus(EventBusOptions options)
```

Creates a new instance of the event bus. The parameterless constructor uses default options. The overload accepting `EventBusOptions` allows customization of bus behavior such as error handling strategies, timeouts, or serialization settings.

**Parameters:**
- `options`: An `EventBusOptions` instance configuring the bus.

**Throws:**
- `ArgumentNullException` when `options` is `null`.

---

### PublishAsync

```csharp
public async Task<PublishResult> PublishAsync<TEvent>(TEvent event)
public async Task<PublishResult> PublishAsync<TEvent>(TEvent event, CancellationToken cancellationToken)
```

Publishes an event of type `TEvent` to all subscribers registered for that type. Subscribers are invoked asynchronously. The method returns a `PublishResult` describing the outcome (e.g., number of handlers invoked, any errors encountered).

**Parameters:**
- `event`: The event payload to publish. Must not be `null`.
- `cancellationToken`: Optional token to cancel the publish operation.

**Returns:**
- A `PublishResult` containing details about handler execution.

**Throws:**
- `ArgumentNullException` when `event` is `null`.
- `OperationCanceledException` when the cancellation token is triggered before completion.

---

### SendAsync

```csharp
public async Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request)
public async Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
```

Sends a request of type `TRequest` and expects a single response of type `TResponse`. This follows the request-response pattern where exactly one subscriber handles the request and returns a result. If zero or multiple handlers are registered, the behavior is defined by the bus configuration (typically throws or returns a default value).

**Parameters:**
- `request`: The request payload. Must not be `null`.
- `cancellationToken`: Optional token to cancel the send operation.

**Returns:**
- The response of type `TResponse` produced by the registered handler.

**Throws:**
- `ArgumentNullException` when `request` is `null`.
- `InvalidOperationException` when no handler or multiple handlers are registered for the request type (depending on configuration).
- `OperationCanceledException` when the cancellation token is triggered.

---

### Subscribe

```csharp
public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler)
public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
```

Registers an asynchronous handler for events of type `TEvent`. The handler is invoked each time an event of that type is published. Returns an `IDisposable` token that, when disposed, unsubscribes the handler.

**Parameters:**
- `handler`: An async function that receives the event and optionally a cancellation token.

**Returns:**
- An `IDisposable` representing the subscription. Disposing it removes the handler.

**Throws:**
- `ArgumentNullException` when `handler` is `null`.

---

### SubscribeSync

```csharp
public IDisposable SubscribeSync<TEvent>(Action<TEvent> handler)
```

Registers a synchronous handler for events of type `TEvent`. The handler is invoked synchronously during publish. Returns a disposable subscription token.

**Parameters:**
- `handler`: A synchronous action that receives the event.

**Returns:**
- An `IDisposable` representing the subscription.

**Throws:**
- `ArgumentNullException` when `handler` is `null`.

---

### SubscribeRequest

```csharp
public IDisposable SubscribeRequest<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler)
public IDisposable SubscribeRequest<TRequest, TResponse>(Func<TRequest, CancellationToken, Task<TResponse>> handler)
```

Registers a handler for request-response messaging. The handler receives a request of type `TRequest` and returns a response of type `TResponse`. Only one handler per request type is typically allowed; subsequent registrations may replace or throw depending on configuration.

**Parameters:**
- `handler`: An async function that processes the request and returns a response.

**Returns:**
- An `IDisposable` representing the subscription.

**Throws:**
- `ArgumentNullException` when `handler` is `null`.
- `InvalidOperationException` when a handler is already registered and the bus does not allow overwriting.

---

### UnsubscribeAsync

```csharp
public async Task UnsubscribeAsync<TEvent>(IDisposable subscription)
```

Removes a previously registered subscription asynchronously. This is an alternative to disposing the subscription token directly and may perform additional cleanup.

**Parameters:**
- `subscription`: The subscription token returned from `Subscribe`, `SubscribeSync`, or `SubscribeRequest`.

**Throws:**
- `ArgumentNullException` when `subscription` is `null`.
- `ArgumentException` when the subscription was not created by this bus instance.

---

### GetSubscriptionsAsync

```csharp
public async Task<IEnumerable<string>> GetSubscriptionsAsync()
```

Returns a collection of strings representing the currently active subscriptions. The format and content of each string are implementation-defined, typically including the event type name and handler information.

**Returns:**
- An `IEnumerable<string>` describing active subscriptions.

---

### ClearSubscriptionsAsync

```csharp
public async Task ClearSubscriptionsAsync()
```

Removes all active subscriptions from the bus. After calling this method, no handlers will respond to published events or requests until new subscriptions are registered.

---

### GetOptions

```csharp
public EventBusOptions GetOptions()
```

Returns the `EventBusOptions` instance that was used to configure this bus. If the parameterless constructor was used, returns the default options.

**Returns:**
- The `EventBusOptions` associated with this bus instance.

---

### ProcessRawDistributedEventAsync

```csharp
public async Task<PublishResult> ProcessRawDistributedEventAsync(string eventType, byte[] payload)
public async Task<PublishResult> ProcessRawDistributedEventAsync(string eventType, byte[] payload, CancellationToken cancellationToken)
```

Processes a raw event received from an external or distributed source. The event type string is used to resolve local handlers, and the payload is deserialized according to bus configuration before being dispatched.

**Parameters:**
- `eventType`: The type name or routing key identifying the event.
- `payload`: The raw serialized event data.
- `cancellationToken`: Optional token to cancel processing.

**Returns:**
- A `PublishResult` describing the outcome of local handler execution.

**Throws:**
- `ArgumentNullException` when `eventType` or `payload` is `null`.
- `ArgumentException` when `eventType` is empty or whitespace.
- `OperationCanceledException` when the cancellation token is triggered.

---

### SubscriptionDisposable

```csharp
public class SubscriptionDisposable : IDisposable
```

A concrete implementation of `IDisposable` returned by subscription methods. Calling `Dispose` on this object removes the associated handler from the bus.

**Members:**
- `public void Dispose()`: Unsubscribes the handler. Safe to call multiple times; subsequent calls have no effect.

---

### Dispose

```csharp
public void Dispose()
```

Disposes the event bus, releasing all resources and clearing all subscriptions. After disposal, any attempt to publish, send, or subscribe will throw an `ObjectDisposedException`.

## Usage

### Example 1: In-process publish-subscribe

```csharp
// Define an event type
public record OrderPlacedEvent(Guid OrderId, string Customer, decimal Total);

// Create the bus
var bus = new EventBus();

// Subscribe an async handler
IDisposable subscription = bus.Subscribe<OrderPlacedEvent>(async order =>
{
    Console.WriteLine($"Processing order {order.OrderId} for {order.Customer}");
    await Task.Delay(100); // Simulate work
});

// Publish an event
var result = await bus.PublishAsync(new OrderPlacedEvent(
    Guid.NewGuid(), "customer@example.com", 99.99m));

Console.WriteLine($"Handlers invoked: {result.HandlerCount}");

// Later, unsubscribe
subscription.Dispose();
```

### Example 2: Request-response with cancellation

```csharp
// Define request and response types
public record GetUserRequest(string UserId);
public record GetUserResponse(string UserId, string Name, string Email);

// Create the bus with custom options
var options = new EventBusOptions { HandlerTimeout = TimeSpan.FromSeconds(5) };
var bus = new EventBus(options);

// Register a request handler
bus.SubscribeRequest<GetUserRequest, GetUserResponse>(async (request, ct) =>
{
    // Simulate database lookup
    await Task.Delay(200, ct);
    return new GetUserResponse(request.UserId, "John Doe", "john@example.com");
});

// Send a request with cancellation support
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
try
{
    var response = await bus.SendAsync<GetUserRequest, GetUserResponse>(
        new GetUserRequest("user-123"), cts.Token);
    Console.WriteLine($"Retrieved user: {response.Name}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Request timed out");
}
```

## Notes

- **Thread safety:** All public methods on `EventBus` are thread-safe. Subscriptions can be added or removed concurrently with publish operations without external synchronization.
- **Handler execution order:** Handlers for a given event type are invoked in the order they were registered. Synchronous handlers registered via `SubscribeSync` run on the publisher's thread; asynchronous handlers run on captured synchronization contexts or the thread pool depending on the bus configuration.
- **Error handling during publish:** If a handler throws an exception, the bus captures it in the `PublishResult` and continues invoking remaining handlers. The publish operation itself does not throw unless the cancellation token is triggered or a fatal bus-level error occurs.
- **Request-response cardinality:** By default, exactly one handler must be registered for a request type. If zero or multiple handlers are present, `SendAsync` throws `InvalidOperationException`. This behavior can be adjusted via `EventBusOptions`.
- **Subscription disposal vs. UnsubscribeAsync:** Disposing the `IDisposable` token is the preferred way to unsubscribe. `UnsubscribeAsync` exists for scenarios where the token is not accessible and the subscription must be removed by reference.
- **Raw distributed events:** `ProcessRawDistributedEventAsync` expects the payload to be deserializable into the type indicated by `eventType`. If deserialization fails, the error is recorded in the `PublishResult` and no handlers are invoked.
- **Lifecycle:** Once `Dispose` is called on the bus, all subscriptions are cleared and any further operations throw `ObjectDisposedException`. Disposing the bus also disposes all outstanding `SubscriptionDisposable` instances.
- **Cancellation tokens in handlers:** When a handler accepts a `CancellationToken`, the bus passes the token from the publish or send call. If the caller cancels the token, cooperative handlers can abort early, but the bus does not forcibly terminate non-cooperative handlers.
