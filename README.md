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

## TestEvent

The `TestEvent` class is a simple test event used throughout the event bus unit tests. It contains basic properties for testing event publishing, subscription, and handler invocation scenarios. This event is commonly used to validate core event bus functionality including handler execution, priority-based invocation, and subscription management.

Example usage:

```csharp
using DotnetEventBus;
using DotnetEventBus.Tests;
using Microsoft.Extensions.DependencyInjection;

// Create service collection and configure event bus
var services = new ServiceCollection();
services.AddEventBus();
var provider = services.BuildServiceProvider();
var eventBus = provider.GetRequiredService<IEventBus>();

// Define a test event with sample data
var testEvent = new TestEvent {
    Data = "Sample event data",
    Value = 42
};

// Subscribe an asynchronous handler
eventBus.Subscribe<TestEvent>(
    async (@event, ct) => {
        Console.WriteLine($"Received event with Data: {@event.Data}, Value: {@event.Value}");
        await Task.CompletedTask;
    },
    handlerName: "TestHandler"
);

// Subscribe a synchronous handler
eventBus.SubscribeSync<TestEvent>(
    @event => {
        Console.WriteLine($"Sync handler received: {@event.Data}");
    },
    handlerName: "SyncTestHandler"
);

// Subscribe with priority
eventBus.Subscribe<TestEvent>(
    async (@event, ct) => {
        Console.WriteLine("High priority handler");
        await Task.CompletedTask;
    },
    handlerName: "HighPriorityHandler",
    priority: (int)HandlerPriority.High
);

// Publish the test event
var result = await eventBus.PublishAsync(testEvent);

// Verify the event was processed
Console.WriteLine($"Handlers invoked: {result.HandlersInvoked}");
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

## BatchEventPublisherTests

The `BatchEventPublisherTests` class provides comprehensive unit tests for the `BatchEventPublisher` batch event publishing functionality. It validates event addition to batches, batch flushing when the batch size is reached, error handling for invalid events, and constructor validation for proper argument handling. These tests serve as both validation and usage examples for working with the batch event publishing system.

Example usage:

```csharp
using DotnetEventBus.Services;
using DotnetEventBus.Models;
using Microsoft.Extensions.Logging;
using Xunit;

// Create a batch event publisher with a batch size of 10 events
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var publisher = new BatchEventPublisher(loggerFactory.CreateLogger<BatchEventPublisher>(), batchSize: 10);

// Add valid events to the batch
var envelope1 = new EventEnvelope { EventType = "OrderCreated", Payload = "{ \"orderId\": 123 }" };
var envelope2 = new EventEnvelope { EventType = "PaymentProcessed", Payload = "{ \"paymentId\": 456 }" };

var added1 = await publisher.AddEventAsync(envelope1); // Returns true
var added2 = await publisher.AddEventAsync(envelope2); // Returns true

// Set a flush handler to process batches when they're full
var flushedBatches = new List<EventBatch>();
publisher.SetFlushHandler(async batch => {
    flushedBatches.Add(batch);
    await Task.CompletedTask;
});

// Add enough events to trigger a flush (10 events with batchSize=10)
for (int i = 0; i < 10; i++) {
    var envelope = new EventEnvelope { EventType = "Event" + i, Payload = "payload" };
    await publisher.AddEventAsync(envelope);
}

// Verify the batch was flushed
Assert.Single(flushedBatches);
Assert.Equal(10, flushedBatches[0].Events.Count);

// Flush remaining events manually
await publisher.FlushAsync();
```

## EventMessageModelTests

The `EventMessageModelTests` class provides comprehensive unit tests for the `EventMessage` model behavior, validating message retry functionality, header management, and processing state management. These tests ensure proper handling of event message operations including creating retry messages, managing custom headers, and tracking processing attempts.

Example usage:

```csharp
using DotnetEventBus.Models;

// Create a new event message
var message = new EventMessage("OrderService.OrderPlaced", "{\"orderId\": 123, \"amount\": 99.99}");

// Set message metadata
message.CorrelationId = "corr-12345";
message.Source = "order-service";
message.ProcessingAttempts = 0;

// Add custom headers for tracing and routing
message.AddHeader("x-trace-id", "trace-abc-123");
message.AddHeader("x-region", "eu-west-1");
message.AddHeader("x-priority", "high");

// Retrieve a header value
string traceId = message.GetHeader("x-trace-id"); // Returns "trace-abc-123"
string unknownHeader = message.GetHeader("x-unknown"); // Returns null

// Create a retry message (increments ProcessingAttempts and generates new MessageId)
var retryMessage = message.CreateRetry();
Console.WriteLine($"Retry has {retryMessage.ProcessingAttempts} attempts"); // 1
Console.WriteLine($"Original MessageId: {message.MessageId}, Retry MessageId: {retryMessage.MessageId}"); // Different IDs

