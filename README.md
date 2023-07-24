// ... (rest of the README.md content remains the same)

## PredicateSubscriptionBuilder

The `PredicateSubscriptionBuilder<TEvent>` class is a fluent builder for constructing predicate-filtered subscriptions on an <see cref="IEventBus"/>. It allows you to compose multiple conditions using AND semantics, ensuring that only events that match all specified criteria are processed by the registered handler.

Example usage:
```csharp
var eventBus = new EventBusBuilder()
    .Build();

var subscription = eventBus.CreatePredicateSubscription<OrderCreatedEvent>()
    .Where(e => e.TotalAmount > 100)
    .WhereNot(e => e.IsCancelled)
    .WithHandlerName("HighValueOrderHandler")
    .WithHandler(HandleHighValueOrderAsync)
    .WithPriority(10)
    .Register();

// Dispose the subscription when no longer needed
subscription.Dispose();
```
```