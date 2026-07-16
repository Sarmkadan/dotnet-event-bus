## EventFormatterFactory

The `EventFormatterFactory` class provides a registry for event formatters within the event bus. It allows registering formatters for specific data formats (JSON, XML, CSV), negotiating the appropriate formatter based on content type or format name, and managing the lifecycle of formatters.

Example usage:

```csharp
using DotnetEventBus.Formatters;

// Create a default factory with pre-configured formatters
var factory = EventFormatterFactory.CreateDefault();

// Register a custom formatter
factory.Register(new CustomEventFormatter());

// Get a formatter by format name
var jsonFormatter = factory.GetFormatter("json");

// Get a formatter by MIME type
var jsonMimeTypeFormatter = factory.GetFormatterByContentType("application/json");

// Get all registered formatters
var allFormatters = factory.GetAllFormatters();

// Check if a format is supported
bool isJsonSupported = factory.IsFormatSupported("json");

// Unregister a formatter
bool unregistered = factory.Unregister("csv");
```

Example usage:
```csharp
using DotnetEventBus;
using DotnetEventBus.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

// Create service collection and configure event bus
var services = new ServiceCollection();
services.AddLogging(configure => configure.AddConsole());
services.AddEventBus(options => {
    options.ApplicationName = "IntegrationTestApp";
    options.EnableDeadLetterQueue = true;
    options.MaxConcurrentHandlers = 10;
    options.RetryPolicy = new ExponentialBackoffRetryPolicy {
        MaxAttempts = 3,
        InitialDelay = TimeSpan.FromMilliseconds(100),
        MaxDelay = TimeSpan.FromSeconds(5)
    };
});

var serviceProvider = services.BuildServiceProvider();
var eventBus = serviceProvider.GetRequiredService<IEventBus>();

// Define event handlers
public class OrderCreatedHandler : IEventHandler<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedHandler> _logger;
    
    public OrderCreatedHandler(ILogger<OrderCreatedHandler> logger)
    {
        _logger = logger;
    }
    
    public async Task Handle(OrderCreatedEvent @event)
    {
        _logger.LogInformation("Processing order {OrderId}", @event.OrderId);
        // Business logic here
    }
}

public class PaymentProcessedHandler : IEventHandler<PaymentProcessedEvent>
{
    public async Task Handle(PaymentProcessedEvent @event)
    {
        // Handle payment processed event
    }
}

// Register handlers
services.AddTransient<OrderCreatedHandler>();
serviceProvider = services.BuildServiceProvider();
eventBus = serviceProvider.GetRequiredService<IEventBus>();

// Subscribe handlers
eventBus.Subscribe<OrderCreatedEvent, OrderCreatedHandler>();
eventBus.Subscribe<PaymentProcessedEvent, PaymentProcessedHandler>();

// Publish events
var orderCreatedEvent = new OrderCreatedEvent { OrderId = 123, Amount = 99.99m };
var paymentProcessedEvent = new PaymentProcessedEvent { PaymentId = 456, Status = "Completed" };

await eventBus.PublishAsync(orderCreatedEvent);
await eventBus.PublishAsync(paymentProcessedEvent);

// Verify events were processed
// Use assertions or metrics to verify handler execution
```

## XmlEventFormatter
The `XmlEventFormatter` class is used to format events as XML, supporting both serialization and deserialization. It provides methods to serialize objects to XML strings, deserialize XML strings to objects, and format events with or without metadata. Here's an example of how to use it:
```csharp
var formatter = new XmlEventFormatter();
var eventData = new { Id = 1, Name = "John" };
var xml = formatter.Serialize(eventData, prettyPrint: true);
var deserializedData = formatter.Deserialize<dynamic>(xml);
var formattedEvent = formatter.FormatEvent(eventData);
var formattedEventWithMetadata = formatter.FormatEventWithMetadata(eventData, new Dictionary<string, object> { { "timestamp", DateTime.UtcNow } });
```

## JsonEventFormatter
The `JsonEventFormatter` class formats events as JSON strings for serialization and API responses. It supports both compact and pretty-printed output, and provides methods for serializing objects to JSON, deserializing JSON strings to objects, and formatting events with or without metadata.