// Verify headers are preserved in retry
string retryTraceId = retryMessage.GetHeader("x-trace-id"); // Returns "trace-abc-123"
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

## EventFilteringExample

The `EventFilteringExample` class demonstrates selective event handler execution based on event properties using fluent filter APIs. It shows how to create filtered subscriptions that only process events matching specific criteria, enabling targeted event handling for different business scenarios.

Example usage:

```csharp
using DotnetEventBus;
using DotnetEventBus.Advanced;
using Microsoft.Extensions.DependencyInjection;

// Create service collection and configure event bus
var services = new ServiceCollection();
services.AddEventBus(options => {
    options.AllowParallelHandling = false;
});

var serviceProvider = services.BuildServiceProvider();
var eventBus = serviceProvider.GetRequiredService<IEventBus>();

// Define event types
public sealed class SalesEvent
{
    public string OrderId { get; set; }
    public string Region { get; set; }
    public decimal Amount { get; set; }
    public string CustomerSegment { get; set; }
    public DateTime Timestamp { get; set; }
}

public sealed class AlertEvent
{
    public string AlertId { get; set; }
    public string Severity { get; set; }
    public string Source { get; set; }
    public string Message { get; set; }
}

// Create a filter for high-value orders (> $1000)
var highValueFilter = new EventFilterBuilder()
    .Where<SalesEvent>(e => e.Amount > 1000m)
    .Build();

// Subscribe a handler that only processes high-value orders
eventBus.Subscribe<SalesEvent>(
    async (@event, ct) => {
        Console.WriteLine($"Processing high-value order: {0}", @event.OrderId);
        await Task.CompletedTask;
    },
    handlerName: "HighValueOrderHandler",
    filter: highValueFilter
);

// Subscribe a handler for premium customers
var premiumFilter = new EventFilterBuilder()
    .Where<SalesEvent>(e => e.CustomerSegment == "Premium")
    .Build();

eventBus.Subscribe<SalesEvent>(
    async (@event, ct) => {
        Console.WriteLine($"Processing premium customer order");
        await Task.CompletedTask;
    },
    handlerName: "PremiumCustomerHandler",
    filter: premiumFilter
);

// Subscribe a handler for critical alerts
var criticalFilter = new EventFilterBuilder()
    .Where<AlertEvent>(e => e.Severity == "Critical")
    .Build();

eventBus.Subscribe<AlertEvent>(
    async (@event, ct) => {
        Console.WriteLine($"CRITICAL ALERT: {0}", @event.Message);
        await Task.CompletedTask;
    },
    handlerName: "CriticalAlertHandler",
    filter: criticalFilter
);

// Publish events - only matching handlers will be invoked
await eventBus.PublishAsync(new SalesEvent {
    OrderId = "ORD-123",
    Region = "North America",
    Amount = 1500m,
    CustomerSegment = "Premium",
    Timestamp = DateTime.UtcNow
});

await eventBus.PublishAsync(new AlertEvent {
    AlertId = "ALT-456",
    Severity = "Critical",
    Source = "Database",
    Message = "Connection pool exhausted"
});
```

## MetricsCollectorTests

The `MetricsCollectorTests` class provides comprehensive unit tests for the metrics collection functionality within the DotnetEventBus library. It validates metrics tracking for event publishing operations including publish counts, durations, failure tracking, handler execution metrics, success rates, and provides methods to retrieve and reset collected metrics.

Example usage:

