# EventHandlerBase

The `EventHandlerBase` class serves as the foundational abstraction for defining event handlers within the `dotnet-event-bus` infrastructure. It provides a standardized contract for identifying supported event types, naming handlers, and executing asynchronous handling logic, while supporting both strongly-typed generic events and dynamic runtime resolution. By inheriting from this base class, developers ensure their custom handlers integrate seamlessly with the bus's discovery, routing, and execution pipelines.

## API

The following members constitute the public surface area of `EventHandlerBase`. Implementations must adhere to the defined contracts to ensure correct behavior within the event bus.

### `GetEventType`
```csharp
public virtual Type GetEventType()
```
Retrieves the primary .NET `Type` associated with this handler. This is typically used for quick identification or logging purposes when a single main event type is expected.
*   **Return Value**: The `System.Type` of the event this handler is primarily designed to process.
*   **Remarks**: Since this member is `virtual`, derived classes may override it to provide specific type information. If not overridden, it may return `null` or a default depending on the base implementation context.

### `GetHandlerName`
```csharp
public virtual string GetHandlerName()
```
Returns a human-readable identifier for the handler instance.
*   **Return Value**: A `string` representing the name of the handler.
*   **Remarks**: This name is often used in diagnostics, logging, and error reporting to distinguish between multiple handlers subscribing to the same event type.

### `Handle` (Generic)
```csharp
public abstract Task<TResponse> Handle<TEvent, TResponse>(TEvent @event, CancellationToken cancellationToken)
```
The core asynchronous method for processing a specific event and producing a typed response. This method must be implemented by derived classes to define the business logic for strongly-typed events.
*   **Parameters**:
    *   `@event`: The event instance to be processed.
    *   `cancellationToken`: A token to monitor for cancellation requests.
*   **Return Value**: A `Task<TResponse>` representing the asynchronous operation, yielding a result of type `TResponse`.
*   **Exceptions**: May throw exceptions inherent to the business logic or if the `cancellationToken` is triggered.

### `Handle` (Void)
```csharp
public abstract Task Handle<TEvent>(TEvent @event, CancellationToken cancellationToken)
```
An overloaded asynchronous method for processing events that do not require a return value.
*   **Parameters**:
    *   `@event`: The event instance to be processed.
    *   `cancellationToken`: A token to monitor for cancellation requests.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: May throw exceptions inherent to the business logic or if the `cancellationToken` is triggered.

### `HandleDynamic`
```csharp
public abstract Task HandleDynamic(object @event, CancellationToken cancellationToken)
```
Processes an event passed as a generic `object`, allowing for runtime resolution of event types without compile-time generics. This is essential for scenarios where the event type is not known until execution time.
*   **Parameters**:
    *   `@event`: The event instance, boxed as an `object`.
    *   `cancellationToken`: A token to monitor for cancellation requests.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Remarks**: Implementations typically need to inspect the runtime type of `@event` and cast it appropriately before processing.

### `CanHandle`
```csharp
public virtual bool CanHandle(Type eventType)
```
Determines whether this handler is capable of processing a specific event type.
*   **Parameters**:
    *   `eventType`: The `System.Type` of the event to check.
*   **Return Value**: `true` if the handler supports the specified event type; otherwise, `false`.
*   **Remarks**: This method is consulted by the event bus during routing to filter applicable handlers.

### `GetSupportedEventTypes`
```csharp
public virtual IEnumerable<Type> GetSupportedEventTypes()
```
Enumerates all event types that this handler explicitly supports.
*   **Return Value**: An `IEnumerable<Type>` containing the supported event types.
*   **Remarks**: This allows the event bus to pre-calculate routing tables or perform validation during startup.

## Usage

### Example 1: Strongly-Typed Handler with Response
This example demonstrates implementing a handler for a specific `OrderCreated` event that returns a confirmation ID.

```csharp
public class OrderCreatedHandler : EventHandlerBase
{
    public override async Task<OrderConfirmationResponse> Handle<OrderCreated, OrderConfirmationResponse>(
        OrderCreated @event, 
        CancellationToken cancellationToken)
    {
        // Simulate processing logic
        await Task.Delay(100, cancellationToken);
        
        var response = new OrderConfirmationResponse 
        { 
            ConfirmationId = Guid.NewGuid(), 
            Status = "Processed" 
        };

        return response;
    }

    public override bool CanHandle(Type eventType)
    {
        return eventType == typeof(OrderCreated);
    }

    public override IEnumerable<Type> GetSupportedEventTypes()
    {
        yield return typeof(OrderCreated);
    }
}
```

### Example 2: Dynamic Handler for Multiple Event Types
This example shows a handler capable of processing multiple event types dynamically, useful for audit logging where the payload structure varies.

```csharp
public class AuditLogHandler : EventHandlerBase
{
    public override async Task HandleDynamic(object @event, CancellationToken cancellationToken)
    {
        if (@event is null) return;

        var eventType = @event.GetType();
        // Perform generic logging logic based on reflection or dynamic casting
        await LogAsync(eventType.Name, @event, cancellationToken);
    }

    public override bool CanHandle(Type eventType)
    {
        // Support any event implementing IAuditable
        return typeof(IAuditable).IsAssignableFrom(eventType);
    }

    public override IEnumerable<Type> GetSupportedEventTypes()
    {
        // In a dynamic scenario, this might return a broad set or rely on CanHandle
        yield return typeof(UserRegisteredEvent);
        yield return typeof(PaymentProcessedEvent);
    }
}
```

## Notes

*   **Thread Safety**: The base class does not enforce internal synchronization. Implementations of `Handle`, `HandleDynamic`, and related methods must be thread-safe if the event bus invokes the same handler instance concurrently for different events. State maintained within the handler instance should be protected or avoided in favor of stateless designs.
*   **Cancellation**: All asynchronous handling methods accept a `CancellationToken`. Implementations must honor this token by passing it to awaited asynchronous operations and checking `IsCancellationRequested` during long-running synchronous blocks to ensure timely termination.
*   **Type Consistency**: When overriding `CanHandle` and `GetSupportedEventTypes`, ensure logical consistency. If `GetSupportedEventTypes` returns a specific type, `CanHandle` should return `true` for that type. Inconsistencies here may lead to routing errors where the bus believes a handler supports an event it subsequently rejects.
*   **Dynamic Casting**: In `HandleDynamic`, the implementer is responsible for safely casting the `object` parameter to the expected concrete type. Failure to validate the type before casting will result in an `InvalidCastException` at runtime.
