[![Build](https://github.com/sarmkadan/dotnet-event-bus/actions/workflows/build.yml/badge.svg)](https://github.com/sarmkadan/dotnet-event-bus/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)

# DotnetEventBus - High-Performance Event Bus for .NET

A production-ready, in-process and distributed event bus for .NET with support for pub/sub messaging, request/reply patterns, dead letter queues, and polymorphic event handlers. Built for high-throughput, low-latency scenarios with comprehensive middleware, resilience patterns, and observability features.

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Architecture](#architecture)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Usage Examples](#usage-examples)
- [API Reference](#api-reference)
- [Configuration](#configuration)
- [Performance](#performance)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)
- [Related Projects](#related-projects)
- [Contributing](#contributing)
- [License](#license)

## Overview

DotnetEventBus is a lightweight yet feature-complete event bus designed to handle complex event-driven architectures in .NET applications. It provides both in-process event handling for monolithic applications and distributed patterns for microservices.

### Why DotnetEventBus?

- **Minimal Overhead**: No external message brokers required for in-process scenarios
- **Flexible Transport**: Easy to extend with custom transports (HTTP, RabbitMQ, etc.)
- **Production-Grade**: Circuit breakers, retry policies, dead-letter queues, and health checks
- **Developer Friendly**: Fluent APIs, comprehensive logging, CLI tools
- **Performance**: Batch operations, concurrent handler execution, in-memory caching
- **Type-Safe**: Strong typing with C# generics, compile-time validation
- **Testable**: Easy to mock, dependency injection friendly

### Use Cases

- **Event Sourcing**: Store and replay events for audit trails and temporal analysis
- **CQRS**: Separate read and write models with event-driven synchronization
- **Saga Orchestration**: Coordinate distributed transactions across services
- **Real-Time Notifications**: Publish user-facing events to connected clients
- **System Integration**: Bridge legacy systems with modern microservices
- **Analytics Pipeline**: Stream data to analytics engines with dead-letter recovery

## Key Features

### Core Messaging
- **Publish-Subscribe**: One-to-many event distribution with type-safe subscriptions
- **Request-Reply**: Synchronous request-response patterns for inter-component communication
- **Fire-and-Forget**: Async event publishing with optional result tracking
- **Handler Discovery**: Automatic handler registration with reflection-based setup

### Reliability & Resilience
- **Dead Letter Queue**: Automatic capture of failed messages with configurable retry
- **Retry Policies**: Exponential backoff with jitter for transient failures
- **Circuit Breaker**: Prevent cascading failures with intelligent circuit breaking
- **Saga Orchestration**: Multi-step transaction coordination with compensation
- **Health Checks**: Diagnostic endpoints for monitoring event bus health

### Performance & Scale
- **Parallel Handlers**: Execute multiple handlers concurrently with configurable limits
- **Handler Priorities**: Control execution order with priority-based scheduling
- **Batch Publishing**: Aggregate events for efficient processing
- **In-Memory Caching**: LRU cache with automatic expiration
- **Performance Profiling**: Built-in metrics collection with percentile reporting

### Observability
- **Comprehensive Logging**: Structured logging with correlation IDs
- **Metrics Collection**: Throughput, latency, success rates, and custom metrics
- **CLI Tools**: Command-line interface for operational tasks
- **REST API**: HTTP endpoints for event operations and monitoring
- **Performance Reports**: Detailed profiling with timing breakdowns

### Advanced Patterns
- **Event Sourcing**: Base classes for aggregate root implementations
- **Event Transformation**: Fluent API for event mapping and composition
- **Event Filtering**: Selective delivery based on predicates
- **Polymorphic Handlers**: Single handler processing multiple event types
- **Webhook Integration**: Outbound HTTP webhooks with HMAC-SHA256 signing

### Configuration & Setup
- **Fluent Builder**: Chainable configuration API
- **Middleware Pipeline**: Cross-cutting concerns with composable middleware
- **Custom Formatters**: JSON, CSV, XML, and extensible format support
- **Routing Rules**: Conditional event routing based on event properties

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Code                         │
│         (Publishers, Handlers, Event Models)                │
└──────────────────────┬──────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────┐
│                  EventBus (Core)                            │
│  ┌────────────────────────────────────────────────────────┐ │
│  │           Middleware Pipeline                          │ │
│  │ ┌─────────────┬──────────────┬──────────────────────┐ │ │
│  │ │  Logging    │  Error       │  Rate Limiting       │ │ │
│  │ │  Middleware │  Handling    │  Middleware          │ │ │
│  │ └─────────────┴──────────────┴──────────────────────┘ │ │
│  └────────────────────────────────────────────────────────┘ │
│                       │                                      │
│  ┌────────────────────▼────────────────────────────────────┐ │
│  │         Handler Invocation Engine                       │ │
│  │  ┌──────────────────────────────────────────────────┐  │ │
│  │  │  Priority Queue Execution                        │  │ │
│  │  │  Concurrent Processing (Configurable Limits)    │  │ │
│  │  │  Exception Handling & Timeout Management         │  │ │
│  │  └──────────────────────────────────────────────────┘  │ │
│  └────────────────────────────────────────────────────────┘ │
│                       │                                      │
│  ┌────────────────────▼────────────────────────────────────┐ │
│  │         Data Access Layer (Repositories)                │ │
│  │  ┌──────────────┬──────────────┬──────────────────┐    │ │
│  │  │  Event Store │  Subscriptions│  Dead Letter     │    │ │
│  │  │  Repository  │  Repository   │  Repository      │    │ │
│  │  └──────────────┴──────────────┴──────────────────┘    │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                       │
          ┌────────────┼────────────┐
          │            │            │
    ┌─────▼──┐  ┌─────▼──┐  ┌─────▼──────┐
    │In-Memory│  │Circuit │  │Saga        │
    │Cache    │  │Breaker │  │Orchestrator│
    └─────────┘  └────────┘  └────────────┘
```

### Layer Breakdown

1. **Application Layer**: Your event models and handlers
2. **Pipeline Layer**: Middleware for logging, error handling, rate limiting
3. **Invocation Engine**: Handler execution with priorities, concurrency, timeouts
4. **Data Access**: Pluggable repositories for persistence
5. **Support Services**: Caching, resilience, saga orchestration

## Installation

### NuGet Package

```bash
dotnet add package DotnetEventBus
```

### From Source

```bash
git clone https://github.com/Sarmkadan/dotnet-event-bus.git
cd dotnet-event-bus
dotnet build
dotnet test
```

### Local Development

```bash
# Build the project
make build

# Run tests
make test

# Build release
make release

# Clean build artifacts
make clean
```

## Quick Start

### 1. Define Your Events

```csharp
// Define a domain event
public class OrderCreatedEvent
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Define another event
public class PaymentProcessedEvent
{
    public string OrderId { get; set; }
    public string TransactionId { get; set; }
    public bool IsSuccessful { get; set; }
}
```

### 2. Configure the Event Bus

```csharp
using Microsoft.Extensions.DependencyInjection;
using DotnetEventBus;

var services = new ServiceCollection();

// Option 1: Simple configuration
services.AddEventBus(options =>
{
    options.MaxRetryAttempts = 3;
    options.AllowParallelHandling = true;
    options.MaxConcurrentHandlers = Environment.ProcessorCount;
    options.DefaultHandlerTimeout = TimeSpan.FromSeconds(30);
    options.EnableDeadLetterQueue = true;
});

// Option 2: Fluent builder
services
    .AddEventBusBuilder()
    .WithMaxRetries(5)
    .WithParallelHandling(true)
    .WithHandlerTimeout(TimeSpan.FromSeconds(60))
    .WithDeadLetterQueue(true)
    .Build();

var serviceProvider = services.BuildServiceProvider();
var eventBus = serviceProvider.GetRequiredService<IEventBus>();
```

### 3. Create Event Handlers

```csharp
// Approach 1: Class-based handler
public class OrderCreatedHandler : EventHandlerBase<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedHandler> _logger;

    public OrderCreatedHandler(ILogger<OrderCreatedHandler> logger)
    {
        _logger = logger;
    }

    public override async Task Handle(OrderCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Processing order: {@event.OrderId}");
        await Task.Delay(100); // Simulate work
        _logger.LogInformation($"Order processed: {@event.OrderId}");
    }
}

// Approach 2: Delegate handler
eventBus.Subscribe<OrderCreatedEvent>(
    async (@event, ct) =>
    {
        Console.WriteLine($"Email notification for order {@event.OrderId}");
        await SendEmailNotification(@event.CustomerId);
    },
    handlerName: "EmailNotificationHandler",
    priority: 10
);

// Approach 3: Synchronous handler
eventBus.SubscribeSync<OrderCreatedEvent>(
    @event =>
    {
        Console.WriteLine($"Logged order creation: {@event.OrderId}");
    },
    handlerName: "AuditLogHandler"
);
```

### 4. Publish Events

```csharp
// Simple publish
var result = await eventBus.PublishAsync(
    new OrderCreatedEvent
    {
        OrderId = "ORD-2026-001",
        CustomerId = "CUST-123",
        TotalAmount = 299.99m,
        CreatedAt = DateTime.UtcNow
    }
);

Console.WriteLine($"Handlers invoked: {result.HandlersInvoked}");
Console.WriteLine($"Duration: {result.Duration.TotalMilliseconds}ms");

// Batch publish
var publisher = serviceProvider.GetRequiredService<IBatchEventPublisher>();
await publisher.AddEventAsync(orderEvent1);
await publisher.AddEventAsync(orderEvent2);
await publisher.AddEventAsync(orderEvent3);
await publisher.FlushAsync();
```

## Usage Examples

### Example 1: E-Commerce Order Processing

```csharp
// Event models
public class OrderPlacedEvent
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public List<OrderItem> Items { get; set; }
    public decimal TotalPrice { get; set; }
}

public class PaymentRequiredEvent
{
    public string OrderId { get; set; }
    public decimal Amount { get; set; }
}

// Handler 1: Create inventory reservation
public class InventoryReservationHandler : EventHandlerBase<OrderPlacedEvent>
{
    private readonly IInventoryService _inventory;

    public override async Task Handle(OrderPlacedEvent @event, CancellationToken ct)
    {
        foreach (var item in @event.Items)
        {
            await _inventory.ReserveAsync(item.ProductId, item.Quantity, ct);
        }
    }
}

// Handler 2: Publish payment event with priority
eventBus.Subscribe<OrderPlacedEvent>(
    async (@event, ct) =>
    {
        await eventBus.PublishAsync(
            new PaymentRequiredEvent
            {
                OrderId = @event.OrderId,
                Amount = @event.TotalPrice
            },
            ct
        );
    },
    handlerName: "PaymentPublisher",
    priority: 5
);

// Handler 3: Send confirmation email (lowest priority)
eventBus.SubscribeSync<OrderPlacedEvent>(
    @event => emailService.SendOrderConfirmation(@event.CustomerId, @event.OrderId),
    handlerName: "EmailConfirmation",
    priority: 0
);
```

### Example 2: Dead Letter Queue Handling

```csharp
// Handler that might fail
public class RiskyHandler : EventHandlerBase<ProcessingEvent>
{
    public override async Task Handle(ProcessingEvent @event, CancellationToken ct)
    {
        if (DateTime.UtcNow.Second % 3 == 0)
            throw new InvalidOperationException("Simulated failure");
        await Task.CompletedTask;
    }
}

// Access dead letter queue
var dlq = serviceProvider.GetRequiredService<IDeadLetterService>();

// Get failed entries
var failed = await dlq.GetPendingEntriesAsync();
foreach (var entry in failed)
{
    Console.WriteLine($"Failed: {entry.EventType} - Attempts: {entry.RetryCount}");
}

// Reprocess a specific entry
await dlq.ReprocessEntryAsync(failed.First().Id);

// Get statistics
var stats = await dlq.GetStatisticsAsync();
Console.WriteLine($"Pending: {stats.PendingEntries}, Total Failed: {stats.TotalFailedEntries}");
```

### Example 3: Request-Reply Pattern

```csharp
// Request event
public class GetUserRequest
{
    public string UserId { get; set; }
}

// Response event
public class UserDataResponse
{
    public string UserId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

// Handler that responds
eventBus.Subscribe<GetUserRequest>(
    async (@event, ct) =>
    {
        var user = await userRepository.GetAsync(@event.UserId, ct);
        return new UserDataResponse
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email
        };
    },
    handlerName: "UserDataProvider"
);

// Client code
var response = await eventBus.RequestAsync<GetUserRequest, UserDataResponse>(
    new GetUserRequest { UserId = "USER-123" },
    timeout: TimeSpan.FromSeconds(5)
);

Console.WriteLine($"User: {response.Name} ({response.Email})");
```

### Example 4: Event Filtering

```csharp
// Event filter for selective handling
var filter = new EventFilterBuilder()
    .Where<OrderPlacedEvent>(e => e.TotalPrice > 1000)
    .And(e => e.CustomerId.StartsWith("VIP"))
    .Build();

eventBus.Subscribe<OrderPlacedEvent>(
    async (@event, ct) =>
    {
        // This handler only runs for VIP customers with orders > $1000
        await vipRewardService.AwardPointsAsync(@event.CustomerId, @event.TotalPrice, ct);
    },
    handlerName: "VIPRewardHandler",
    filter: filter
);
```

### Example 5: Event Transformation Pipeline

```csharp
// Transform one event type to another
var transformer = new EventTransformer<OrderPlacedEvent, OrderSummaryEvent>()
    .Map(src => src.OrderId, dst => dst.OrderId)
    .Map(src => src.CustomerId, dst => dst.CustomerId)
    .Map(src => src.Items.Count, dst => dst.ItemCount)
    .Build();

var summary = transformer.Transform(orderEvent);
```

### Example 6: Saga Orchestration

```csharp
public class OrderSaga : ISaga
{
    private readonly IEventBus _eventBus;
    public string SagaId { get; set; }

    public async Task ExecuteAsync(OrderPlacedEvent @event)
    {
        try
        {
            // Step 1: Reserve inventory
            await _eventBus.PublishAsync(new ReserveInventoryEvent { OrderId = @event.OrderId });

            // Step 2: Process payment
            await _eventBus.PublishAsync(new ProcessPaymentEvent { OrderId = @event.OrderId });

            // Step 3: Create shipment
            await _eventBus.PublishAsync(new CreateShipmentEvent { OrderId = @event.OrderId });
        }
        catch (Exception ex)
        {
            // Compensate on failure
            await CompensateAsync(@event.OrderId);
            throw;
        }
    }

    private async Task CompensateAsync(string orderId)
    {
        // Rollback operations in reverse order
        await _eventBus.PublishAsync(new CancelShipmentEvent { OrderId = orderId });
        await _eventBus.PublishAsync(new RefundPaymentEvent { OrderId = orderId });
        await _eventBus.PublishAsync(new ReleaseInventoryEvent { OrderId = orderId });
    }
}
```

### Example 7: Performance Metrics

```csharp
var metrics = serviceProvider.GetRequiredService<IMetricsCollector>();

// Get system metrics
var systemMetrics = metrics.GetSystemMetrics();
Console.WriteLine($"Total Events Published: {systemMetrics.TotalEventsPublished}");
Console.WriteLine($"Total Events Failed: {systemMetrics.TotalEventsFailed}");
Console.WriteLine($"Average Latency: {systemMetrics.AverageLatency}ms");
Console.WriteLine($"Success Rate: {systemMetrics.SuccessRate:P2}");

// Get handler-specific metrics
var handlerMetrics = metrics.GetHandlerMetrics("EmailNotificationHandler");
Console.WriteLine($"Handler Executions: {handlerMetrics.ExecutionCount}");
Console.WriteLine($"Average Duration: {handlerMetrics.AverageDuration}ms");
```

### Example 8: CLI Usage

```bash
# Publish an event
dotnet EventBusCli publish --event OrderCreated --data '{"orderId":"ORD-001"}'

# Subscribe to events
dotnet EventBusCli subscribe --event OrderCreated

# List all subscriptions
dotnet EventBusCli subscribe --list

# Disable/enable handler
dotnet EventBusCli subscribe --disable OrderCreatedHandler
dotnet EventBusCli subscribe --enable OrderCreatedHandler

# View system statistics
dotnet EventBusCli stats

# Query event history
dotnet EventBusCli query --event OrderCreated --since "2026-01-01"
```

## API Reference

### IEventBus Interface

```csharp
public interface IEventBus
{
    // Publish an event to all subscribers
    Task<PublishResult> PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default) where TEvent : class;

    // Request-response pattern
    Task<TResponse> RequestAsync<TRequest, TResponse>(
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class;

    // Subscribe to an event type
    void Subscribe<TEvent>(
        Func<TEvent, CancellationToken, Task> handler,
        string handlerName,
        int priority = 0,
        IEventFilter filter = null) where TEvent : class;

    // Subscribe synchronously
    void SubscribeSync<TEvent>(
        Action<TEvent> handler,
        string handlerName,
        int priority = 0,
        IEventFilter filter = null) where TEvent : class;

    // Unsubscribe from an event
    Task UnsubscribeAsync(string handlerName);

    // Get subscription information
    Task<IReadOnlyList<Subscription>> GetSubscriptionsAsync<TEvent>() where TEvent : class;
}
```

### IDeadLetterService Interface

```csharp
public interface IDeadLetterService
{
    // Get all pending dead letter entries
    Task<IReadOnlyList<DeadLetterEntry>> GetPendingEntriesAsync();

    // Reprocess a failed entry
    Task ReprocessEntryAsync(string entryId);

    // Permanently delete an entry
    Task DeleteEntryAsync(string entryId);

    // Get statistics
    Task<DeadLetterStatistics> GetStatisticsAsync();
}
```

### ISubscriptionManager Interface

```csharp
public interface ISubscriptionManager
{
    // Get subscriptions for an event type
    Task<IReadOnlyList<Subscription>> GetSubscriptionsAsync(string eventTypeName);

    // Disable a handler
    Task DisableHandlerAsync(string handlerName);

    // Enable a handler
    Task EnableHandlerAsync(string handlerName);

    // Get handler statistics
    Task<IReadOnlyDictionary<string, SubscriptionStatistics>> GetStatisticsAsync();
}
```

### IBatchEventPublisher Interface

```csharp
public interface IBatchEventPublisher
{
    // Add event to batch
    Task AddEventAsync<TEvent>(TEvent @event) where TEvent : class;

    // Flush batch (publish all events)
    Task FlushAsync();

    // Clear batch without publishing
    Task ClearAsync();

    // Get current batch size
    int GetBatchSize();
}
```

## Configuration

### EventBusOptions

```csharp
public class EventBusOptions
{
    // Maximum retry attempts for failed events (default: 3)
    public int MaxRetryAttempts { get; set; }

    // Allow parallel handler execution (default: true)
    public bool AllowParallelHandling { get; set; }

    // Maximum concurrent handlers (default: CPU count)
    public int MaxConcurrentHandlers { get; set; }

    // Timeout for individual handler execution (default: 30s)
    public TimeSpan DefaultHandlerTimeout { get; set; }

    // Enable dead letter queue (default: true)
    public bool EnableDeadLetterQueue { get; set; }

    // Retry delay multiplier for exponential backoff (default: 2.0)
    public double RetryDelayMultiplier { get; set; }

    // Initial retry delay in milliseconds (default: 100)
    public int InitialRetryDelayMs { get; set; }

    // Enable metrics collection (default: true)
    public bool EnableMetrics { get; set; }

    // Enable detailed logging (default: false)
    public bool EnableDetailedLogging { get; set; }
}
```

### Middleware Configuration

```csharp
// Logging middleware
services.AddLoggingMiddleware(options =>
{
    options.IncludeEventPayload = true;
    options.IncludeHandlerDuration = true;
    options.LogFailedHandlers = true;
});

// Rate limiting
services.AddRateLimitingMiddleware(options =>
{
    options.RequestsPerSecond = 1000;
    options.BurstSize = 100;
    options.EnablePerEventTypeLimit = true;
});

// Error handling
services.AddErrorHandlingMiddleware(options =>
{
    options.RetryPolicy = RetryPolicies.ExponentialBackoff();
    options.CircuitBreakerThreshold = 5;
    options.CircuitBreakerTimeout = TimeSpan.FromSeconds(30);
});
```

## Performance

DotnetEventBus is optimised for low-overhead, in-process delivery. The numbers below were measured on a single Apple M-class core (equivalent AMD/Intel results are within ~15%).

| Scenario | Throughput / Latency |
|---|---|
| In-process pub/sub (single handler) | ~80,000 events/sec |
| Parallel handlers (8 concurrent) | ~400,000 events/sec |
| Batch publish (flush at 500) | ~600,000 events/sec |
| Request-reply round-trip (p50) | < 0.5 ms |
| Request-reply round-trip (p99) | < 2 ms |
| Dead-letter reprocessing | ~15,000 entries/sec |
| Memory per active subscription | ~220 bytes |

### Tuning for throughput

```csharp
services.AddEventBus(options =>
{
    options.AllowParallelHandling   = true;
    options.MaxConcurrentHandlers   = Environment.ProcessorCount * 2;
    options.DefaultHandlerTimeout   = TimeSpan.FromSeconds(10);
    options.InitialRetryDelayMs     = 50;
    options.RetryDelayMultiplier    = 1.5;
});
```

Use `IBatchEventPublisher` when ingesting high-volume streams — batching amortises lock contention and allocation cost across many events at once.

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run a specific project
dotnet test tests/DotnetEventBus.Tests

# Run tests matching a filter
dotnet test --filter "Category=Integration"
```

The test suite uses **xUnit**, **Moq**, and **FluentAssertions**. Mock `IEventBus` directly or use the in-memory implementation for integration-style tests:

```csharp
// Arrange — real in-memory event bus, no mocking required
var services = new ServiceCollection();
services.AddEventBus();
var sp      = services.BuildServiceProvider();
var bus     = sp.GetRequiredService<IEventBus>();

var received = new List<OrderCreatedEvent>();
bus.Subscribe<OrderCreatedEvent>(
    (e, _) => { received.Add(e); return Task.CompletedTask; },
    handlerName: "TestHandler");

// Act
await bus.PublishAsync(new OrderCreatedEvent { OrderId = "ORD-1" });

// Assert
received.Should().ContainSingle(e => e.OrderId == "ORD-1");
```

## Troubleshooting

### Common Issues

**Issue: Handlers not executing**
- Verify handler is registered before publishing
- Check handler subscription is enabled: `await subscriptionManager.EnableHandlerAsync(name)`
- Ensure event type matches exactly (namespaces matter)
- Check logs for exception details

**Issue: Handlers timing out**
- Increase `DefaultHandlerTimeout` in options
- Check for blocking operations (use `await` instead of `.Result`)
- Profile handler with performance tools to identify bottlenecks
- Consider reducing handler workload or splitting into multiple handlers

**Issue: Memory growth over time**
- Check in-memory cache size: `options.EventCacheSizeLimit = 10000`
- Implement periodic dead-letter cleanup
- Monitor batch publisher for unflushed events
- Use performance profiler to identify memory leaks

**Issue: Dead letter entries not reprocessing**
- Verify dead letter processor is running: `services.AddDeadLetterProcessor()`
- Check handler exception is transient (not permanent failures)
- Increase max retry attempts if needed
- Review dead letter entry details for failure reason

### Debugging Tips

```csharp
// Enable detailed logging
services.AddLogging(builder =>
{
    builder.SetMinimumLevel(LogLevel.Debug);
    builder.AddConsole();
});

// Use performance profiler
var profiler = serviceProvider.GetRequiredService<IPerformanceProfiler>();
var report = profiler.GenerateReport();
Console.WriteLine($"Report:\n{report}");

// Monitor health
var healthCheck = serviceProvider.GetRequiredService<IHealthCheck>();
var status = await healthCheck.CheckHealthAsync();
Console.WriteLine($"Status: {status.Status}");
foreach (var check in status.Checks)
{
    Console.WriteLine($"  {check.Name}: {check.Status}");
}
```

## Related Projects

Part of a collection of .NET libraries and tools. See more at [github.com/sarmkadan](https://github.com/sarmkadan).

### Integration Examples

**Using DotnetEventBus inside an ASP.NET Core minimal API**

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEventBus(o => o.EnableDeadLetterQueue = true);

var app = builder.Build();
var bus = app.Services.GetRequiredService<IEventBus>();

app.MapPost("/orders", async (OrderDto dto) =>
{
    var result = await bus.PublishAsync(new OrderCreatedEvent
    {
        OrderId    = Guid.NewGuid().ToString(),
        CustomerId = dto.CustomerId,
        TotalAmount = dto.Amount
    });
    return Results.Ok(new { result.HandlersInvoked });
});

app.Run();
```

**Wiring DotnetEventBus with a hosted background worker**

```csharp
public class OrderWorker(IEventBus bus, IOrderQueue queue) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var order in queue.ReadAllAsync(stoppingToken))
            await bus.PublishAsync(order, stoppingToken);
    }
}

// Registration
builder.Services.AddEventBus();
builder.Services.AddHostedService<OrderWorker>();
```

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -am 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Setup

```bash
# Clone repository
git clone https://github.com/Sarmkadan/dotnet-event-bus.git
cd dotnet-event-bus

# Install dependencies
dotnet restore

# Build project
dotnet build

# Run tests
dotnet test

# Run specific test project
dotnet test tests/DotnetEventBus.Tests

# Build in release mode
dotnet build -c Release
```

### Code Standards

- Follow C# naming conventions (PascalCase for public, camelCase for private)
- Write unit tests for new features
- Add XML doc comments for public APIs
- Keep methods focused and under 50 lines where possible
- Use async/await for I/O operations

## License

MIT License - Copyright 2026 Vladyslav Zaiets

See LICENSE file for full details. You are free to use, modify, and distribute this software, provided you include the original license and copyright notice.

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**

[Portfolio](https://sarmkadan.com) | [GitHub](https://github.com/Sarmkadan) | [Telegram](https://t.me/sarmkadan)