Example usage:
```csharp
using DotnetEventBus.Formatters;
using System;
using System.Collections.Generic;

// Create a new JSON formatter
var formatter = new JsonEventFormatter();

// Serialize an object to compact JSON
var eventData = new { Id = 1, Name = "Test Event", Timestamp = DateTime.UtcNow };
string compactJson = formatter.Serialize(eventData);

// Serialize with pretty printing
string prettyJson = formatter.Serialize(eventData, prettyPrint: true);

// Deserialize JSON back to a strongly-typed object
var deserializedData = formatter.Deserialize<Dictionary<string, object>>(compactJson);

// Format an event as JSON
string formattedEvent = formatter.FormatEvent(eventData);

// Format an event with metadata
var metadata = new Dictionary<string, object> {
    { "source", "event-bus" },
    { "priority", "high" }
};
string formattedEventWithMetadata = formatter.FormatEventWithMetadata(eventData, metadata);

// Deserialize to a specific type
var typedData = formatter.Deserialize<MyEventType>(compactJson);

public class MyEventType
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## DeadLetterServiceTests

The `DeadLetterServiceTests` class provides comprehensive unit tests for the `DeadLetterService` dead letter queue functionality. It validates the service's ability to retrieve pending dead letter entries, mark entries as reviewed, retrieve statistics, and archive old entries. These tests ensure proper handling of failed event processing scenarios and provide examples for working with the dead letter queue system.

Example usage:

```csharp
using DotnetEventBus.Services;
using DotnetEventBus.Models;
using Microsoft.Extensions.DependencyInjection;

// Create service collection and configure event bus
var services = new ServiceCollection();
services.AddEventBus();

// Build service provider
var provider = services.BuildServiceProvider();
var eventBus = provider.GetRequiredService<IEventBus>();
var repository = new InMemoryDeadLetterRepository();
var deadLetterService = new DeadLetterService(repository, eventBus);

// Add a dead letter entry
var msg = new EventMessage("OrderCreated", "{ \"orderId\": 123 }");
var entry = new DeadLetterEntry(msg, "OrderHandler", new InvalidOperationException("Order processing failed"), 3);
await repository.AddAsync(entry);

// Get pending entries
var pendingEntries = await deadLetterService.GetPendingEntriesAsync();

// Mark as reviewed
await deadLetterService.MarkAsReviewedAsync(entry.Id, "Reviewed for reprocessing");

// Get statistics
var stats = await deadLetterService.GetStatisticsAsync();
Console.WriteLine($"Total entries: {stats.TotalEntries}, Pending: {stats.PendingEntries}");

// Archive old entries
await deadLetterService.ArchiveOldEntriesAsync(TimeSpan.FromDays(7));
```

## InMemoryRepositoryTests

The `InMemoryRepositoryTests` class provides comprehensive unit tests for the in-memory repository implementation. It validates basic CRUD operations, pagination, existence checks, counting, and clearing functionality for generic entity storage. These tests serve as both validation and usage examples for the `InMemoryRepository<T>` class.

Example usage:

```csharp
using DotnetEventBus.Repositories;
using DotnetEventBus.Models;

// Create an in-memory repository for a custom entity
var repository = new InMemoryRepository<TestEntity>();

// Add a new entity
var newEntity = new TestEntity { Id = "1", Name = "Sample Entity" };
var addedEntity = await repository.AddAsync(newEntity);

// Retrieve an entity by ID
var retrievedEntity = await repository.GetByIdAsync("1");
Console.WriteLine($"Retrieved: {retrievedEntity?.Name}");

// Update an existing entity
retrievedEntity.Name = "Updated Entity";
await repository.UpdateAsync(retrievedEntity);

// Check if entity exists
hasEntity = await repository.ExistsAsync("1");
Console.WriteLine($"Entity exists: {hasEntity}");

// Get total count of entities
var totalCount = await repository.CountAsync();
Console.WriteLine($"Total entities: {totalCount}");

// Get paginated results
var page = await repository.GetPagedAsync(1, 10);
Console.WriteLine($"Page 1 has {page.Items.Count} items, total {page.TotalCount} items");

// Clear all entities
await repository.ClearAsync();
var countAfterClear = await repository.CountAsync();
Console.WriteLine($"Entities after clear: {countAfterClear}");

// Access repository properties
Console.WriteLine($"Repository ID: {repository.Id}");
Console.WriteLine($"Repository Name: {repository.Name}");
```

## PipelineBuilderTests

The `PipelineBuilderTests` class provides comprehensive unit tests for the `PipelineBuilder` middleware pipeline construction. It verifies middleware registration, execution order, context manipulation, error handling, and pipeline building scenarios. The tests cover both synchronous and asynchronous middleware execution, short-circuiting behavior, exception handling, and proper initialization of event context.

## EventBusIntegrationTests

The `EventBusIntegrationTests` class provides comprehensive integration tests for the event bus system, validating end-to-end scenarios including event publishing, subscription, handler execution, error handling, retry policies, circuit breakers, metrics collection, and middleware pipelines. These tests ensure the event bus operates correctly under various conditions including parallel publishing, batch processing, priority-based execution, and failure scenarios.

Example usage:
```csharp
using DotnetEventBus.Middleware;
using Xunit;

