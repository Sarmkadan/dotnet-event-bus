# Middleware

The `DotnetEventBus.Middleware` namespace contains the building blocks for wrapping event processing in cross-cutting behavior: logging, retries, and rate limiting. It has two composition models:

1. **Standalone pipeline** (`PipelineBuilder`, `EventBusMiddleware`, `EventContext`). You compose steps yourself and run the resulting delegate. The event bus does **not** use this model.
2. **Bus pipeline** (`IEventBusMiddleware`, `EventMiddlewareContext`, `EventMiddlewareDelegate`). Types are registered with `EventBusOptions.UseMiddleware<T>()` and the bus runs them around handler invocation. See [EventBus](EventBus.md) for registration.

This page covers the standalone model and the three built-in components, which are written against it. The built-in classes are not `IEventBusMiddleware` implementations; each exposes a `Create(EventBusMiddleware next)` factory that plugs into `PipelineBuilder.Use`.

## Components

| File | Type | Role |
|------|------|------|
| `PipelineBuilder.cs` | `PipelineBuilder`, `EventBusMiddleware` (delegate), `EventContext` | Registers step factories, composes them with `Build()`, and defines the step delegate and the context passed through the chain |
| `IEventBusMiddleware.cs` | `IEventBusMiddleware`, `EventMiddlewareContext`, `EventMiddlewareDelegate` | Bus pipeline contract (see the bus pipeline section below) |
| `EventBusLoggingMiddleware.cs` | `EventBusLoggingMiddleware` | Logs publish start, payload, duration, and outcome; assigns a correlation ID when missing |
| `ErrorHandlingMiddleware.cs` | `ErrorHandlingMiddleware`, `EventProcessingException` | Retries the rest of the chain on failure and optionally runs a recovery callback |
| `RateLimitingMiddleware.cs` | `RateLimitingMiddleware`, `RateLimitExceededException` | Sliding-window limit per event type |
| `RateLimitingMiddlewareValidation.cs` | `RateLimitingMiddlewareValidation` | Validation helpers for `RateLimitingMiddleware` instances |

## Standalone pipeline

### Core types

- `delegate Task EventBusMiddleware(EventContext context)` — one pipeline step. A step either calls `next(context)` to continue, or returns without calling it to short-circuit the chain.
- `PipelineBuilder`
  - `PipelineBuilder Use(Func<EventBusMiddleware, EventBusMiddleware> middleware)` — registers a step factory. The factory receives the next step and returns the step it wraps. Throws `ArgumentNullException` for `null`. Returns the builder.
  - `EventBusMiddleware Build()` — composes the registered factories and returns the outermost step. The first registered factory runs first.
  - `void Clear()` — removes all registered factories.
- `EventContext` — the data passed through the chain:

| Member | Type | Notes |
|--------|------|-------|
| `EventType` | `string` (`required`) | Event type name. Used as the rate-limit bucket key. |
| `EventData` | `object` (`required`) | Event payload. |
| `Metadata` | `Dictionary<string, object>` | Empty by default. Steps write annotations here. |
| `CreatedAt` | `DateTime` | UTC, set on construction. |
| `CorrelationId` | `string?` | Set by the logging middleware if absent. |
| `IsProcessed` | `bool` | Set to `true` on success and `false` on failure by the built-in middleware. |
| `ProcessingException` | `Exception?` | Last exception seen by the built-in middleware. |

### Composition rules

- **Order.** Steps run in registration order. The first `Use` call is the outermost step, so it sees the event first and sees the result last.
- **Terminal step.** The innermost step is a no-op that completes immediately. `PipelineBuilder` does not invoke subscribers or handlers. Only your steps do work.
- **Short-circuit.** A step that does not call `next` ends the chain. Steps after it do not run.
- **Snapshot.** `Build()` produces a delegate that does not depend on later `Use` or `Clear` calls. Build once at startup and reuse the delegate.
- **Factories run per execution.** `Build()` does not call your factories. The composed delegate calls each factory every time the pipeline runs. Keep factories cheap, and keep shared state in fields or captured variables.
- **Thread safety.** `Use` and `Clear` are not thread-safe. Do not call them concurrently, and do not call them while another thread is building.

