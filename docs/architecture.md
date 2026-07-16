# DotnetEventBus Architecture

This document describes the actual structure of the code in `src/DotnetEventBus` - what the pieces are, how an event flows through them, and why some decisions were made the way they were.

## Overview

DotnetEventBus is a single-assembly, **in-process** event bus for .NET (`net10.0`). There is no broker and no network transport: publisher and subscribers live in the same process, and delivery is an async method call chain. The library provides:

- pub/sub with polymorphic dispatch (`PublishAsync`)
- request/reply over the same bus (`SendAsync` + `SubscribeRequest`)
- per-handler retry with exponential backoff, and a dead letter queue for handlers that exhaust retries
- a per-publish middleware pipeline (logging, error handling, rate limiting)
- pluggable persistence via repository interfaces (in-memory implementations ship in the box)

Repository layout:

```
src/DotnetEventBus/        the library (single project)
tests/DotnetEventBus.Tests xUnit test suite
benchmarks/                BenchmarkDotNet projects
examples/                  standalone usage samples
```

## Module map

The library is one project, organized by namespace/folder:

| Folder | What lives there |
|---|---|
| `Services/` | The core: `EventBus` (implements `IEventBus`), `DeadLetterService`, `SubscriptionManager`, `HandlerInvoker`, `BatchEventPublisher` |
| `Models/` | `EventMessage`, `EventEnvelope`, `Subscription`, `PublishResult`, `DeadLetterEntry` |
| `Handlers/` | `IEventHandler<TEvent>`, `HandlerBase`, predicate-filtered handlers and the `PredicateSubscriptionBuilder` fluent API |
| `Middleware/` | `IEventBusMiddleware`, `EventMiddlewareContext`, built-ins (`EventBusLoggingMiddleware`, `ErrorHandlingMiddleware`, `RateLimitingMiddleware`), plus a standalone `PipelineBuilder` |
| `Configuration/` | `EventBusOptions`, `ServiceCollectionExtensions` (DI wiring), `MiddlewareConfiguration`, routing config |
| `Repositories/` | `IRepository`-style abstractions (`IEventMessageRepository`, `ISubscriptionRepository`, `IDeadLetterRepository`) and their in-memory implementations |
| `Formatters/` | `IEventFormatter` with JSON (default), XML and CSV implementations plus `EventFormatterFactory` |
| `Workers/` | `DeadLetterProcessor`, a `BackgroundService` that periodically sweeps the DLQ |
| `Advanced/` | Opt-in building blocks: `SagaOrchestrator`, `EventSourcedAggregate`, `EventTransformer`, `EventFilter`, `MetricsCollector`, request/response helpers |
| `Integration/` | `CircuitBreaker`, `RetryPolicy`, `HttpEventPublisher`, `WebhookHandler` (HMAC-SHA256 signed) |
| `Monitoring/` | `HealthCheck` |
| `Caching/` | `IEventCache` / `InMemoryEventCache` |
| `Api/`, `Cli/` | Optional ASP.NET controller surface and a small CLI (publish/subscribe/query/stats commands) |
| `Utilities/`, `Performance/` | Extension helpers, `ValidationHelper`, `PerformanceProfiler` |

Everything outside `Services/`, `Models/`, `Handlers/`, `Middleware/`, `Configuration/`, `Repositories/`, `Formatters/` is optional; the core bus does not depend on it.

## The core: EventBus

`Services/EventBus.cs` is the heart. Its state:

- `Dictionary<string, List<Subscription>> _subscriptions` keyed by event type full name, guarded by a plain lock
- `Dictionary<string, TaskCompletionSource<object?>> _pendingRequests` for request/reply correlation
- a `SemaphoreSlim` sized by `EventBusOptions.MaxConcurrentHandlers` that caps parallel handler execution

### Publish flow

`PublishAsync<TEvent>(event, correlationId)` does, in order:

1. Wraps the event in an `EventMessage` (id, type name, serialized payload via `IEventFormatter`, correlation id, timestamp) and persists it through `IEventMessageRepository`.
2. Resolves applicable subscriptions **polymorphically**: it walks the event type, its base types, and all implemented interfaces, collecting active subscriptions in `Priority` (descending) order and de-duplicating by handler name - so a handler subscribed to `IOrderEvent` fires once for `OrderCreated` even if it also subscribed to the concrete type.
3. Builds the middleware pipeline for this publish: `EventBusOptions.MiddlewareTypes` is walked in reverse, each type resolved from `IServiceProvider`, and wrapped around a terminal delegate.
4. The terminal delegate invokes handlers - in parallel (bounded by the semaphore) when `AllowParallelHandling` is true, sequentially otherwise.
5. Each handler runs through `InvokeHandlerWithRetry`: up to `MaxRetryAttempts` retries with exponential backoff (`RetryDelay` * `RetryDelayMultiplier`^attempt, capped by `MaxRetryDelay`). When retries are exhausted, the failure is written to the dead letter queue via `IDeadLetterService` (if `EnableDeadLetterQueue`).
6. A `PublishResult` is returned: per-handler success/failure lists, elapsed time, `Success = (FailedHandlers == 0)`. Handler failures do **not** throw at the publisher by default; set `ThrowOnHandlerFailure` to change that.

### Request/reply flow

`SendAsync<TRequest, TResponse>` publishes the request and parks a `TaskCompletionSource` in `_pendingRequests` keyed by correlation id; the responder registered via `SubscribeRequest<TRequest, TResponse>` produces the response, which completes the TCS. Timeout precedence is explicit: method parameter > `[RequestTimeout]` attribute on the request type > `EventBusOptions.RequestTimeout`. On timeout the caller gets a `TimeoutException` and the pending entry is removed.

