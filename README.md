// ... (rest of the README.md content remains the same)

## PredicateSubscriptionBuilderExtensions

The `PredicateSubscriptionBuilderExtensions` class provides a set of extension methods for `PredicateSubscriptionBuilder<TEvent>`, allowing for more convenient and expressive subscription configuration. These extensions enable features like conditional event filtering, property-based matching, and handler registration.

Here's an example usage:

```csharp
eventBus.Subscribe<OrderCreatedEvent>(
    builder => builder
        .Where(e => e.TotalAmount > 1000)
        .WhereNot(e => e.CustomerId.StartsWith("CANCELED"))
        .WhereTypeIs<OrderCreatedEvent, PromotionalOrderCreatedEvent>()
        .WherePropertyIn(e => e.Items, new[] { "Laptop", "Tablet" })
        .WherePropertyIsNotNull(e => e.PaymentMethod)
        .WherePropertyMatches(e => e.OrderId, "^ORD-\\d{3}$")
        .WherePropertyInRange(e => e.Items.Count, 1, 5)
        .WithHandler("OrderProcessor", async (e, ct) => await ProcessOrderAsync(e, ct))
        .WithLogger(loggerFactory.CreateLogger("OrderSubscription"))
);
```

These extensions provide a fluent and readable way to configure complex subscription rules and handler settings.
// ... (rest of the README.md content remains the same)
