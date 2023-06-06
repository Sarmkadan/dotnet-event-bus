# EventMiddlewareContext

Provides contextual information to middleware components in the event‑bus pipeline, allowing them to inspect the published event, access the underlying message metadata, and propagate cancellation signals.

## API

### Constructor

```csharp
public EventMiddlewareContext(
    object @event,
    Type eventType,
    string? correlationId,
    EventMessage eventMessage,
    CancellationToken cancellationToken)
```

- **Purpose** – Creates a new instance that wraps the data needed by a middleware delegate.
- **Parameters**
  - `@event`: The payload object published to the bus.
  - `eventType`: The CLR type of `@event`.
  - `correlationId`: An optional identifier used to trace related messages across services; may be `null`.
  - `eventMessage`: The raw `EventMessage` envelope containing headers, timestamps, etc.
  - `cancellationToken`: A token that can be used to abort processing of the current pipeline.
- **Return value** – A fully initialized `EventMiddlewareContext`.
- **Exceptions** – Throws `ArgumentNullException` if `@event`, `eventType`, `eventMessage`, or `cancellationToken` is `null`.

### Properties

| Member | Type | Description |
|--------|------|-------------|
| `Event` | `object` | Gets the event payload. The value is the same object supplied to the constructor. |
| `EventType` | `Type` | Gets the CLR type of the event payload. |
| `CorrelationId` | `string?` | Gets the optional correlation identifier; may be `null` if none was supplied. |
| `EventMessage` | `EventMessage` | Gets the envelope that transported the event, including headers and metadata. |
| `CancellationToken` | `CancellationToken` | Gets the token that signals cancellation of the current middleware chain. |

### Delegate

```csharp
public delegate Task EventMiddlewareDelegate(EventMiddlewareContext context);
```

- **Purpose** – Defines the signature for a middleware component. Implementations receive an `EventMiddlewareContext`, perform any required logic (e.g., logging, validation, transformation), and either invoke the next delegate or short‑circuit the pipeline.
- **Parameters**
  - `context`: Provides access to the event and its surrounding metadata.
- **Return value** – A `Task` that completes when the middleware has finished its work.
- **Exceptions** – Any exception thrown by the delegate propagates outward and is handled by the pipeline’s error‑handling logic; the delegate itself does not declare specific exceptions.

## Usage

### Simple logging middleware

```csharp
public Task LogMiddleware(EventMiddlewareContext context, EventMiddlewareDelegate next)
{
    Console.WriteLine(
        $"Processing {context.EventType.Name} (CorrelationId: {context.CorrelationId ?? "none"})");

    // Continue down the pipeline
    return next(context);
}
```

### Validation middleware that short‑circuits on failure

```csharp
public Task ValidateMiddleware(EventMiddlewareContext context, EventMiddlewareDelegate next)
{
    if (context.Event is OrderCreated order && order.Amount <= 0)
    {
        // Fail fast – do not invoke the next delegate
        throw new InvalidOperationException("Order amount must be positive.");
    }

    return next(context);
}
```

## Notes

- The `EventMiddlewareContext` instance is immutable after construction; its properties cannot be changed, which makes it safe to share across asynchronous middleware stages.
- `CorrelationId` may be `null`; middleware should handle this case explicitly rather than assuming a value exists.
- Because the delegate returns a `Task`, middleware can perform asynchronous work (e.g., I/O, async validation) without blocking the pipeline.
- Throwing an exception from a middleware delegate aborts further processing; the pipeline’s error handling will capture the exception and may invoke registered error handlers.
- The `CancellationToken` is sourced from the bus’s invocation context; checking `IsCancellationRequested` permits early exit when the overall operation is cancelled. 
- No thread‑affinity is implied; the context can be used on any thread, but care must be taken not to mutate the underlying `EventMessage` or payload objects if they are shared elsewhere.
