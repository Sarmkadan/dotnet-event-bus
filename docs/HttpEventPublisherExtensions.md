# HttpEventPublisherExtensions
The `HttpEventPublisherExtensions` class provides a set of extension methods for publishing events over HTTP. It allows for flexible and efficient event publishing, including batch publishing and error handling. These methods can be used to simplify the process of sending events to an HTTP endpoint, making it easier to integrate event-driven architecture into applications.

## API
The `HttpEventPublisherExtensions` class includes the following public members:
* `PublishAsync`: Publishes an event to an HTTP endpoint. Returns an `HttpPublishResult` object containing information about the publish operation.
* `PublishSuccessfullyAsync`: Publishes an event to an HTTP endpoint and returns a boolean indicating whether the publish operation was successful.
* `PublishWithDetailsAsync`: Publishes an event to an HTTP endpoint and returns a tuple containing a boolean indicating success, the HTTP status code, and an error message if applicable.
* `PublishBatchAsync`: Publishes a batch of events to an HTTP endpoint. Returns a list of `HttpPublishResult` objects containing information about each publish operation.
* `PublishToMultipleAsync`: Publishes an event to multiple HTTP endpoints. Returns a list of `HttpPublishResult` objects containing information about each publish operation.
* `PublishWithErrorContainingAsync`: Publishes an event to an HTTP endpoint and returns a boolean indicating whether the publish operation was successful, with error information included in the response.

## Usage
Here are two examples of using the `HttpEventPublisherExtensions` class:
```csharp
// Example 1: Publishing a single event
var event = new MyEvent { /* event data */ };
var result = await HttpEventPublisherExtensions.PublishAsync(event);
if (result.Success)
{
    Console.WriteLine("Event published successfully");
}
else
{
    Console.WriteLine($"Error publishing event: {result.ErrorMessage}");
}

// Example 2: Publishing a batch of events
var events = new[] { new MyEvent { /* event data */ }, new MyEvent { /* event data */ } };
var results = await HttpEventPublisherExtensions.PublishBatchAsync(events);
foreach (var result in results)
{
    if (result.Success)
    {
        Console.WriteLine("Event published successfully");
    }
    else
    {
        Console.WriteLine($"Error publishing event: {result.ErrorMessage}");
    }
}
```

## Notes
When using the `HttpEventPublisherExtensions` class, consider the following:
* The `PublishAsync` and `PublishSuccessfullyAsync` methods will throw an exception if the HTTP request fails or times out.
* The `PublishWithDetailsAsync` method provides more detailed information about the publish operation, including the HTTP status code and error message.
* The `PublishBatchAsync` and `PublishToMultipleAsync` methods are useful for publishing multiple events in a single operation, but may have performance implications depending on the size of the batch and the number of endpoints.
* The `HttpEventPublisherExtensions` class is designed to be thread-safe, but it is still important to ensure that the underlying HTTP client is properly configured and used in a thread-safe manner.
