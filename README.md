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

## PipelineBuilderTests

The `PipelineBuilderTests` class provides comprehensive unit tests for the `PipelineBuilder` middleware pipeline construction. It verifies middleware registration, execution order, context manipulation, error handling, and pipeline building scenarios. The tests cover both synchronous and asynchronous middleware execution, short-circuiting behavior, exception handling, and proper initialization of event context.

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