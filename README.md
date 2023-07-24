// ... (rest of the README.md content remains the same)

## EventBusException

The `EventBusException` class serves as the base exception type for all event bus-related errors. It inherits from `System.Exception` and provides constructors for creating exception instances with custom messages and inner exceptions. This exception type is the parent class for specific event bus exceptions like `NoHandlersRegisteredException`, `HandlerInvocationException`, `InvalidHandlerException`, `MessageSerializationException`, `DistributedBusNotConfiguredException`, and `RequestTimeoutException`.

Example usage:
```csharp
try
{
    await eventBus.PublishAsync(new OrderCreatedEvent(orderId: 123));
}
catch (EventBusException ex)
{
    // Handle generic event bus exceptions
    Console.WriteLine($"Event bus error: {ex.Message}");
    if (ex is NoHandlersRegisteredException noHandlersEx)
    {
        Console.WriteLine($"No handlers for event type: {noHandlersEx.EventType}");
    }
    else if (ex is HandlerInvocationException handlerEx)
    {
        Console.WriteLine($"Handler '{handlerEx.HandlerName}' failed for event '{handlerEx.EventType}': {handlerEx.Message}");
    }
    else if (ex is RequestTimeoutException timeoutEx)
    {
        Console.WriteLine($"Request '{timeoutEx.RequestType}' timed out after {timeoutEx.Timeout.TotalSeconds} seconds");
    }
}
```

## EventBusBuilder

The `EventBusBuilder` class is a fluent builder for configuring and creating the event bus. It allows you to customize various settings, such as event message repositories, subscription repositories, dead letter repositories, and distributed event bus settings.

Example usage:
```csharp
var services = new ServiceCollection();
var eventBusBuilder = services.AddEventBusBuilder()
    .WithOptions(options => options.DefaultHandlerTimeout = TimeSpan.FromSeconds(30))
    .WithMessageRepository(new InMemoryEventMessageRepository())
    .WithSubscriptionRepository(new InMemorySubscriptionRepository())
    .WithDeadLetterRepository(new InMemoryDeadLetterRepository())
    .WithMaxRetries(5)
    .WithParallelHandling(true)
    .WithMaxConcurrentHandlers(10)
    .WithDeadLetterQueue(true)
    .WithThrowOnHandlerFailure(true)
    .AsDistributed("rabbitmq", "amqp://guest:guest@localhost:5672")
    .Build();

var serviceProvider = services.BuildServiceProvider();
var eventBus = serviceProvider.GetService<IEventBus>();
```

## EventHandlerBase

The `EventHandlerBase<TEvent>` class is an abstract base class that provides common functionality for implementing event handlers with built-in logging support. It implements the `IEventHandler<TEvent>` interface and provides virtual methods for type introspection (`GetEventType()`), handler naming (`GetHandlerName()`), and lifecycle hooks for before/after handling and error handling.

Example usage:
```csharp
public class OrderCreatedEvent : IEvent
{
    public int OrderId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class OrderCreatedHandler : EventHandlerBase<OrderCreatedEvent>
{
    public OrderCreatedHandler(ILogger<OrderCreatedHandler> logger) : base(logger)
    {
    }

    public override async Task Handle(OrderCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        // Your handler logic here
        Logger?.LogInformation("Processing order {OrderId}", @event.OrderId);
        
        // Example: Save to database, send notifications, etc.
        await Task.Delay(100, cancellationToken);
    }
}

// Registration in DI container
services.AddTransient<IEventHandler<OrderCreatedEvent>, OrderCreatedHandler>();

// Usage with event bus
var orderCreatedEvent = new OrderCreatedEvent { OrderId = 123 };
await eventBus.PublishAsync(orderCreatedEvent);
```
