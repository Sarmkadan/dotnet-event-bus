# Getting Started with DotnetEventBus

This guide will help you get up and running with DotnetEventBus in just a few minutes.

## Prerequisites

- .NET 10.0 SDK or later
- C# 13.0 or later
- Basic understanding of pub-sub messaging patterns

## Installation

### Via NuGet Package Manager

```bash
dotnet add package DotnetEventBus
```

### Via .csproj

Add the following to your `.csproj` file:

```xml
<ItemGroup>
    <PackageReference Include="DotnetEventBus" Version="1.2.0" />
</ItemGroup>
```

Then run:

```bash
dotnet restore
```

### From Source

Clone the repository and build locally:

```bash
git clone https://github.com/Sarmkadan/dotnet-event-bus.git
cd dotnet-event-bus
dotnet build -c Release
```

## 5-Minute Quick Start

### 1. Define Your Events

```csharp
namespace MyApp.Events;

public class UserCreatedEvent
{
    public string UserId { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class WelcomeEmailSentEvent
{
    public string UserId { get; set; }
    public string Email { get; set; }
    public DateTime SentAt { get; set; }
}
```

### 2. Configure the Event Bus

```csharp
using Microsoft.Extensions.DependencyInjection;
using DotnetEventBus;

// Create service collection
var services = new ServiceCollection();

// Add event bus
services.AddEventBus(options =>
{
    options.MaxRetryAttempts = 3;
    options.AllowParallelHandling = true;
    options.EnableDeadLetterQueue = true;
});

// Build provider
var serviceProvider = services.BuildServiceProvider();
var eventBus = serviceProvider.GetRequiredService<IEventBus>();
```

### 3. Create Event Handlers

```csharp
using DotnetEventBus.Handlers;
using Microsoft.Extensions.Logging;

namespace MyApp.Handlers;

// Handler option 1: Class-based
public class SendWelcomeEmailHandler : EventHandlerBase<UserCreatedEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendWelcomeEmailHandler> _logger;

    public SendWelcomeEmailHandler(
        IEmailService emailService,
        ILogger<SendWelcomeEmailHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public override async Task Handle(
        UserCreatedEvent @event,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Sending welcome email to {0}", @event.Email);
        
        await _emailService.SendWelcomeEmailAsync(
            @event.Email,
            @event.FullName,
            cancellationToken);
    }
}

// Handler option 2: Delegate
eventBus.Subscribe<UserCreatedEvent>(
    async (@event, ct) =>
    {
        Console.WriteLine($"User registered: {0}", @event.FullName);
        await Task.CompletedTask;
    },
    handlerName: "LogUserRegistration"
);
```

### 4. Publish Events

```csharp
var newUser = new UserCreatedEvent
{
    UserId = "user-123",
    Email = "john@example.com",
    FullName = "John Doe",
    CreatedAt = DateTime.UtcNow
};

var result = await eventBus.PublishAsync(newUser);

Console.WriteLine($"Event published to {0} handlers", result.HandlersInvoked);
```

## 4. Publish Events

```csharp
var newUser = new UserCreatedEvent
{
    UserId = "user-123",
    Email = "john@example.com",
    FullName = "John Doe",
    CreatedAt = DateTime.UtcNow
};

var result = await eventBus.PublishAsync(newUser);

Console.WriteLine($"Event published to {0} handlers", result.HandlersInvoked);
```

## In-Process vs. Distributed Mode

DotnetEventBus supports both in-process and distributed event processing. Understanding the differences is crucial for choosing the right mode for your application.

### In-Process Mode

**When to use:** Ideal for monolithic applications or microservices where events are consumed within the same application domain. Offers high performance and low latency as events are passed directly in memory.

**Characteristics:**
- Events are handled synchronously or asynchronously within the same application process.
- No external message broker is required.
- Simplified setup and debugging.

**Configuration:** This is the default mode. No special configuration is required beyond `services.AddEventBus()`.

**Handler Registration:** Handlers are registered directly with the `IEventBus` instance, as shown in the "Create Event Handlers" section.

### Distributed Mode

**When to use:** Essential for communication between separate services or applications (e.g., microservices architecture). Provides resilience, scalability, and loose coupling between producers and consumers.