```csharp
using DotnetEventBus.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

// Create service collection and configure event bus with metrics
var services = new ServiceCollection();
services.AddEventBus(options => {
    options.EnableMetricsCollection = true;
});

var provider = services.BuildServiceProvider();
var metricsCollector = provider.GetRequiredService<IMetricsCollector>();

// Record successful event publishing with duration
metricsCollector.RecordEventPublished("OrderCreated", TimeSpan.FromMilliseconds(150));
metricsCollector.RecordEventPublished("PaymentProcessed", TimeSpan.FromMilliseconds(80));
metricsCollector.RecordEventPublished("InventoryUpdated", TimeSpan.FromMilliseconds(200));

// Record failed event publishing with error message
metricsCollector.RecordEventFailed("OrderCreated", new InvalidOperationException("Database timeout"));
metricsCollector.RecordEventFailed("PaymentProcessed", new InvalidOperationException("Payment gateway unavailable"));

// Record handler execution metrics
metricsCollector.RecordHandlerExecution("OrderCreatedHandler", TimeSpan.FromMilliseconds(120), true);
metricsCollector.RecordHandlerExecution("PaymentProcessedHandler", TimeSpan.FromMilliseconds(60), true);
metricsCollector.RecordHandlerExecution("InventoryUpdatedHandler", TimeSpan.FromMilliseconds(180), false);

// Retrieve all event metrics
var allEventMetrics = metricsCollector.GetAllEventMetrics();
foreach (var metric in allEventMetrics)
{
    Console.WriteLine($"Event: {metric.EventType}, Published: {metric.PublishCount}, " +
                     $"Avg Duration: {metric.AverageDuration.TotalMilliseconds}ms, " +
                     $"Failures: {metric.FailureCount}, Last Failure: {metric.LastFailureTime}");
}

// Retrieve all handler metrics
var allHandlerMetrics = metricsCollector.GetAllHandlerMetrics();
foreach (var metric in allHandlerMetrics)
{
    Console.WriteLine($"Handler: {metric.HandlerName}, Executions: {metric.ExecutionCount}, " +
                     $"Avg Duration: {metric.AverageDuration.TotalMilliseconds}ms, " +
                     $"Success Rate: {metric.SuccessRate:P}, Last Failure: {metric.LastFailureTime}");
}

// Calculate success rate for a specific event type
var orderSuccessRate = metricsCollector.GetSuccessRate("OrderCreated");
Console.WriteLine($"OrderCreated success rate: {orderSuccessRate:P}");

// Calculate average duration for a specific handler
var handlerAvgDuration = metricsCollector.GetAverageDuration("OrderCreatedHandler");
Console.WriteLine($"OrderCreatedHandler average duration: {handlerAvgDuration.TotalMilliseconds}ms");

// Get last failure time for an event type
var lastFailure = metricsCollector.GetLastFailureTime("PaymentProcessed");
Console.WriteLine($"Last PaymentProcessed failure: {lastFailure}");

// Get last published time for an event type
var lastPublished = metricsCollector.GetLastPublishedTime("InventoryUpdated");
Console.WriteLine($"Last InventoryUpdated publish: {lastPublished}");

// Reset all metrics (useful for testing scenarios)
metricsCollector.Reset();

// Verify metrics were cleared
var emptyEventMetrics = metricsCollector.GetAllEventMetrics();
var emptyHandlerMetrics = metricsCollector.GetAllHandlerMetrics();
Console.WriteLine($"Event metrics after reset: {emptyEventMetrics.Count}");
Console.WriteLine($"Handler metrics after reset: {emptyHandlerMetrics.Count}");
```

## CircuitBreakerTests

The `CircuitBreakerTests` class provides comprehensive unit tests for the `CircuitBreaker` class, validating circuit breaker behavior including state transitions, failure handling, and recovery mechanisms. The tests cover all circuit states (Closed, Open, HalfOpen) and verify proper exception handling.

Example usage:

```csharp
using DotnetEventBus.Integration;
using Xunit;

// Create a circuit breaker with failure threshold of 5 exceptions
var breaker = new CircuitBreaker(failureThreshold: 5);

// Execute a successful operation - circuit remains closed
var result = await breaker.ExecuteAsync(async () => "success");
Assert.Equal("success", result);
Assert.Equal(CircuitBreakerState.Closed, breaker.State);

// Execute operations that throw exceptions below threshold - circuit stays closed
for (int i = 0; i < 3; i++)
{
try
{
await breaker.ExecuteAsync(async () => throw new TimeoutException("Transient failure"));
}
catch { /* expected */ }
}
Assert.Equal(CircuitBreakerState.Closed, breaker.State);

// Execute operations that exceed failure threshold - circuit opens
for (int i = 0; i < 5; i++)
{
try
{
await breaker.ExecuteAsync(async () => throw new TimeoutException("Failure"));
}
catch { /* expected */ }
}
Assert.Equal(CircuitBreakerState.Open, breaker.State);

// Attempt to execute when circuit is open - throws CircuitBreakerOpenException
try
{
await breaker.ExecuteAsync(async () => "should not execute");
Assert.Fail("Should have thrown CircuitBreakerOpenException");
}
catch (CircuitBreakerOpenException)
{
// Expected exception
}

// Wait for timeout to expire (10 seconds), then circuit transitions to HalfOpen
// A successful operation in HalfOpen state closes the circuit again
await Task.Delay(TimeSpan.FromSeconds(10));
var recoveryResult = await breaker.ExecuteAsync(async () => "recovery attempt");
Assert.Equal("recovery attempt", recoveryResult);
Assert.Equal(CircuitBreakerState.Closed, breaker.State);

// Execute a void operation (no return value)
var breaker2 = new CircuitBreaker(failureThreshold: 3);
bool wasExecuted = false;
await breaker2.ExecuteAsync(async () => 
{
wasExecuted = true;
await Task.CompletedTask;
});
Assert.True(wasExecuted);
```