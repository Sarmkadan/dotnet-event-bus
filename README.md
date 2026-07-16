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
```