**Characteristics:**
- Requires an external message broker (e.g., RabbitMQ, Kafka, Azure Service Bus).
- Events are serialized and transported over the network.
- Supports features like durable messaging, competing consumers, and message replay depending on the broker.

**Configuration:** To enable distributed mode, configure `EventBusOptions` during service registration:

```csharp
services.AddEventBus(options =>
{
    options.IsDistributed = true;
    options.DistributedTransportType = "RabbitMQ"; // Example: "RabbitMQ", "Kafka", etc.
    options.DistributedTransportConnectionString = "host=localhost"; // Connection string for your broker
    // Other options like MaxRetryAttempts, EnableDeadLetterQueue are also applicable
});
```
**Important:** While DotnetEventBus provides the framework for distributed event handling (e.g., `ProcessRawDistributedEventAsync`), it does not directly implement distributed transports. You will need to integrate with a specific message broker library (e.g., MassTransit, NServiceBus) to handle the actual sending and receiving of messages over the network.

**Processing Raw Distributed Events:** When an external message broker delivers a message, you'll receive it as a raw payload (e.g., a string or byte array) and an event type identifier. Use `IEventBus.ProcessRawDistributedEventAsync` to deserialize and publish this event. This method also handles deserialization failures by routing malformed messages to the Dead Letter Queue.

```csharp
// Example of processing a raw message received from a distributed transport
public async Task ProcessMessageFromBroker(
    string eventType,
    string rawPayload,
    string? correlationId,
    IEventBus eventBus,
    CancellationToken cancellationToken)
{
    Console.WriteLine($"Received raw distributed event of type {eventType}");
    var result = await eventBus.ProcessRawDistributedEventAsync(
        eventType,
        rawPayload,
        correlationId,
        cancellationToken);

    if (!result.Success)
    {
        Console.WriteLine($"Failed to process distributed event: {result.Exception?.Message}");
        // Further handling for permanent failures after dead-lettering, if necessary
    }
}
```

## Common Patterns

### Pattern 1: Registration Workflow

```csharp
// Event definitions
public class RegistrationInitiatedEvent
{
    public string Email { get; set; }
    public string Username { get; set; }
}

public class VerificationEmailSentEvent
{
    public string Email { get; set; }
    public string VerificationCode { get; set; }
}

public class RegistrationCompletedEvent
{
    public string Email { get; set; }
    public string UserId { get; set; }
}

// Handlers
public class SendVerificationEmailHandler : EventHandlerBase<RegistrationInitiatedEvent>
{
    private readonly IEmailService _email;
    private readonly IEventBus _eventBus;

    public override async Task Handle(RegistrationInitiatedEvent @event, CancellationToken ct)
    {
        var code = GenerateVerificationCode();
        
        await _email.SendAsync(
            to: @event.Email,
            subject: "Verify Your Email",
            body: $"Code: {code}",
            cancellationToken: ct);

        await _eventBus.PublishAsync(
            new VerificationEmailSentEvent
            {
                Email = @event.Email,
                VerificationCode = code
            },
            ct);
    }
}

// Usage
var registration = new RegistrationInitiatedEvent
{
    Email = "user@example.com",
    Username = "johndoe"
};

await eventBus.PublishAsync(registration);
```

### Pattern 2: Error Handling with Dead Letter Queue

```csharp
public class ProcessPaymentHandler : EventHandlerBase<PaymentInitiatedEvent>
{
    private readonly IPaymentGateway _gateway;

    public override async Task Handle(PaymentInitiatedEvent @event, CancellationToken ct)
    {
        try
        {
            var result = await _gateway.ProcessAsync(@event.Amount, ct);
            
            if (!result.IsSuccessful)
                throw new InvalidOperationException($"Payment failed: {result.Error}");
        }
        catch (TimeoutException ex)
        {
            // Will be retried automatically
            throw;
        }
        catch (Exception ex)
        {
            // Will go to dead letter queue
            throw;
        }
    }
}

// Check dead letter queue
var dlq = serviceProvider.GetRequiredService<IDeadLetterService>();
var failed = await dlq.GetPendingEntriesAsync();

foreach (var entry in failed)
{
    Console.WriteLine($"Failed: {entry.EventType}");
    Console.WriteLine($"Attempts: {entry.RetryCount}");
    Console.WriteLine($"Last Error: {entry.LastException}");
}

// Retry a specific entry
await dlq.ReprocessEntryAsync(failed.First().Id);
```