### Example: custom steps

```csharp
using DotnetEventBus.Middleware;

EventBusMiddleware pipeline = new PipelineBuilder()
    .Use(next => async context =>
    {
        Console.WriteLine($"Before {context.EventType}");
        await next(context);
        Console.WriteLine($"After {context.EventType}");
    })
    .Use(next => context =>
    {
        context.Metadata["processedBy"] = "audit";
        return next(context);
    })
    .Build();

await pipeline(new EventContext
{
    EventType = "OrderCreated",
    EventData = new { OrderId = 42 }
});
```

## Built-in components

Each built-in component is created with its dependencies and then registered through its `Create` method. The `Create` method is called once per pipeline execution by `PipelineBuilder`, so the returned step captures the component instance. All state (rate-limit buckets, logger, settings) lives on that instance.

### EventBusLoggingMiddleware

```csharp
public EventBusLoggingMiddleware(
    ILogger<EventBusLoggingMiddleware> logger,
    LogLevel logLevel = LogLevel.Information,
    bool logEventPayload = true)

public EventBusMiddleware Create(EventBusMiddleware next)
```

Behavior for each event:

- If `context.CorrelationId` is `null`, it assigns a new GUID and writes it back to the context. Existing values are kept.
- Logs `Event published: {EventType} [CorrelationId: ...]` at `logLevel`.
- If `logEventPayload` is `true` and `EventData` is not `null`, serializes the payload with `System.Text.Json` and logs it at `logLevel`.
- Calls `next(context)`. On success it sets `IsProcessed = true` and logs the elapsed time at `logLevel`.
- On an exception it sets `ProcessingException` and `IsProcessed = false`, logs at `Error`, and **rethrows**. It never swallows exceptions.

Notes:

- Payload serialization runs inside the same `try` block. If `EventData` cannot be serialized (for example, it contains a cycle or a delegate), the event fails even though the handlers would have worked. Set `logEventPayload: false` for such payloads.
- The payload is logged in full. Disable payload logging for data that must not reach log storage.

### ErrorHandlingMiddleware

```csharp
public ErrorHandlingMiddleware(
    ILogger<ErrorHandlingMiddleware> logger,
    int maxRetries = 3,
    TimeSpan? retryDelay = null,
    Func<EventContext, Exception, Task<bool>>? errorHandler = null)

public EventBusMiddleware Create(EventBusMiddleware next)
```

Behavior for each event:

1. Calls `next(context)` up to `maxRetries` times. Defaults: 3 attempts, 1 second delay.
2. Each failed attempt sets `context.ProcessingException` and `context.Metadata["attempt"]` (the attempt number, as `int`), logs a warning, and waits `retryDelay` before the next attempt. The delay is not applied after the last attempt.
3. On success it sets `IsProcessed = true` and returns.
4. After the last failure, if `errorHandler` is set, it calls `errorHandler(context, lastException)`.
   - If it returns `true`, the event counts as recovered: `IsProcessed = true`, `Metadata["recoveredByHandler"] = true`, and the step returns normally.
   - If it returns `false`, processing continues to step 5.
5. Sets `IsProcessed = false`, logs at `Error`, and throws `EventProcessingException` with the last exception as `InnerException`.

Notes:

- Retries use a fixed delay with no backoff and no jitter.
- Every exception is retried, including `OperationCanceledException`. The retry delay does not observe `context.CancellationToken`, so cancellation does not stop retries early.
- `maxRetries` is not validated. A value of `0` or less never calls `next`, does not throw, and leaves `IsProcessed = false`. The event is dropped silently. Use a value of at least `1`.
- The step retries the whole rest of the chain. Place it so that only the work you want retried sits inside it (see recommended order below).

