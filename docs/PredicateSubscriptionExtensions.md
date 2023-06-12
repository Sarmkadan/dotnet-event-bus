# PredicateSubscriptionExtensions

Provides extension methods for `IEventBus` that enable subscriptions filtered by a predicate, allowing handlers to receive only those events that satisfy a user‑defined condition.

## API

### SubscribeWhere<TEvent>(this IEventBus bus, Func<TEvent, bool> predicate, Action<TEvent> handler)

**Purpose**  
Registers a synchronous handler for events of type `TEvent` that match the supplied predicate.

**Parameters**  
- `bus`: The event bus instance to subscribe on.  
- `predicate`: A function that returns `true` for events that should be delivered to the handler.  
- `handler`: The action to invoke when a matching event is published.

**Return value**  
An `IDisposable` that, when disposed, unsubscribes the handler from the bus.

**Exceptions**  
- `ArgumentNullException` if `bus`, `predicate`, or `handler` is `null`.

### SubscribeWhere<TEvent>(this IEventBus bus, Func<TEvent, bool> predicate, Func<TEvent, Task> handler)

**Purpose**  
Registers an asynchronous handler for events of type `TEvent` that satisfy the predicate.

**Parameters**  
- `bus`: The event bus instance to subscribe on.  
- `predicate`: A function that returns `true` for events that should be delivered to the handler.  
- `handler`: An asynchronous action invoked for each matching event.

**Return value**  
An `IDisposable` that disposes the subscription.

**Exceptions**  
- `ArgumentNullException` if `bus`, `predicate`, or `handler` is `null`.

### SubscribeWithFilter<TEvent>(this IEventBus bus, Expression<Func<TEvent, bool>> filter, Action<TEvent> handler)

**Purpose**  
Registers a handler using an expression tree filter, which can be inspected or serialized before execution.

**Parameters**  
- `bus`: The event bus instance to subscribe on.  
- `filter`: An expression representing the predicate to evaluate for each event.  
- `handler`: The action to invoke for events where the filter evaluates to `true`.

**Return value**  
An `IDisposable` that removes the subscription when disposed.

**Exceptions**  
- `ArgumentNullException` if `bus`, `filter`, or `handler` is `null`.  
- `InvalidOperationException` if the expression cannot be compiled to a delegate.

### CreatePredicateSubscription<TEvent>(this IEventBus bus)

**Purpose**  
Begins a fluent configuration for a predicate‑based subscription, allowing further specification of the predicate and handler before registration.

**Parameters**  
- `bus`: The event bus instance to subscribe on.

**Return value**  
A `PredicateSubscriptionBuilder<TEvent>` instance used to define the predicate, handler, and ultimately call `Subscribe()` to obtain an `IDisposable`.

**Exceptions**  
- `ArgumentNullException` if `bus` is `null`.

## Usage

```csharp
// Example 1: Simple synchronous predicate subscription
var bus = new InMemoryEventBus();
IDisposable subscription = bus.SubscribeWhere<OrderCreated>(
    e => e.Amount > 1000,
    e => Console.WriteLine($"Large order: {e.OrderId}"));
// Later, to stop receiving events:
subscription.Dispose();
```

```csharp
// Example 2: Fluent builder with asynchronous handler
var bus = new InMemoryEventBus();
IDisposable subscription = bus
    .CreatePredicateSubscription<MessageReceived>()
    .WithPredicate(m => !m.IsSystem)
    .WithHandlerAsync(async m =>
    {
        await ProcessMessageAsync(m);
        await Task.Delay(10);
    })
    .Subscribe();

// Dispose when no longer needed
subscription.Dispose();
```

## Notes

- If the predicate throws an exception during evaluation, the exception is propagated to the caller of the publish operation; the handler is not invoked for that event.  
- The returned `IDisposable` is safe to dispose from any thread, but it is recommended to dispose on the same thread that created the subscription to avoid race conditions with internal bus state.  
- Multiple subscriptions with identical or overlapping predicates are allowed; each receives its own copy of the event when the predicate matches.  
- The extension methods themselves are stateless and thread‑safe; thread‑safety of the underlying subscription logic depends on the implementation of `IEventBus`.  
- When using `SubscribeWithFilter`, the expression tree is compiled each time an event is evaluated; consider caching the compiled delegate if the same filter is reused frequently.
