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

## HttpEventPublisherExtensions

The `HttpEventPublisherExtensions` class provides a set of extension methods for `HttpEventPublisher` that enable fluent and convenient APIs for publishing events via HTTP. These extensions simplify common publishing scenarios including custom headers, content types, batch operations, and detailed result handling.

Here's an example usage:

```csharp
// Create an HttpEventPublisher instance
var publisher = new HttpEventPublisher(httpClient);

// Publish an event with custom headers
var result = await publisher.PublishAsync(
    "https://api.example.com/events",
    new { EventType = "OrderCreated", OrderId = 12345 },
    headers => headers.Add("X-API-Key", "secret-key-123")
);

// Publish an event with a custom content type
var xmlResult = await publisher.PublishAsync(
    "https://api.example.com/events",
    new { EventType = "UserRegistered", UserId = 67890 },
    "application/xml"
);

// Publish an event and check if it was successful (2xx status code)
var success = await publisher.PublishSuccessfullyAsync(
    "https://api.example.com/events",
    new { EventType = "PaymentProcessed", Amount = 99.99m }
);

// Publish an event and get detailed response information
var (success, statusCode, errorMessage) = await publisher.PublishWithDetailsAsync(
    "https://api.example.com/events",
    new { EventType = "InventoryUpdated", ProductId = 456 }
);

// Publish multiple events in batch
var batchResults = await publisher.PublishBatchAsync(new List<(string Url, object Data, Dictionary<string, string>? Headers)>
{
    ("https://api1.example.com/events", new { EventType = "OrderShipped", OrderId = 1 }, new Dictionary<string, string> { { "X-Source", "SystemA" } }),
    ("https://api2.example.com/events", new { EventType = "OrderShipped", OrderId = 2 }, null),
    ("https://api3.example.com/events", new { EventType = "OrderShipped", OrderId = 3 }, new Dictionary<string, string> { { "X-Source", "SystemB" } })
});

// Publish the same event to multiple endpoints
var multiResults = await publisher.PublishToMultipleAsync(
    new List<string> { "https://api1.example.com/events", "https://api2.example.com/events" },
    new { EventType = "SystemAlert", Message = "High memory usage detected" }
);

// Publish an event and check if the error message contains specific text
var hasError = await publisher.PublishWithErrorContainingAsync(
    "https://api.example.com/events",
    new { EventType = "InvalidRequest", Data = "corrupted" },
    "validation failed"
);
```

These extension methods provide a clean and expressive API for common HTTP event publishing scenarios, reducing boilerplate code and improving readability.
// ... (rest of the README.md content remains the same)