// Create a pipeline builder
var builder = new PipelineBuilder();

// Add middleware components to the pipeline
builder.Use(next => async context => {
    // Pre-processing logic
    context.Metadata["startedAt"] = DateTime.UtcNow;
    await next(context);
    // Post-processing logic
});

builder.Use(next => async context => {
    // Validation middleware
    if (context.EventData == null)
        throw new InvalidOperationException("Event data cannot be null");
    await next(context);
});

builder.Use(next => async context => {
    // Processing middleware
    context.IsProcessed = true;
    await next(context);
});

// Build the pipeline
var pipeline = builder.Build();

// Create and execute the pipeline with an event context
var context = new EventContext {
    EventType = "OrderPlaced",
    EventData = new { OrderId = 123, Amount = 99.99 }
};

await pipeline(context);

// Verify the context was processed
Assert.True(context.IsProcessed);
Assert.ContainsKey(context.Metadata, "startedAt");
```

## TestFilterEvent

The `TestFilterEvent` class is a test event used for filtering scenarios within the event bus system. It contains properties that can be filtered on including order ID, amount, status, and region. This event is primarily used in unit tests for the `EventFilter<T>` class to validate filtering logic.

Example usage:

```csharp
using DotnetEventBus.Advanced;

// Define a test event
var testEvent = new TestFilterEvent {
    OrderId = 123,
    Amount = 99.99m,
    Status = "Pending",
    Region = "US"
};

// Create a filter to match events with OrderId > 100
var filter = new EventFilter<TestFilterEvent>()
    .Where(e => e.OrderId > 100);

// Check if the event matches the filter
bool matches = filter.Matches(testEvent); // Returns true
```

## NewCoreFunctionalityTests

The `NewCoreFunctionalityTests` class provides comprehensive integration tests for new core functionality of the DotnetEventBus library. It validates priority-based handler invocation, middleware pipeline execution, retry mechanisms, dead letter queue functionality, and subscription management. These tests ensure the event bus operates correctly under various conditions including parallel publishing, priority-based execution, and failure scenarios.

Example usage:

```csharp
using DotnetEventBus.Configuration;
using DotnetEventBus.Middleware;
using DotnetEventBus.Models;
using DotnetEventBus.Repositories;
using DotnetEventBus.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

// Create service collection and configure event bus
var services = new ServiceCollection();
services.AddLogging();
var serviceProvider = services.BuildServiceProvider();

// Create event bus with default options
var options = new EventBusOptions();
var bus = new EventBus(options: options, serviceProvider: serviceProvider);

// Subscribe handlers with different priorities
bus.Subscribe<string>((e, ct) => {
    Console.WriteLine("Low priority handler executed");
    return Task.CompletedTask;
}, "LowPriority", priority: 0);

bus.Subscribe<string>((e, ct) => {
    Console.WriteLine("High priority handler executed");
    return Task.CompletedTask;
}, "HighPriority", priority: 10);

// Publish an event - handlers will be invoked in priority order
await bus.PublishAsync("test-event");

// Subscribe with retry policy
var retryOptions = new EventBusOptions { MaxRetryAttempts = 2 };
var retryBus = new EventBus(options: retryOptions, serviceProvider: serviceProvider);
int attempts = 0;

retryBus.Subscribe<string>((e, ct) => {
    attempts++;
    if (attempts < 3) throw new Exception("Temporary failure");
    return Task.CompletedTask;
});

await retryBus.PublishAsync("retry-test");
Console.WriteLine($"Handler attempted {attempts} times"); // Will be 3

// Unsubscribe to prevent future invocations
var subscription = bus.SubscribeSync<string>(e => Console.WriteLine("Handler called"));
await bus.PublishAsync("first"); // Handler called
subscription.Dispose();
await bus.PublishAsync("second"); // Handler not called
```

Example usage:

```csharp
using DotnetEventBus.Advanced;

// Define a test event
var testEvent = new TestFilterEvent {
    OrderId = 123,
    Amount = 99.99m,
    Status = "Pending",
    Region = "US"
};

// Create a filter to match events with OrderId > 100
var filter = new EventFilter<TestFilterEvent>()
    .Where(e => e.OrderId > 100);

