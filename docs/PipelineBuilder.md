# PipelineBuilder
The `PipelineBuilder` type is used to construct and manage event processing pipelines in the `dotnet-event-bus` project. It provides a flexible way to build and customize event handling workflows, allowing for the incorporation of various middleware components and metadata management.

## API
* `public PipelineBuilder Use`: This method is used to add middleware components to the pipeline. It returns the current `PipelineBuilder` instance, allowing for method chaining.
* `public EventBusMiddleware Build`: This method constructs and returns the event processing pipeline as an `EventBusMiddleware` delegate. It does not take any parameters and does not throw any exceptions.
* `public void Clear`: This method clears the pipeline, removing all added middleware components and resetting the pipeline to its initial state. It does not take any parameters and does not throw any exceptions.
* `public delegate Task EventBusMiddleware`: This delegate represents the event processing pipeline, which takes no parameters and returns a `Task`.
* `public required string EventType`: This property sets the type of event being processed. It is required and must be set before building the pipeline.
* `public required object EventData`: This property sets the data associated with the event being processed. It is required and must be set before building the pipeline.
* `public Dictionary<string, object> Metadata`: This property stores additional metadata associated with the event processing pipeline.
* `public DateTime CreatedAt`: This property stores the timestamp when the pipeline was created.
* `public string? CorrelationId`: This property stores a correlation ID for the event processing pipeline, which can be used for tracing and logging purposes.
* `public bool IsProcessed`: This property indicates whether the event has been processed.
* `public Exception? ProcessingException`: This property stores any exception that occurred during event processing.

## Usage
The following examples demonstrate how to use the `PipelineBuilder` type to construct and manage event processing pipelines:
```csharp
// Example 1: Building a simple event processing pipeline
var pipelineBuilder = new PipelineBuilder();
pipelineBuilder.EventType = "MyEventType";
pipelineBuilder.EventData = new MyEventData();
pipelineBuilder.Use(async (next) => {
    // Process the event
    await next();
});
var pipeline = pipelineBuilder.Build();

// Example 2: Building a pipeline with multiple middleware components
var pipelineBuilder2 = new PipelineBuilder();
pipelineBuilder2.EventType = "MyEventType2";
pipelineBuilder2.EventData = new MyEventData2();
pipelineBuilder2.Use(async (next) => {
    // Validate the event data
    if (!(pipelineBuilder2.EventData is MyEventData2 data) || data.IsValid == false)
    {
        throw new InvalidEventDataException();
    }
    await next();
});
pipelineBuilder2.Use(async (next) => {
    // Process the event
    // ...
    await next();
});
var pipeline2 = pipelineBuilder2.Build();
```

## Notes
When using the `PipelineBuilder` type, it is essential to set the `EventType` and `EventData` properties before building the pipeline. Failure to do so may result in unexpected behavior or exceptions. Additionally, the `Clear` method should be used with caution, as it removes all added middleware components and resets the pipeline to its initial state. The `PipelineBuilder` type is not thread-safe, and it is recommended to create a new instance for each event processing pipeline. The `EventBusMiddleware` delegate returned by the `Build` method can be used to process events asynchronously, and it is the responsibility of the caller to handle any exceptions that may occur during event processing.
