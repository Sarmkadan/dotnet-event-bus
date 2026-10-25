# Configuration

The `DotnetEventBus.Configuration` namespace (`src/DotnetEventBus/Configuration/`) holds everything that controls how the bus is set up:

| Type | Kind | Purpose |
|------|------|---------|
| [`EventBusOptions`](#eventbusoptions) | class | Runtime settings: retries, concurrency, dead-letter behaviour, middleware list |
| [`MiddlewareConfiguration`](#middlewareconfiguration) | static class | Extension methods that add middleware to `EventBusOptions` and the DI container |
| [`ServiceCollectionExtensions`](#servicecollectionextensions) | static class | `AddEventBus`, transport registration and transport registry setup |
| [`EventRoutingConfiguration`](#eventroutingconfiguration) | class + builder | Per-event-type routing rules. **Not consumed by the bus; see the note in that section.** |

The folder also contains `RabbitMqOptions` and `RabbitMqOptionsValidator`, which back the `AddEventBus(RabbitMqOptions?)` overload described below, and `EventBusRegistration` (internal), which records which bus kind was registered.

A typical setup:

```csharp
services.AddEventBus(options =>
{
    options.MaxRetryAttempts = 5;
    options.RetryDelay = TimeSpan.FromMilliseconds(200);
    options.EnableDeadLetterQueue = true;
    options.UseMiddleware<LoggingMiddleware>();
});

services.AddEventBusMiddleware<LoggingMiddleware>(); // optional: pick a lifetime explicitly
```

---

## EventBusOptions

`public sealed class EventBusOptions` (namespace `DotnetEventBus.Configuration`)

Mutable settings object. `AddEventBus` creates one instance, passes it to your `configureOptions` callback, calls `Validate()`, then registers it as a singleton. The bus reads the same instance at runtime.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DefaultHandlerTimeout` | `TimeSpan` | 30 s | Default timeout for handler execution. Not checked by `Validate()`. |
| `MaxRetryAttempts` | `int` | `3` | Maximum retries for a failed handler. Must be `>= 0`. |
| `RetryDelay` | `TimeSpan` | 100 ms | Base delay for exponential backoff. Must be `>= 0`. |
| `RetryDelayMultiplier` | `double` | `2.0` | Growth factor applied per attempt. Must be `>= 1.0`. |
| `MaxRetryDelay` | `TimeSpan` | 30 s | Upper bound for any single retry delay. |
| `AllowParallelHandling` | `bool` | `true` | Whether handlers for one event may run in parallel. |
| `MaxConcurrentHandlers` | `int` | `Environment.ProcessorCount` | Maximum handlers running at once. Must be `>= 1`. |
| `EnableDeadLetterQueue` | `bool` | `true` | Send failed messages to the dead-letter queue. |
| `ThrowOnHandlerFailure` | `bool` | `false` | Rethrow handler exceptions instead of logging them. |
| `ThrowOnNoHandlers` | `bool` | `false` | Throw `NoHandlersRegisteredException` when an event has no subscribers. |
| `DeadLetterOnNoHandlers` | `bool` | `false` | Record unhandled events in the dead-letter queue. Ignored if `EnableDeadLetterQueue` is `false` or `ThrowOnNoHandlers` is `true` (the throw takes precedence). |
| `MaxDeadLetterEntries` | `int` | `1000` | Ring-buffer size of the default in-memory dead-letter store; the oldest entry is evicted first. Must be `>= 1`. Has no effect on a custom `IDeadLetterRepository`. |
| `IsDistributed` | `bool` | `false` | Marks the bus as distributed. When `true`, `DistributedTransportType` is required. |
| `DistributedTransportType` | `string?` | `null` | Transport identifier for distributed messaging (for example `"RabbitMq"`). |
| `DistributedTransportConnectionString` | `string?` | `null` | Connection string for the distributed transport. |
| `MiddlewareTypes` | `List<Type>` | empty | Middleware types to run in the pipeline, in the order they were added. Get-only; populated through `UseMiddleware`. |
| `RequestTimeout` | `TimeSpan` | 30 s | Default timeout for request/reply when no per-request timeout is set. Must be `> 0`. |

### Methods

#### `Validate()`

Checks the cross-property rules and throws `DotnetEventBus.Exceptions.ValidationException` (derived from `EventBusException`) on the first failure:

- `RequestTimeout` must be greater than zero.
- `MaxRetryAttempts` must not be negative.
- `RetryDelay` must not be negative.
- `RetryDelayMultiplier` must be at least `1.0`.
- `MaxConcurrentHandlers` must be at least `1`.
- If `IsDistributed` is `true`, `DistributedTransportType` must not be empty or whitespace.
- `MaxDeadLetterEntries` must be at least `1`.

`AddEventBus` calls this for you. Call it yourself only if you build options outside DI.

#### `CalculateRetryDelay(int attemptNumber)`

Returns `RetryDelay * RetryDelayMultiplier^attemptNumber`, capped at `MaxRetryDelay`.

- `attemptNumber` starts at `0`, so attempt `0` returns `RetryDelay` unchanged.
- Throws `ArgumentException` if `attemptNumber` is negative.

With the defaults the delays are 100 ms, 200 ms, 400 ms, and so on, up to 30 s.

#### `Clone()`

Returns a new `EventBusOptions` with the same scalar values and a copied `MiddlewareTypes` list. Use it when you need a variant of an existing configuration without changing the original.

#### `ToString()`

Returns a short summary of six settings: `DefaultHandlerTimeout`, `MaxRetryAttempts`, `RetryDelay`, `RetryDelayMultiplier`, `MaxRetryDelay`, and `AllowParallelHandling`. It is meant for logging and does not list the other options.

---

## MiddlewareConfiguration

`public static class MiddlewareConfiguration`

Extension methods for registering pipeline middleware. Middleware types must implement `IEventBusMiddleware`.

### `UseMiddleware<TMiddleware>(this EventBusOptions options)`

Appends `TMiddleware` to `options.MiddlewareTypes`. Adding the same type twice has no effect. Returns `options` for chaining.

- Throws `ArgumentNullException` if `options` is `null`.
- The generic constraint already requires `IEventBusMiddleware`; the runtime check that follows is a defensive duplicate of it.

This only records the type. The instance is created from DI when the bus publishes, so the type must also be registered in the container. `AddEventBus` does this for you with `TryAddTransient`, so a registration you make yourself (for example with a different lifetime) is kept.

### `UseMiddlewareIf<TMiddleware>(this EventBusOptions options, Func<EventBusOptions, bool> predicate)`

Calls `predicate(options)` immediately. If it returns `true`, calls `UseMiddleware<TMiddleware>()`. Returns `options` for chaining.

- Throws `ArgumentNullException` if `options` or `predicate` is `null`.
- The predicate is evaluated once, at configuration time, not per publish.

```csharp
options.UseMiddlewareIf<MetricsMiddleware>(o => o.IsDistributed);
```

### `AddEventBusMiddleware<TMiddleware>(this IServiceCollection services)`

Registers `TMiddleware` as **transient** in the container with `AddTransient`. Use it when you want a lifetime other than the transient default that `AddEventBus` applies. Returns `services`.

- Throws `ArgumentNullException` if `services` is `null`.
- Unlike `AddEventBus`, this uses `AddTransient`, not `TryAdd`. Calling it twice for one type adds two registrations.

---

## ServiceCollectionExtensions

`public static class ServiceCollectionExtensions`

Entry points for registering the bus. Each method returns the `IServiceCollection` it was given.

Only one bus kind can be registered per container. Registering a different kind after one already exists throws `InvalidOperationException`. Registering the same kind again is allowed, which lets you call `AddInProcessTransport` and then `AddEventBus`.

### `AddEventBus(this IServiceCollection services, Action<EventBusOptions>? configureOptions = null)`

Registers the in-process bus with in-memory repositories.

Registers, among other services:

- `EventBusOptions` (singleton instance)
- `IEventBus` backed by `Services.EventBus`
- `IEventMessageRepository`, `ISubscriptionRepository`, and `IDeadLetterRepository` (in-memory). The dead-letter store is sized by `MaxDeadLetterEntries`.
- `IEventFormatter` (`JsonEventFormatter`), `IDeadLetterService`, `ISubscriptionManager`, `IHandlerInvoker`, and `ITransportRegistry`
- Every middleware type from `options.MiddlewareTypes`, via `TryAddTransient`

Throws:

- `ArgumentNullException` if `services` is `null`.
- `InvalidOperationException` if a different bus kind is already registered.
- `DotnetEventBus.Exceptions.ValidationException` if `Validate()` fails. Nothing is registered in that case.

```csharp
services.AddEventBus(options =>
{
    options.DefaultHandlerTimeout = TimeSpan.FromSeconds(10);
    options.MaxConcurrentHandlers = 8;
});
```

### `AddEventBus(this IServiceCollection services, IEventMessageRepository messageRepository, ISubscriptionRepository subscriptionRepository, IDeadLetterRepository deadLetterRepository, Action<EventBusOptions>? configureOptions = null)`

Same as above, but uses the repositories you supply. Use it for custom storage. The repositories are registered as singletons.

Throws `ArgumentNullException` if any argument is `null`, plus the same exceptions as the overload above.

`MaxDeadLetterEntries` is validated but does not apply to your dead-letter repository. Enforce its limits yourself.

### `AddEventBus(this IServiceCollection services, RabbitMqOptions? options)`

Validates RabbitMQ settings at registration time. **It never registers a working bus.** For valid options it throws `NotSupportedException`. The RabbitMQ transport is not implemented in this version. Use the in-process overloads for delivery.

Throws, in order:

1. `ArgumentNullException` if `services` is `null`.
2. `InvalidOperationException` if a different bus kind is already registered.
3. `InvalidOperationException` if `options` is `null` or `ConnectionString` is empty or whitespace.
4. `OptionsValidationException` if `RabbitMqOptionsValidator` reports failures. The message names each invalid property.
5. `NotSupportedException` when the options are valid. Nothing is registered at this point.

### `AddInProcessTransport(this IServiceCollection services, Action<EventBusOptions>? configureOptions = null)`

Calls `AddEventBus(configureOptions)`, then registers `IEventTransport` as an `InProcessTransport` that wraps the bus.

Throws `ArgumentNullException` if `services` is `null`.

### `AddWebhookTransport(this IServiceCollection services, string? signingSecret = null)`

Registers a `WebhookHandler` (constructed with `signingSecret`) and an `IEventTransport` that wraps it. It does **not** call `AddEventBus`, so add a bus separately if you need one.

Throws `ArgumentNullException` if `services` is `null`.

### `ConfigureTransportRegistry(this IServiceCollection services, params string[] transportIds)`

Registers an `ITransportRegistry` factory that:

1. Registers every `IEventTransport` in the container.
2. Marks as default the first transport whose `TransportId` matches an entry in `transportIds`, in the order given. Later entries are ignored once one matches.

The default in-process transport ID is `"in-process-transport"`.

This registration uses `AddSingleton`, not `TryAdd`. It is added after `AddEventBus`'s own `ITransportRegistry`, so the last registration wins when the container resolves a single `ITransportRegistry`.

Throws `ArgumentNullException` if `services` is `null`.

```csharp
services.AddInProcessTransport(o => o.MaxRetryAttempts = 2);
services.AddWebhookTransport(signingSecret: "change-me");
services.ConfigureTransportRegistry("in-process-transport");
```

---

## EventRoutingConfiguration

`public sealed class EventRoutingConfiguration : IEquatable<EventRoutingConfiguration>`

Holds routing rules keyed by event type. Each rule names a target handler, an optional metadata condition, a priority, and a `ContinueEvaluation` flag.

> **Note:** nothing in the library calls `ShouldRoute` or reads these rules during publish. `EventBusOptions` and `AddEventBus` do not accept a routing configuration. To use it, call `ShouldRoute` yourself, for example inside a handler or a middleware. Setting `Priority` or `ContinueEvaluation` does not change any result today, because `ShouldRoute` checks only the first rule whose `TargetHandler` matches.

### Methods

#### `AddRoute(string eventType, RoutingRule rule)`

Appends `rule` to the list for `eventType`. Throws `ArgumentNullException` if either argument is `null`.

#### `GetRoutes(string eventType)`

Returns the rules for `eventType` in insertion order, or an empty sequence if there are none.

#### `ShouldRoute(string eventType, string handlerName, Dictionary<string, object>? metadata = null)`

Decides whether `handlerName` should receive `eventType`:

1. If the event type has no rules, returns `true` (deliver to everyone).
2. Otherwise, finds the first rule whose `TargetHandler` equals `handlerName`. If none exists, returns `false`.
3. If that rule has a `Condition` and `metadata` is not `null`, returns the result of the condition.
4. Otherwise returns `true`. In particular, a condition is skipped when `metadata` is `null`.

#### `GetConfiguredEventTypes()`

Returns the event types that have at least one rule.

#### `Clear()`

Removes all rules.

#### `Equals`, `GetHashCode`, `==`, `!=`, `ToString()`

Value equality. Two configurations are equal when they have the same event types and, for each type, the same rules in the same order. Rules compare `TargetHandler` (ordinal), `Priority`, `ContinueEvaluation`, and `Condition` by delegate reference. `ToString()` gives a summary of the rules.

### `RoutingRule`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `TargetHandler` | `string` (required) | — | Handler name the rule applies to. |
| `Condition` | `Func<Dictionary<string, object>, bool>?` | `null` | Predicate over the event metadata. |
| `Priority` | `int` | `0` | Stored only; see the note above. |
| `ContinueEvaluation` | `bool` | `false` | Stored only; see the note above. |

### `EventRoutingBuilder`

Fluent wrapper that builds an `EventRoutingConfiguration`. Each method returns the same builder.

- `RouteEvent(string eventType, string handlerName)` routes the event to the handler with no condition.
- `RouteEventIf(string eventType, string handlerName, Func<Dictionary<string, object>, bool> condition, int priority = 0)` routes the event when `condition` returns `true`.
- `RouteByMetadata(string eventType, string handlerName, string metadataKey, object expectedValue)` routes the event when `metadata[metadataKey]` exists and equals `expectedValue` (using `object.Equals`).
- `Build()` returns the configuration the builder holds. It is the same instance on every call, so later builder calls change the object you already received.

```csharp
var routing = new EventRoutingBuilder()
    .RouteEvent("OrderCreated", "InventoryHandler")
    .RouteByMetadata("OrderCreated", "EmailHandler", "channel", "email")
    .Build();

bool sendEmail = routing.ShouldRoute("OrderCreated", "EmailHandler",
    new Dictionary<string, object> { ["channel"] = "email" }); // true
```

---

## See also

- [EventBusOptions](EventBusOptions.md) — per-property reference. Note: that page's `RetryDelay` default (1 s) does not match the code (100 ms); the table above follows the source.
- [EventRoutingConfiguration](EventRoutingConfiguration.md)
- [Middleware](Middleware.md)
- [EventBusBuilder](EventBusBuilder.md)
