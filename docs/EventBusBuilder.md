# EventBusBuilder

`EventBusBuilder` is the primary configuration and bootstrapping class for the `dotnet-event-bus` library. It provides a fluent API to register repositories, configure retry and timeout policies, enable parallel or distributed processing, and ultimately produce a fully configured `IServiceCollection` ready for dependency injection.

## API

### `public static EventBusBuilder AddEventBusBuilder(this IServiceCollection services)`

Extension method on `IServiceCollection` that creates and returns a new `EventBusBuilder` instance, registering core event bus infrastructure into the service collection.

- **Parameters**: `services` — the `IServiceCollection` to configure.
- **Returns**: A new `EventBusBuilder` instance bound to the given service collection.
- **Throws**: `ArgumentNullException` if `services` is `null`.

### `public EventBusBuilder WithOptions(Action<EventBusOptions> configure)`

Applies a configuration delegate to the underlying `EventBusOptions` object, allowing bulk setting of multiple options at once.

- **Parameters**: `configure` — an `Action<EventBusOptions>` that receives the options instance to modify.
- **Returns**: The same `EventBusBuilder` instance for chaining.
- **Throws**: `ArgumentNullException` if `configure` is `null`.

### `public EventBusBuilder WithMessageRepository<T>() where T : IMessageRepository`

Registers the specified type as the implementation of `IMessageRepository` used to persist and retrieve event messages.

- **Type Parameters**: `T` — a concrete type implementing `IMessageRepository`.
- **Returns**: The same `EventBusBuilder` instance for chaining.
- **Throws**: Nothing directly; invalid registrations may cause resolution failures at build time.

### `public EventBusBuilder WithSubscriptionRepository<T>() where T : ISubscriptionRepository`

Registers the specified type as the implementation of `ISubscriptionRepository` used to store and query event subscriptions.

- **Type Parameters**: `T` — a concrete type implementing `ISubscriptionRepository`.
- **Returns**: The same `EventBusBuilder` instance for chaining.
- **Throws**: Nothing directly; invalid registrations may cause resolution failures at build time.

### `public EventBusBuilder WithDeadLetterRepository<T>() where T : IDeadLetterRepository`

Registers the specified type as the implementation of `IDeadLetterRepository` used to persist messages that have exceeded retry limits or failed permanently.

- **Type Parameters**: `T` — a concrete type implementing `IDeadLetterRepository`.
- **Returns**: The same `EventBusBuilder` instance for chaining.
- **Throws**: Nothing directly; invalid registrations may cause resolution failures at build time.

### `public EventBusBuilder WithMaxRetries(int maxRetries)`

Sets the maximum number of delivery attempts for a message before it is moved to the dead-letter queue.

- **Parameters**: `maxRetries` — a non-negative integer. A value of zero means no retries.
- **Returns**: The same `EventBusBuilder` instance for chaining.
- **Throws**: `ArgumentOutOfRangeException` if `maxRetries` is negative.

### `public EventBusBuilder WithHandlerTimeout(TimeSpan timeout)`

Sets the maximum duration a single handler invocation is allowed to execute before being considered timed out.

- **Parameters**: `timeout` — a positive `TimeSpan`.
- **Returns**: The same `EventBusBuilder` instance for chaining.
- **Throws**: `ArgumentOutOfRangeException` if `timeout` is zero or negative.

### `public EventBusBuilder WithParallelHandling(bool enable = true)`

Enables or disables parallel execution of event handlers. When enabled, multiple handlers for the same event type may run concurrently.

- **Parameters**: `enable` — `true` to allow parallel handling; `false` to force sequential execution.
- **Returns**: The same `EventBusBuilder` instance for chaining.
- **Throws**: Nothing.

### `public EventBusBuilder WithMaxConcurrentHandlers(int maxConcurrency)`

Sets the maximum number of handlers that can execute concurrently when parallel handling is enabled.

- **Parameters**: `maxConcurrency` — a positive integer.
- **Returns**: The same `EventBusBuilder` instance for chaining.
- **Throws**: `ArgumentOutOfRangeException` if `maxConcurrency` is less than 1.

### `public EventBusBuilder WithDeadLetterQueue(bool enable = true)`