// Check if the event matches the filter
bool matches = filter.Matches(testEvent); // Returns true

// Create a filter with multiple predicates
var complexFilter = new EventFilter<TestFilterEvent>()
    .Where(e => e.OrderId > 100)
    .WhereProperty(e => e.Status, "Pending")
    .WherePropertyInRange(e => e.Amount, 50, 500);

// Check if the event matches all predicates
bool complexMatches = complexFilter.Matches(testEvent); // Returns true

// Create a filter that excludes completed events
var notFilter = new EventFilter<TestFilterEvent>()
    .Not(e => e.Status == "Completed");

// Check if the event matches the inverted predicate
bool notMatches = notFilter.Matches(testEvent); // Returns true

// Filter events by region (null-safe)
var regionFilter = new EventFilter<TestFilterEvent>()
    .WhereProperty(e => e.Region, "US");

var usEvent = new TestFilterEvent { Region = "US" };
bool regionMatches = regionFilter.Matches(usEvent); // Returns true

var nullRegionEvent = new TestFilterEvent { Region = null };
bool nullRegionMatches = regionFilter.Matches(nullRegionEvent); // Returns false

// Filter events by string contains (case-insensitive)
var statusFilter = new EventFilter<TestFilterEvent>()
    .WherePropertyContains(e => e.Status, "end");

var endStatusEvent = new TestFilterEvent { Status = "Pending" };
bool containsMatches = statusFilter.Matches(endStatusEvent); // Returns true
```

## RetryPolicyTests

The `RetryPolicyTests` class provides comprehensive unit tests for the retry policy functionality within the DotnetEventBus library. It validates various retry scenarios including successful operations without retries, transient failures with automatic retries, maximum retry limits, exponential backoff with jitter, delay capping, exception filtering, and configuration validation for retry parameters.

Example usage:

```csharp
using DotnetEventBus.Retry;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

// Configure retry policy with exponential backoff
var retryPolicy = new ExponentialBackoffRetryPolicy
{
    MaxAttempts = 3,
    InitialDelay = TimeSpan.FromMilliseconds(100),
    MaxDelay = TimeSpan.FromSeconds(5),
    BackoffMultiplier = 2.0,
    EnableJitter = true
};

// Execute an operation with retry policy
int attemptCount = 0;
var result = await retryPolicy.ExecuteAsync(async () =>
{
    attemptCount++;
    if (attemptCount < 3)
    {
        throw new InvalidOperationException("Temporary failure");
    }
    return "Success";
});

Console.WriteLine($"Operation succeeded after {attemptCount} attempts");

// Configure retry policy with immediate retries
var immediateRetryPolicy = new ImmediateRetryPolicy
{
    MaxAttempts = 5
};

// Execute an operation that eventually succeeds
var successResult = await immediateRetryPolicy.ExecuteAsync(async () =>
{
    if (DateTime.UtcNow.Second % 2 == 0)
    {
        return "Success";
    }
    throw new InvalidOperationException("Temporary failure");
});

// Configure retry policy with custom exception filter
var filteredRetryPolicy = new ExponentialBackoffRetryPolicy
{
    MaxAttempts = 3,
    InitialDelay = TimeSpan.FromMilliseconds(50),
    ExceptionFilter = ex => ex is InvalidOperationException || ex is ArgumentException
};

// Execute an operation that throws a retryable exception
var filteredResult = await filteredRetryPolicy.ExecuteAsync(async () =>
{
    throw new InvalidOperationException("This will be retried");
});

// Configure retry policy with capped delay
var cappedRetryPolicy = new ExponentialBackoffRetryPolicy
{
    MaxAttempts = 5,
    InitialDelay = TimeSpan.FromSeconds(1),
    MaxDelay = TimeSpan.FromSeconds(2), // Maximum delay will be capped at 2 seconds
    BackoffMultiplier = 2.0
};

// Execute an operation with capped delay between retries
var cappedResult = await cappedRetryPolicy.ExecuteAsync(async () =>
{
    throw new InvalidOperationException("Will retry with capped delay");
});

// Configure retry policy with jitter disabled for consistent delays
var consistentRetryPolicy = new ExponentialBackoffRetryPolicy
{
    MaxAttempts = 3,
    InitialDelay = TimeSpan.FromMilliseconds(100),
    EnableJitter = false // Delays will be exactly InitialDelay * multiplier
};

// Execute an operation with consistent delays
var consistentResult = await consistentRetryPolicy.ExecuteAsync(async () =>
{
    throw new InvalidOperationException("Will retry with consistent delays");
});
```