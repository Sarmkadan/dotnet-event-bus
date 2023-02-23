# DotnetEventBus

A high-performance, in-process and distributed event bus for .NET with support for pub/sub, request/reply, dead letter queue, and polymorphic handlers.

## Features

- **In-Process Event Bus**: Publish and subscribe to events within the same application
- **Distributed Messaging**: Support for distributed event bus through configurable transports
- **Pub/Sub Pattern**: Flexible publisher-subscriber messaging
- **Request/Reply Pattern**: Synchronous request-response communication
- **Dead Letter Queue**: Automatic handling of failed messages with retry policies
- **Polymorphic Handlers**: Support for multiple event types through a single handler
- **Retry Policies**: Configurable exponential backoff retry mechanism
- **Handler Priorities**: Execute handlers in a defined order
- **Concurrent Processing**: Parallel handler execution with configurable limits
- **Message Tracking**: Built-in message history and correlation IDs
- **Async/Await Support**: Fully async handler implementations
- **Exception Handling**: Customizable exception handling and logging

## Installation

```bash
dotnet add package DotnetEventBus
```

## Quick Start

### Basic Setup

```csharp
using Microsoft.Extensions.DependencyInjection;
using DotnetEventBus;
using DotnetEventBus.Services;

var services = new ServiceCollection();

// Add the event bus
services.AddEventBus(options =>
{
    options.MaxRetryAttempts = 3;
    options.AllowParallelHandling = true;
});

var serviceProvider = services.BuildServiceProvider();
var eventBus = serviceProvider.GetRequiredService<IEventBus>();
```

### Publishing Events

```csharp
// Define an event
public class OrderCreatedEvent
{
    public string OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Publish the event
var result = await eventBus.PublishAsync(
    new OrderCreatedEvent { OrderId = "123", CreatedAt = DateTime.UtcNow }
);

Console.WriteLine($"Published: {result.HandlersInvoked} handlers invoked");
```

### Subscribing to Events

```csharp
// Option 1: Using a handler class
public class OrderCreatedHandler : EventHandlerBase<OrderCreatedEvent>
{
    public override async Task Handle(OrderCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Order created: {@event.OrderId}");
        await Task.CompletedTask;
    }
}

// Option 2: Using a delegate
eventBus.Subscribe<OrderCreatedEvent>(
    async (@event, ct) =>
    {
        Console.WriteLine($"Order created: {@event.OrderId}");
        await Task.CompletedTask;
    },
    handlerName: "LogOrderCreated"
);

// Option 3: Synchronous handler
eventBus.SubscribeSync<OrderCreatedEvent>(
    @event =>
    {
        Console.WriteLine($"Order created: {@event.OrderId}");
    },
    handlerName: "SyncOrderHandler"
);
```

### Dead Letter Queue

```csharp
var deadLetterService = serviceProvider.GetRequiredService<IDeadLetterService>();

// Get pending dead letter entries
var pendingEntries = await deadLetterService.GetPendingEntriesAsync();

// Reprocess a failed message
await deadLetterService.ReprocessEntryAsync(entryId);

// Get statistics
var stats = await deadLetterService.GetStatisticsAsync();
Console.WriteLine($"Pending: {stats.PendingEntries}, Reprocessed: {stats.ReprocessedEntries}");
```

### Subscription Management

```csharp
var subscriptionManager = serviceProvider.GetRequiredService<ISubscriptionManager>();

// Get subscriptions for an event type
var subs = await subscriptionManager.GetSubscriptionsAsync("OrderCreatedEvent");

// Disable/enable handlers
await subscriptionManager.DisableHandlerAsync("OrderCreatedHandler");
await subscriptionManager.EnableHandlerAsync("OrderCreatedHandler");

// Get statistics
var stats = await subscriptionManager.GetStatisticsAsync();
```

## Advanced Configuration

### Using EventBusBuilder

```csharp
var services = new ServiceCollection();

services
    .AddEventBusBuilder()
    .WithMaxRetries(5)
    .WithHandlerTimeout(TimeSpan.FromSeconds(60))
    .WithParallelHandling(true)
    .WithMaxConcurrentHandlers(10)
    .WithDeadLetterQueue(true)
    .Build();
```

### Custom Repositories

```csharp
var messageRepo = new YourCustomEventMessageRepository();
var subscriptionRepo = new YourCustomSubscriptionRepository();
var deadLetterRepo = new YourCustomDeadLetterRepository();

services.AddEventBus(
    messageRepo,
    subscriptionRepo,
    deadLetterRepo,
    options =>
    {
        options.MaxRetryAttempts = 3;
    }
);
```

### Handler Priorities

```csharp
// Higher priority handlers execute first
eventBus.Subscribe<OrderCreatedEvent>(
    handler1,
    handlerName: "CriticalHandler",
    priority: 10  // Executes first
);

eventBus.Subscribe<OrderCreatedEvent>(
    handler2,
    handlerName: "NormalHandler",
    priority: 0   // Executes second
);
```

## Architecture

- **Models**: Core domain objects (EventMessage, Subscription, DeadLetterEntry)
- **Handlers**: Event handler interfaces and base classes
- **Repositories**: Data access layer for persisting messages and subscriptions
- **Services**: Business logic for event bus, dead letter management, and subscriptions
- **Configuration**: DI setup and options

## Project Structure

```
src/DotnetEventBus/
├── Configuration/         # DI and configuration
├── Exceptions/           # Custom exception types
├── Handlers/             # Handler interfaces and base classes
├── Models/               # Domain models
├── Repositories/         # Data access layer
├── Services/             # Business logic services
└── Constants.cs          # Constants and enums
```

## Performance Considerations

- Use `AllowParallelHandling: true` for independent handlers
- Set appropriate `MaxConcurrentHandlers` based on CPU count
- Configure `DefaultHandlerTimeout` based on handler complexity
- Use `EnableDeadLetterQueue: true` for reliability

## License

MIT License - Copyright 2026 Vladyslav Zaiets

See LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## Author

**Vladyslav Zaiets**  
CTO & Software Architect  
https://sarmkadan.com
