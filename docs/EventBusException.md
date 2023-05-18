# EventBusException
The `EventBusException` is a base exception class for the `dotnet-event-bus` project, providing a common root for various exceptions that may occur during event bus operations. It serves as a catch-all for errors related to event handling, message serialization, and distributed bus configuration, allowing for more specific exception handling and error reporting.

## API
* `public EventBusException()`: Initializes a new instance of the `EventBusException` class with no message.
* `public EventBusException(string? message)`: Initializes a new instance of the `EventBusException` class with a specified error message.
* `public string EventType`: Gets the type of event associated with the exception.
* `public NoHandlersRegisteredException`: Gets the exception related to no handlers being registered.
* `public string HandlerName`: Gets the name of the handler associated with the exception.
* `public string EventType`: Gets the type of event associated with the exception.
* `public HandlerInvocationException`: Gets the exception related to handler invocation.
* `public Type HandlerType`: Gets the type of handler associated with the exception.
* `public InvalidHandlerException`: Gets the exception related to an invalid handler.
* `public string MessageType`: Gets the type of message associated with the exception.
* `public MessageSerializationException`: Gets the exception related to message serialization.
* `public DistributedBusNotConfiguredException`: Gets the exception related to the distributed bus not being configured.
* `public string RequestType`: Gets the type of request associated with the exception.
* `public TimeSpan Timeout`: Gets the timeout associated with the exception.
* `public RequestTimeoutException`: Gets the exception related to a request timeout.

## Usage
The following examples demonstrate how to use the `EventBusException` class:
```csharp
try
{
    // Attempt to publish an event
    eventBus.Publish(new MyEvent());
}
catch (EventBusException ex)
{
    // Handle the exception
    Console.WriteLine($"Error publishing event: {ex.Message}");
    Console.WriteLine($"Event type: {ex.EventType}");
}

try
{
    // Attempt to handle an event
    eventBus.Subscribe<MyEvent>(async (event) =>
    {
        // Handle the event
    });
}
catch (EventBusException ex)
{
    // Handle the exception
    Console.WriteLine($"Error handling event: {ex.Message}");
    Console.WriteLine($"Handler name: {ex.HandlerName}");
}
```

## Notes
When using the `EventBusException` class, consider the following edge cases and thread-safety remarks:
* The `EventBusException` class is not thread-safe, as it relies on instance fields to store exception data. When accessing exception properties from multiple threads, consider using thread-safe alternatives or synchronizing access to the exception instance.
* The `EventType` and `HandlerName` properties may be null if the exception is not related to a specific event or handler.
* The `NoHandlersRegisteredException`, `HandlerInvocationException`, `InvalidHandlerException`, `MessageSerializationException`, `DistributedBusNotConfiguredException`, and `RequestTimeoutException` properties provide additional information about the exception cause. When handling these exceptions, consider logging or reporting this information for debugging and error tracking purposes.