### Subscription surface

All `Subscribe*` methods return `IDisposable`; disposing unregisters the handler. Three shapes are supported: class-based (`IEventHandler<TEvent>`), async delegate, and sync delegate (`SubscribeSync`, wrapped into async). `PredicateSubscriptionBuilder` layers filtered subscriptions on top ("handle `OrderCreated` where `Total > 100`") by wrapping the handler in a `PredicateFilteredHandler`.

## Dead letter pipeline

Three cooperating pieces:

- `IDeadLetterRepository` - storage for `DeadLetterEntry` records (original message, failing handler name, exception details, retry counters, status: Pending/Reviewed/Reprocessed/Archived).
- `DeadLetterService` - query/reprocess/review/archive/purge operations plus `DeadLetterStatistics`. Reprocessing re-publishes the original payload through the bus.
- `DeadLetterProcessor` (`BackgroundService`) - optional periodic sweep that retries pending entries automatically.

`DeadLetterService` needs the bus to reprocess, and the bus needs the service to record failures - a genuine cycle. It is broken in DI by giving `DeadLetterService` an `IServiceProvider` and resolving `IEventBus` lazily on first reprocess, instead of a constructor dependency. The comments in `ServiceCollectionExtensions` document this; changing it back to a direct constructor dependency deadlocks singleton resolution on first use.

## DI composition

`AddEventBus()` (in `Configuration/ServiceCollectionExtensions`) registers everything as singletons: options, the three in-memory repositories, `JsonEventFormatter` as the default `IEventFormatter`, `DeadLetterService`, `SubscriptionManager`, `HandlerInvoker`, and `EventBus` itself via factory lambdas. A second overload accepts caller-supplied repository instances for custom persistence.

Middleware registered through `options.UseMiddleware<T>()` is resolved from the container at publish time, so the concrete middleware types are auto-registered as transient services by `AddEventBus` (a `TryAddTransient` per type in `MiddlewareTypes`). `AddEventBusMiddleware<T>()` remains available for registering middleware with custom lifetimes or constructor dependencies before calling `AddEventBus`.

`EventBus` also has a "loose" constructor where every dependency except `IServiceProvider` is optional and falls back to private in-memory instances. That exists for tests and no-DI usage; the trade-off (documented in the constructor) is that fallback repositories are per-instance, so dead letters written there are invisible to any separately constructed `DeadLetterService`. When composing manually, pass the same repository instances everywhere.

## Key design decisions and trade-offs

**In-process only.** No broker means no serialization boundary to cross for delivery, no infrastructure, microsecond-level latency - and no durability or cross-process fan-out. `EventBusOptions` has `IsDistributed` / `DistributedTransportType` placeholders, but no transport is implemented; setting them changes nothing today.

**Events are serialized anyway.** Every publish serializes the event into `EventMessage` via `IEventFormatter` even though handlers receive the live object. That costs CPU per publish, but buys three things: an auditable message store, dead letter entries that can be reprocessed after a restart (with a persistent repository), and payloads that are transport-ready if a distributed backend is added later.

**Per-publish pipeline construction.** The middleware chain is composed on every publish from `MiddlewareTypes`, resolving instances from DI each time. That is an allocation per publish, but it honors transient middleware lifetimes and lets middleware take scoped dependencies; a cached chain would silently freeze the first resolved instances.

**Dual bookkeeping for subscriptions.** Active dispatch uses the in-memory dictionary (fast, lock-guarded); `ISubscriptionRepository` records subscription metadata for the management/statistics surface (`SubscriptionManager`). The dictionary is the source of truth for delivery - the repository is observational.

**Failures are data, not exceptions.** `PublishAsync` reports per-handler outcomes in `PublishResult` instead of throwing, because with multiple independent handlers a single exception cannot represent "2 of 5 failed". One handler's failure never prevents the others from running.

**Coarse locking.** Subscription reads/writes use one lock. Publishes only hold it while snapshotting the subscription list, not during handler execution, so contention is limited to registration churn. A `ConcurrentDictionary` would not remove the lock anyway: the polymorphic lookup reads several keys (type, base types, interfaces) and needs a consistent view across them.

## Extension points

- `IEventHandler<TEvent>` / `HandlerBase` - custom handlers
- `IEventBusMiddleware` - cross-cutting behavior around handler invocation (see the three built-ins for reference implementations)
- `IEventMessageRepository`, `ISubscriptionRepository`, `IDeadLetterRepository` - swap in SQL/Redis/Mongo persistence; the second `AddEventBus` overload takes them directly
- `IEventFormatter` - custom wire formats (JSON, XML, CSV provided)
- `EventFilter` / predicate subscriptions - selective delivery
- `IEventCache` - caching layer for read paths

## Known limitations

- **Not distributed.** The `IsDistributed` options are inert placeholders.
- **`Middleware/PipelineBuilder` is vestigial.** `EventBus` builds its pipeline directly from `EventBusOptions.MiddlewareTypes`; the standalone `PipelineBuilder` composes a chain around a no-op terminal delegate and is not used by the bus itself. Prefer `options.UseMiddleware<T>()`.
- **In-memory defaults lose everything on restart** - messages, subscriptions, and dead letters. Persistence requires custom repository implementations.
- **Request/reply is single-responder.** Multiple `SubscribeRequest` registrations for the same request type race; the first response wins the correlation slot.
- **No ordering guarantee across handlers in parallel mode.** With `AllowParallelHandling` (the default), handler priority controls start order, not completion order.