### RateLimitingMiddleware

```csharp
public RateLimitingMiddleware(
    ILogger<RateLimitingMiddleware> logger,
    int requestsPerWindow = 1000,
    TimeSpan? timeWindow = null)

public EventBusMiddleware Create(EventBusMiddleware next)
```

Behavior for each event:

- Looks up the bucket for `context.EventType`, creating it on first use. Each bucket is a queue of timestamps.
- Drops timestamps older than the window, then checks whether the count is below `requestsPerWindow`.
- If the quota is used up, logs a warning and throws `RateLimitExceededException` **without calling `next`**. Rejected events are not recorded.
- Otherwise records the current timestamp and calls `next(context)`.

Defaults: 1000 events per 60 seconds, per event type.

Notes:

- The window is sliding. Each accepted event occupies one slot until it is older than `timeWindow`.
- Buckets are keyed by the exact `EventType` string (case-sensitive) and are never removed. Memory grows with the number of distinct event type names.
- The quota check and the timestamp recording use two separate lock acquisitions. Concurrent calls for the same type can pass the check together, so the limit can be exceeded by up to the number of concurrent callers.
- `requestsPerWindow` and `timeWindow` are not validated. `requestsPerWindow` of `0` or less rejects every event.
- `RateLimitExceededException` is thrown as the faulted result of the pipeline call. The caller must catch it. Its message has the form `Rate limit exceeded for event type: {EventType}`.
- Logging is `Information` when each check starts and finishes, and `Warning` when an event is rejected.

## Composing a pipeline

Use `Use` with a `Create` method group for each built-in component. Register components in the order listed below.

```csharp
using DotnetEventBus.Middleware;
using Microsoft.Extensions.Logging;

var logging = new EventBusLoggingMiddleware(
    loggerFactory.CreateLogger<EventBusLoggingMiddleware>());

var rateLimiter = new RateLimitingMiddleware(
    loggerFactory.CreateLogger<RateLimitingMiddleware>(),
    requestsPerWindow: 100,
    timeWindow: TimeSpan.FromSeconds(10));

var errorHandling = new ErrorHandlingMiddleware(
    loggerFactory.CreateLogger<ErrorHandlingMiddleware>(),
    maxRetries: 3,
    retryDelay: TimeSpan.FromMilliseconds(200));

EventBusMiddleware pipeline = new PipelineBuilder()
    .Use(logging.Create)
    .Use(rateLimiter.Create)
    .Use(errorHandling.Create)
    .Build();

try
{
    await pipeline(new EventContext
    {
        EventType = "OrderCreated",
        EventData = new { OrderId = 42 }
    });
}
catch (RateLimitExceededException)
{
    // Quota exhausted for this event type: back off, or map to HTTP 429.
}
catch (EventProcessingException ex)
{
    // All retries failed. ex.InnerException holds the last failure.
}
```

### Recommended order

The order above is the recommended one for these reasons:

- **Logging outermost.** It sees every outcome, including rate-limit rejections and final failures.
- **Rate limiting before error handling.** A rejected event throws `RateLimitExceededException`. If error handling sat outside the limiter, it would treat that rejection as a failure and retry it, adding delays and warnings for events that were never attempted.
- **Error handling innermost (among the built-ins).** Retries re-run only the steps inside it. The rate-limit check runs once per publish, not once per retry.

Custom steps follow the built-ins. Put steps that should be retried inside `ErrorHandlingMiddleware`, and steps that must run only once per publish outside it.

## Notes

- The standalone pipeline is independent of the bus. Adding steps here does not change how `EventBus` publishes events. For bus-level middleware, use `EventBusOptions.UseMiddleware<T>()` with an `IEventBusMiddleware` implementation.
- `PipelineBuilder`'s innermost step is a no-op. To run handlers from this pipeline, your own final step must do that work.
