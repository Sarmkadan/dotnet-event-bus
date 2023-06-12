# IHandlerInvoker

The `IHandlerInvoker` interface defines a contract for invoking event handlers in the `dotnet-event-bus` system. It abstracts the execution of both fire-and-forget events and request-response events, providing a uniform way to check handler compatibility and retrieve supported event types. Implementations of this interface are responsible for locating, validating, and executing the appropriate handler logic for a given event.

## API

### `HandlerInvoker`

A property that returns the underlying `HandlerInvoker` instance associated with this invoker. This property allows consumers to access the concrete invoker object for advanced scenarios such as configuration or lifecycle management.

- **Type**: `HandlerInvoker`
- **Remarks**: The returned object is the same instance that implements the `IHandlerInvoker` interface. It is guaranteed to be non-null.

### `InvokeAsync`

```csharp
public async Task InvokeAsync(object eventData, CancellationToken cancellationToken = default)
```

Invokes the handler for a fire-and-forget event. The method does not return a result; it only signals completion when the handler has finished processing.

- **Parameters**:
  - `eventData`: The event data object to be handled. Must be of a type supported by the invoker.
  - `cancellationToken` (optional): A cancellation token that can be used to cancel the invocation.
- **Return value**: A `Task` representing the asynchronous operation.
- **Throws**:
  - `ArgumentNullException`: If `eventData` is `null`.
  - `InvalidOperationException`: If the invoker cannot handle the type of `eventData` (i.e., `CanHandle` returns `false` for the event type).
  - `OperationCanceledException`: If the operation is canceled via the cancellation token.
  - Any exception thrown by the underlying handler.

### `InvokeRequestAsync`

```csharp
public async Task<object?> InvokeRequestAsync(object eventData, CancellationToken cancellationToken = default)
```

Invokes the handler for a request-response event and returns the response.

- **Parameters**:
  - `eventData`: The request event data object. Must be of a type supported by the invoker.
  - `cancellationToken` (optional): A cancellation token that can be used to cancel the invocation.
- **Return value**: A `Task<object?>` that resolves to the response object, or `null` if the handler returns no value.
- **Throws**:
  - `ArgumentNullException`: If `eventData` is `null`.
  - `InvalidOperationException`: If the invoker cannot handle the type of `eventData` (i.e., `CanHandle` returns `false` for the event type).
  - `OperationCanceledException`: If the operation is canceled via the cancellation token.
  - Any exception thrown by the underlying handler.

### `CanHandle`

```csharp
public bool CanHandle(Type eventType)
```

Determines whether the invoker can handle events of the specified type.

- **Parameters**:
  - `eventType`: The `Type` of the event to check.
- **Return value**: `true` if the invoker can handle events of the given type; otherwise, `false`.
- **Throws**:
  - `ArgumentNullException`: If `eventType` is `null`.

### `GetSupportedEventTypes`

```csharp
public IEnumerable<Type> GetSupportedEventTypes()
```

Returns a collection of event types that this invoker is capable of handling.

- **Parameters**: None.
- **Return value**: An `IEnumerable<Type>` containing the supported event types. The collection may be empty if no types are registered.
- **Throws**: None.

## Usage

### Example 1: Fire-and-forget event invocation

```csharp
public class OrderPlacedEvent
{
    public int OrderId { get; set; }
}

// Assume invoker is obtained from a DI container or event bus
IHandlerInvoker invoker = GetInvokerForEvent<OrderPlacedEvent>();

if (invoker.CanHandle(typeof(OrderPlacedEvent)))
{
    var orderEvent = new OrderPlacedEvent { OrderId = 42 };
    await invoker.InvokeAsync(orderEvent);
}
else
{
    Console.WriteLine("No handler registered for OrderPlacedEvent.");
}
```

### Example 2: Request-response event invocation with cancellation

```csharp
public class GetUserQuery
{
    public int UserId { get; set; }
}

public class UserResponse
{
    public string Name { get; set; }
}

IHandlerInvoker invoker = GetInvokerForEvent<GetUserQuery>();

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

if (invoker.CanHandle(typeof(GetUserQuery)))
{
    var query = new GetUserQuery { UserId = 1 };
    object? result = await invoker.InvokeRequestAsync(query, cts.Token);
    
    if (result is UserResponse user)
    {
        Console.WriteLine($"User name: {user.Name}");
    }
}
else
{
    Console.WriteLine("No handler registered for GetUserQuery.");
}
```

## Notes

- **Thread safety**: Implementations of `IHandlerInvoker` are expected to be thread-safe for concurrent calls to `InvokeAsync`, `InvokeRequestAsync`, `CanHandle`, and `GetSupportedEventTypes`. However, the underlying handler execution may introduce its own concurrency constraints. Consumers should not assume that handlers are reentrant unless explicitly documented.
- **Null event data**: Both `InvokeAsync` and `InvokeRequestAsync` throw `ArgumentNullException` when `eventData` is `null`. Always validate input before invocation.
- **Type checking**: Use `CanHandle` before invoking to avoid `InvalidOperationException`. The set of supported types is static for the lifetime of the invoker; `GetSupportedEventTypes` returns a snapshot that may be cached.
- **Cancellation**: Cancellation tokens are forwarded to the handler. If the handler does not support cancellation, the token may be ignored. The `Task` returned by `InvokeAsync` or `InvokeRequestAsync` will still complete normally unless the handler itself throws `OperationCanceledException`.
- **Empty supported types**: `GetSupportedEventTypes` may return an empty enumeration. In that case, `CanHandle` will always return `false`, and any invocation attempt will throw `InvalidOperationException`.
- **Inheritance**: The `HandlerInvoker` property returns the concrete implementation instance. This can be used to access implementation-specific features, but doing so couples the consumer to a particular implementation.