Enables or disables the dead-letter queue mechanism. When disabled, messages that exhaust retries are discarded rather than persisted.

- **Parameters**: `enable` — `true` to enable the dead-letter queue; `false` to disable it.
- **Returns**: The same `EventBusBuilder` instance for chaining.
- **Throws**: Nothing.

### `public EventBusBuilder WithThrowOnHandlerFailure(bool throwOnFailure = true)`

Controls whether handler exceptions are propagated immediately or captured and handled according to retry/dead-letter policies.

- **Parameters**: `throwOnFailure` — `true` to rethrow handler exceptions; `false` to suppress and route through retry logic.
- **Returns**: The same `EventBusBuilder` instance for chaining.
- **Throws**: Nothing.

### `public EventBusBuilder AsDistributed(Action<DistributedEventBusOptions>? configure = null)`

Configures the event bus for distributed (multi-node) operation, optionally applying additional distributed-specific options.

- **Parameters**: `configure` — an optional `Action<DistributedEventBusOptions>` delegate.
- **Returns**: The same `EventBusBuilder` instance for chaining.
- **Throws**: `ArgumentNullException` if `configure` is non-null and the underlying distributed infrastructure is not available.

### `public IServiceCollection Build()`

Finalizes configuration and returns the `IServiceCollection` with all event bus services registered and ready for provider building.

- **Returns**: The configured `IServiceCollection`.
- **Throws**: `InvalidOperationException` if required repositories have not been registered or if configuration is otherwise incomplete.

## Usage

### Example 1: Basic In-Process Event Bus

```csharp
var services = new ServiceCollection();

services.AddEventBusBuilder()
    .WithMessageRepository<InMemoryMessageRepository>()
    .WithSubscriptionRepository<InMemorySubscriptionRepository>()
    .WithMaxRetries(3)
    .WithHandlerTimeout(TimeSpan.FromSeconds(30))
    .WithParallelHandling(true)
    .WithMaxConcurrentHandlers(4)
    .Build();

var provider = services.BuildServiceProvider();
var eventBus = provider.GetRequiredService<IEventBus>();
```

### Example 2: Distributed Event Bus with Dead-Letter Queue

```csharp
var services = new ServiceCollection();

services.AddEventBusBuilder()
    .WithMessageRepository<SqlMessageRepository>()
    .WithSubscriptionRepository<SqlSubscriptionRepository>()
    .WithDeadLetterRepository<SqlDeadLetterRepository>()
    .WithMaxRetries(5)
    .WithHandlerTimeout(TimeSpan.FromMinutes(1))
    .WithDeadLetterQueue(true)
    .WithThrowOnHandlerFailure(false)
    .AsDistributed(opts => opts.WithBackplane("redis://localhost:6379"))
    .Build();

var provider = services.BuildServiceProvider();
var eventBus = provider.GetRequiredService<IEventBus>();
```

## Notes

- **Order of calls**: Configuration methods can be called in any order; all settings are applied at `Build()` time. Calling the same method multiple times overwrites the previous value.
- **Repository requirements**: `Build()` will throw `InvalidOperationException` if `WithMessageRepository` and `WithSubscriptionRepository` have not been called. `WithDeadLetterRepository` is required only when `WithDeadLetterQueue(true)` is set (the default).
- **Parallel handling constraints**: `WithMaxConcurrentHandlers` has no effect unless `WithParallelHandling` is enabled. If parallel handling is enabled but no maximum is specified, a library-defined default is used.
- **Throw-on-failure interaction**: When `WithThrowOnHandlerFailure(true)` is set, retry and dead-letter logic is bypassed — exceptions propagate directly to the caller. This is typically used in testing or scenarios requiring immediate failure visibility.
- **Thread safety**: `EventBusBuilder` is not designed to be thread-safe. Configuration should be performed on a single thread during application startup. Concurrent calls to builder methods from multiple threads produce undefined behavior.
- **Distributed mode**: Calling `AsDistributed` registers additional infrastructure for cross-node message delivery. The underlying transport (e.g., Redis, RabbitMQ) must be available at resolution time; otherwise, runtime failures will occur when the event bus is used.
