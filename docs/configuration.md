# Configuration: EventBusBuilder

`EventBusBuilder` is the fluent entry point for configuring the event bus. You obtain one from `IServiceCollection`, call `With*` / `As*` methods to set options and repositories, and finish with `Build()`, which registers the event bus services in the same collection.

```csharp
services
    .AddEventBusBuilder()
    .WithMaxRetries(5)
    .WithHandlerTimeout(TimeSpan.FromSeconds(10))
    .WithParallelHandling(true)
    .WithMaxConcurrentHandlers(8)
    .WithDeadLetterQueue(true)
    .Build();
```

The builder is a `sealed` class in the `DotnetEventBus` namespace. Every `With*` / `As*` method returns the same builder instance, so calls chain. Nothing is registered until `Build()` is called.

## Obtaining a builder

```csharp
public static EventBusBuilder AddEventBusBuilder(this IServiceCollection services)
```

Extension method on `IServiceCollection`. Creates a new `EventBusBuilder` bound to `services`.

- Throws `ArgumentNullException` if `services` is `null`.

The constructor `new EventBusBuilder(IServiceCollection)` does the same thing and also throws `ArgumentNullException` for `null`.

## Methods

Defaults in the table below are the `EventBusOptions` defaults that apply when a method is not called.

| Method | Sets | Default | Validation (throws `ArgumentException`) |
|---|---|---|---|
| `WithOptions(Action<EventBusOptions>)` | Any options via a delegate | see `EventBusOptions` | `ArgumentNullException` if delegate is `null`. Option values are **not** validated here; see [Validation](#validation) |
| `WithMaxRetries(int)` | `MaxRetryAttempts` | `3` | Negative value |
| `WithHandlerTimeout(TimeSpan)` | `DefaultHandlerTimeout` | `30 s` | Zero or negative |
| `WithParallelHandling(bool)` | `AllowParallelHandling` | `true` | none |
| `WithMaxConcurrentHandlers(int)` | `MaxConcurrentHandlers` | `Environment.ProcessorCount` | Less than `1` |
| `WithDeadLetterQueue(bool)` | `EnableDeadLetterQueue` | `true` | none |
| `WithThrowOnHandlerFailure(bool)` | `ThrowOnHandlerFailure` | `false` | none |
| `WithThrowOnNoHandlers(bool)` | `ThrowOnNoHandlers` | `false` | none |
| `WithDeadLetterOnNoHandlers(bool)` | `DeadLetterOnNoHandlers` | `false` | none |
| `WithMessageRepository(IEventMessageRepository)` | Message storage | in-memory | `ArgumentNullException` if `null` |
| `WithSubscriptionRepository(ISubscriptionRepository)` | Subscription storage | in-memory | `ArgumentNullException` if `null` |
| `WithDeadLetterRepository(IDeadLetterRepository)` | Dead letter storage | in-memory | `ArgumentNullException` if `null` |
| `AsDistributed(string transportType, string? connectionString = null)` | `IsDistributed`, `DistributedTransportType`, `DistributedTransportConnectionString` | not distributed | `transportType` null, empty or whitespace |
| `Build()` | Registers services, returns `IServiceCollection` | - | Runs `EventBusOptions.Validate()`; see [Build](#build) |

### Retry and timeouts

- `WithMaxRetries(int maxAttempts)` - number of retry attempts after a handler fails. `0` disables retries.
- `WithHandlerTimeout(TimeSpan timeout)` - default timeout applied to each handler invocation.

Retry delay tuning (`RetryDelay`, `RetryDelayMultiplier`, `MaxRetryDelay`) has no dedicated builder method. Set it through `WithOptions`:

```csharp
services.AddEventBusBuilder()
    .WithOptions(o =>
    {
        o.RetryDelay = TimeSpan.FromMilliseconds(250);
        o.RetryDelayMultiplier = 2.0;   // must be >= 1.0
        o.MaxRetryDelay = TimeSpan.FromSeconds(10);
    })
    .Build();
```

### Concurrency

- `WithParallelHandling(bool enabled)` - when `true`, handlers for one event may run concurrently.
- `WithMaxConcurrentHandlers(int maxConcurrent)` - upper bound on concurrent handlers. Only meaningful when parallel handling is enabled.

### Failure behaviour

- `WithThrowOnHandlerFailure(bool)` - when `true`, a handler exception propagates to the publisher. When `false`, the failure is logged and handled by retry / dead letter logic.
- `WithThrowOnNoHandlers(bool)` - when `true`, publishing an event with no registered handler throws `NoHandlersRegisteredException`. When `false`, the event is logged (and optionally dead-lettered).
- `WithDeadLetterOnNoHandlers(bool)` - when `true`, events published with no handlers are recorded in the dead letter queue instead of being silently logged.
- `WithDeadLetterQueue(bool)` - enables or disables the dead letter queue for failed handler invocations.

`MaxDeadLetterEntries` (default `1000`) bounds the in-memory dead letter store. It has no dedicated builder method; set it through `WithOptions`.

### Repositories

Custom repositories are optional. If none is supplied, in-memory implementations are used.

```csharp
services.AddEventBusBuilder()
    .WithMessageRepository(new MyMessageRepository())
    .WithSubscriptionRepository(new MySubscriptionRepository())
    .WithDeadLetterRepository(new MyDeadLetterRepository())
    .Build();
```

If at least one repository is supplied, `Build()` fills the missing ones with in-memory implementations. The dead letter repository is created with `MaxDeadLetterEntries` as its capacity.

### Distributed mode

```csharp
services.AddEventBusBuilder()
    .AsDistributed("rabbitmq", "amqp://guest:guest@localhost:5672")
    .Build();
```

- `transportType` is stored as-is. It must be non-empty; the library does not check it against a list of known transports.
- `connectionString` is optional and stored as-is.
- Calling `AsDistributed` sets `IsDistributed = true`. Without it, the bus runs in-process only.

## Build

```csharp
public IServiceCollection Build()
```

`Build()`:

1. Calls `EventBusOptions.Validate()`. Invalid options throw before anything is registered.
2. Registers the event bus with `AddEventBus`, copying the current option values into it.
3. Returns the same `IServiceCollection`, so you can keep chaining registrations.

`Build()` does not return an `IEventBus`. Resolve `IEventBus` from the container after the service provider is built.

`Build()` uses `AddSingleton<IEventBus>` without `TryAdd`, so calling it more than once adds a duplicate registration (the last one wins for `IEventBus`). Call it exactly once per container.

### Known gaps in Build

- `MiddlewareTypes` on `EventBusOptions` is **not** copied to the registration. Middleware configured through `WithOptions` is ignored by `Build()`.
- `RequestTimeout` on `EventBusOptions` is **not** copied either. It is validated, but the value passed to `AddEventBus` uses its default.

If you rely on either setting, register them through the DI extensions directly (`services.AddEventBus(...)`) instead of `Build()`.

## Validation

`EventBusBuilderValidation` (namespace `DotnetEventBus`) adds extension methods that check the options before `Build()` is called:

| Method | Returns | Throws |
|---|---|---|
| `Validate()` | `IReadOnlyList<string>` of error messages (empty if valid) | `ArgumentNullException` |
| `IsValid()` | `bool` | `ArgumentNullException` |
| `EnsureValid()` | `void` | `ArgumentNullException`, `ArgumentException` with all error messages |

```csharp
var builder = services.AddEventBusBuilder().WithMaxRetries(2);

builder.EnsureValid();   // throws if any option is out of range
builder.Build();
```

Checks performed on `EventBusOptions`:

- `RequestTimeout` must be greater than zero.
- `MaxRetryAttempts` must not be negative.
- `RetryDelay` must not be negative.
- `RetryDelayMultiplier` must be at least `1.0`.
- `MaxConcurrentHandlers` must be at least `1`.
- When `IsDistributed` is `true`, `DistributedTransportType` must not be empty.

Note: `Validate()` reads the builder's private `_options` field through reflection. It works with the current code, but it is a known workaround and will need updating if the field is renamed.

## Full example

```csharp
using DotnetEventBus;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddEventBusBuilder()
    .WithMaxRetries(3)
    .WithHandlerTimeout(TimeSpan.FromSeconds(15))
    .WithParallelHandling(true)
    .WithMaxConcurrentHandlers(4)
    .WithDeadLetterQueue(true)
    .WithDeadLetterOnNoHandlers(true)
    .WithOptions(o => o.MaxDeadLetterEntries = 5000)
    .Build();

var provider = services.BuildServiceProvider();
var bus = provider.GetRequiredService<IEventBus>();
```

## See also

- [EventBusOptions](EventBusOptions.md)
- [EventBus](EventBus.md)
- [getting-started.md](getting-started.md)
- [migration-guide-v2.md](migration-guide-v2.md)