### Pattern 3: Multiple Handlers with Priority

```csharp
// High priority - critical operations
eventBus.Subscribe<OrderCreatedEvent>(
    async (@event, ct) => await ValidateInventory(@event, ct),
    handlerName: "InventoryValidator",
    priority: 100
);

// Normal priority
eventBus.Subscribe<OrderCreatedEvent>(
    async (@event, ct) => await SendConfirmationEmail(@event, ct),
    handlerName: "EmailNotification",
    priority: 50
);

// Low priority - optional operations
eventBus.Subscribe<OrderCreatedEvent>(
    async (@event, ct) => await UpdateAnalytics(@event, ct),
    handlerName: "AnalyticsTracker",
    priority: 10
);

// Handlers execute in order: InventoryValidator → EmailNotification → AnalyticsTracker
```

## Pattern 4: Polymorphic Handler Resolution

DotnetEventBus automatically supports polymorphic event handling, meaning that handlers registered for a base event type will also receive events of its derived types. When an event is published, the bus identifies all handlers subscribed to the exact event type, as well as handlers subscribed to any of its base classes or interfaces.

To prevent duplicate processing, the system ensures that each unique handler instance is invoked only once per event. If a handler is subscribed to both a base type and a derived type of the same event, it will only be invoked once.

**Example:**

Consider the following event hierarchy and handlers:

```csharp
// Event Definitions
public class OrderEvent { }
public class OrderCreatedEvent : OrderEvent { }
public class OrderCancelledEvent : OrderEvent { }

// Handlers
public class BaseOrderHandler : EventHandlerBase<OrderEvent>
{
    private readonly ILogger<BaseOrderHandler> _logger;
    public BaseOrderHandler(ILogger<BaseOrderHandler> logger) => _logger = logger;
    public override Task Handle(OrderEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Base handler processed OrderEvent (or derived): {@event.GetType().Name}");
        return Task.CompletedTask;
    }
}

public class CreatedOrderHandler : EventHandlerBase<OrderCreatedEvent>
{
    private readonly ILogger<CreatedOrderHandler> _logger;
    public CreatedOrderHandler(ILogger<CreatedOrderHandler> logger) => _logger = logger;
    public override Task Handle(OrderCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Derived handler processed OrderCreatedEvent: {@event.GetType().Name}");
        return Task.CompletedTask;
    }
}

// Registration
// services.AddTransient<BaseOrderHandler>();
// services.AddTransient<CreatedOrderHandler>();
// eventBus.Subscribe<OrderEvent>(serviceProvider.GetRequiredService<BaseOrderHandler>());
// eventBus.Subscribe<OrderCreatedEvent>(serviceProvider.GetRequiredService<CreatedOrderHandler>());

// Publishing an OrderCreatedEvent
await eventBus.PublishAsync(new OrderCreatedEvent());
// Expected Output:
// Base handler processed OrderEvent (or derived): OrderCreatedEvent
// Derived handler processed OrderCreatedEvent: OrderCreatedEvent

// Note: Even if BaseOrderHandler was explicitly subscribed to OrderCreatedEvent too,
// it would still only be invoked once due to duplicate prevention logic.
```

## Testing

### Unit Testing Handlers

```csharp
using Xunit;
using Moq;

public class SendWelcomeEmailHandlerTests
{
    [Fact]
    public async Task Handle_SendsEmailWhenUserCreated()
    {
        // Arrange
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<SendWelcomeEmailHandler>>();
        
        var handler = new SendWelcomeEmailHandler(
            mockEmailService.Object,
            mockLogger.Object);

        var @event = new UserCreatedEvent
        {
            UserId = "user-1",
            Email = "test@example.com",
            FullName = "Test User",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        mockEmailService.Verify(
            x => x.SendWelcomeEmailAsync("test@example.com", "Test User", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

### Integration Testing Event Bus

```csharp
public class EventBusIntegrationTests
{
    [Fact]
    public async Task PublishAsync_InvokesAllSubscribedHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEventBus();
        var provider = services.BuildServiceProvider();
        var eventBus = provider.GetRequiredService<IEventBus>();

        var handlerInvoked = false;
        
