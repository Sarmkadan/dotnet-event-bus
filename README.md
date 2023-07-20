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


## ValidationHelperExtensions

The `ValidationHelperExtensions` class provides a comprehensive set of extension methods for `ValidationHelper` that enable fluent and expressive validation scenarios. These extensions allow for validating strings, collections, numeric values, and complex patterns with custom error messages, supporting method chaining for clean and readable validation code.

Here's an example usage:

```csharp
var validationHelper = new ValidationHelper();

// Validate a string is not empty with custom error message
validationHelper
    .RequireNotEmpty(order.CustomerName, "CustomerName", "Customer name is required")
    .RequireMinLength(order.CustomerName, 3, "CustomerName")
    .RequireMaxLength(order.CustomerName, 100, "CustomerName");

// Validate a collection is not empty
validationHelper.RequireNotEmpty(order.Items, "Items");

// Validate a string matches a specific pattern (e.g., email)
validationHelper.RequirePattern(
    order.Email,
    "^[\\w-\\+]+(\\.\\w-\\+)*@[\\w-]+(\\.\\w-)*\\.\\[a-z]{2,}$",
    "Email",
    "must be a valid email address"
);

// Validate a string contains only alphanumeric characters
validationHelper.RequireAlphanumeric(order.Username, "Username");

// Validate a string contains only alphabetic characters
validationHelper.RequireAlphabetic(order.FirstName, "FirstName");

// Validate a string contains only numeric characters
validationHelper.RequireNumeric(order.ZipCode, "ZipCode");

// Validate a string has the exact length
validationHelper.RequireExactItems(order.PhoneNumbers, 2, "PhoneNumbers");

// Validate a numeric value is within range
validationHelper
    .RequireGreaterThan(order.Age, 18, "Age")
    .RequireLessThan(order.Age, 120, "Age");

// Validate a string is a valid IPv4 address
validationHelper.RequireValidIpAddress(order.IpAddress, "IpAddress");

// Validate a string is a valid GUID
validationHelper.RequireValidGuid(order.CorrelationId, "CorrelationId");

// Check if validation failed
if (validationHelper.HasErrors)
{
    foreach (var error in validationHelper.Errors)
    {
        Console.WriteLine(error);
    }
}
```

These extension methods provide a fluent API for common validation scenarios, making validation code more concise and easier to read while maintaining type safety and comprehensive error reporting.

## DeadLetterBenchmarks

The `DeadLetterBenchmarks` class provides performance benchmarks for dead letter queue operations, measuring throughput and latency for error handling and retry scenarios. It evaluates the overhead of publishing failed events, querying pending dead letter entries, reprocessing failed messages, and collecting dead letter statistics.

Here's an example usage:

```csharp
// Setup the benchmark infrastructure
var benchmarks = new DeadLetterBenchmarks();
benchmarks.GlobalSetup();

// Benchmark: Publish event that fails and gets sent to dead letter queue
await benchmarks.Publish_To_DeadLetter();

// Benchmark: Get pending dead letter entries
var pendingEntries = await benchmarks.Get_Pending_DeadLetter_Entries();

// Benchmark: Reprocess 10 dead letter entries
await benchmarks.Reprocess_10_DeadLetter_Entries();

// Benchmark: Get dead letter statistics
var statistics = await benchmarks.Get_DeadLetter_Statistics();

// Benchmark: Publish event with retry policy
await benchmarks.Publish_With_Retry_Policy();

// Benchmark: Memory allocation for dead letter operations
await benchmarks.DeadLetter_Memory_Allocation();

// Cleanup
benchmarks.GlobalCleanup();
```