        eventBus.Subscribe<TestEvent>(
            async (@event, ct) =>
            {
                handlerInvoked = true;
                await Task.CompletedTask;
            },
            handlerName: "TestHandler");

        // Act
        var result = await eventBus.PublishAsync(new TestEvent());

        // Assert
        Assert.True(handlerInvoked);
        Assert.Equal(1, result.HandlersInvoked);
    }
}
```

## Next Steps

1. **Explore Examples**: Check the `/examples` directory for complete sample applications
2. **Read Architecture Guide**: See `docs/architecture.md` for detailed system design
3. **API Reference**: Review `docs/api-reference.md` for complete API documentation
4. **Deployment Guide**: Check `docs/deployment.md` for production deployment strategies
5. **FAQ**: See `docs/faq.md` for common questions and troubleshooting

## Common Pitfalls and Best Practices

To ensure a smooth experience with DotnetEventBus, be aware of these common pitfalls and follow the recommended best practices.

### 1. Missing Handler Registration

**Pitfall:** Events are published but no handlers are invoked, or only some handlers are invoked.
**Cause:** Handlers are not correctly registered with the event bus, or they are registered for an incorrect event type.
**Best Practice:**
- Always ensure your handler classes are registered in your DI container and then subscribed to the event bus using `eventBus.Subscribe()`.
- Verify the `TEvent` type used in `IEventHandler<TEvent>` matches the event you intend to handle.
- For polymorphic handling, remember that while a base handler can receive derived events, it still needs to be registered for the base type or an appropriate derived type.

### 2. Serialization Issues (Distributed Events)

**Pitfall:** Distributed events fail to be processed, often leading to unhandled exceptions or messages stuck in queues.
**Cause:** The raw payload of a distributed event cannot be deserialized into the expected `TEvent` type. This can be due to schema mismatches, missing type information, or malformed JSON/XML.
**Best Practice:**
- Ensure that your event types are designed for serialization (e.g., public properties with getters and setters, parameterless constructors if using certain serializers).
- For distributed events, always use `IEventBus.ProcessRawDistributedEventAsync`. This method provides robust error handling by automatically routing events that fail deserialization to the Dead Letter Queue, allowing for inspection and reprocessing.
- Consider versioning your event schemas to manage changes gracefully.

### 3. Request/Reply Timeouts

**Pitfall:** `SendAsync` calls time out prematurely or wait unnecessarily long for responses.
**Cause:** The default global timeout (`EventBusOptions.RequestTimeout`) is not suitable for all request types, or the timeout is not being correctly applied.
**Best Practice:**
- Configure `EventBusOptions.RequestTimeout` to a sensible default for most of your request/reply operations.
- For specific request types that require a different timeout, use the `[RequestTimeout(milliseconds)]` attribute on the request class:
  ```csharp
  [RequestTimeout(5000)] // 5 seconds
  public class MySlowRequest { /* ... */ }
  ```
- Alternatively, you can override the timeout directly in the `SendAsync` call:
  ```csharp
  await eventBus.SendAsync<MyRequest, MyResponse>(request, timeout: TimeSpan.FromSeconds(10));
  ```
- Ensure your underlying distributed transport (if used for request/reply) also respects and propagates these timeouts.

### 4. Asynchronous Handler Considerations

**Pitfall:** Handlers that perform long-running or external operations block the event bus, leading to performance bottlenecks.
**Cause:** Synchronous handlers or poorly managed asynchronous handlers can tie up threads.
**Best Practice:**
- Favor asynchronous handlers (`Task Handle(...)`) for I/O-bound operations (database calls, API requests).
- Use `await` appropriately to release control back to the event bus, allowing other events to be processed concurrently.
- Avoid `Task.Wait()` or `Task.Result` in asynchronous handlers, as this can lead to deadlocks.



## Getting Help

- **Issues**: Open an issue on [GitHub](https://github.com/Sarmkadan/dotnet-event-bus/issues)
- **Discussions**: Join [GitHub Discussions](https://github.com/Sarmkadan/dotnet-event-bus/discussions)
- **Contact**: Reach out to [@Sarmkadan](https://t.me/sarmkadan) on Telegram

## Resources

- [GitHub Repository](https://github.com/Sarmkadan/dotnet-event-bus)
- [NuGet Package](https://www.nuget.org/packages/DotnetEventBus)
- [Author Portfolio](https://sarmkadan.com)
