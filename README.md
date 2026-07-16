## Architecture

DotnetEventBus is an in-process event bus: pub/sub with polymorphic dispatch, request/reply, per-handler retries with a dead letter queue, and a DI-resolved middleware pipeline - all in a single assembly, no broker. How the pieces fit together (core `EventBus` flow, DLQ pipeline, DI composition, design trade-offs, known limitations) is documented in [docs/architecture.md](docs/architecture.md).

## EventBusOptions

The `EventBusOptions` class provides configuration for the event bus, controlling retry behavior, parallel execution, dead letter queue integration, distributed messaging settings, and middleware pipeline composition. It supports exponential backoff retries, configurable timeouts, concurrency limits, and validation to ensure proper operation.

Example usage:

```csharp
using DotnetEventBus.Configuration;
using DotnetEventBus.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

// Configure event bus options with custom settings
var options = new EventBusOptions
{
    DefaultHandlerTimeout = TimeSpan.FromSeconds(45),
    MaxRetryAttempts = 5,
    RetryDelay = TimeSpan.FromMilliseconds(200),
    RetryDelayMultiplier = 2.5,
    MaxRetryDelay = TimeSpan.FromSeconds(60),
    AllowParallelHandling = true,
    MaxConcurrentHandlers = 8,
    EnableDeadLetterQueue = true,
    ThrowOnHandlerFailure = false,
    IsDistributed = false,
    RequestTimeout = TimeSpan.FromSeconds(60)
};

// Add middleware types to the pipeline
options.MiddlewareTypes.Add(typeof(LoggingMiddleware));
options.MiddlewareTypes.Add(typeof(ValidationMiddleware));

// Register with DI container
var services = new ServiceCollection();
services.AddEventBus(builder => builder
    .Configure(options)
    .AddDeadLetterService()
    .AddJsonFormatter());

var serviceProvider = services.BuildServiceProvider();
var eventBus = serviceProvider.GetRequiredService<EventBus>();

// Validate configuration before use
options.Validate();

// Calculate retry delays programmatically
for (int i = 0; i < options.MaxRetryAttempts; i++)
{
    var delay = options.CalculateRetryDelay(i);
    Console.WriteLine($"Retry {i}: {delay.TotalMilliseconds}ms");
}

// Clone options for testing different configurations
var testOptions = options.Clone();
testOptions.MaxRetryAttempts = 3;
```

## EventBus

The `EventBus` class is the core in-process event bus implementation that provides publish-subscribe and request-reply messaging patterns with middleware support, retry policies, and dead letter queue integration. It supports polymorphic event handling (handlers registered for base types or interfaces will receive events of derived types), configurable middleware pipelines, parallel or sequential handler execution, and automatic retry with exponential backoff.

Example usage:

```csharp
using DotnetEventBus.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

// Setup DI container
var services = new ServiceCollection();
services.AddEventBus(builder => builder
    .Configure(options => options.MaxRetryAttempts = 3)
    .AddDeadLetterService()
    .AddJsonFormatter());

var serviceProvider = services.BuildServiceProvider();
var eventBus = serviceProvider.GetRequiredService<EventBus>();

// Define event types
public class OrderCreatedEvent
{
    public int OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class OrderCreatedHandler : IEventHandler<OrderCreatedEvent>
{
    public async Task Handle(OrderCreatedEvent @event, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Processing order {@event.OrderId} for ${@event.TotalAmount}");
        await Task.Delay(100, cancellationToken);
    }
}

// Register handlers
using var subscription = eventBus.Subscribe(new OrderCreatedHandler());

// Publish events
var publishResult = await eventBus.PublishAsync(new OrderCreatedEvent
{
    OrderId = 123,
    TotalAmount = 99.99m
});

Console.WriteLine($"Published to {publishResult.SuccessfulHandlers.Count} handlers");

// Get subscriptions and clear when done
var handlers = await eventBus.GetSubscriptionsAsync(typeof(OrderCreatedEvent).FullName!);
await eventBus.ClearSubscriptionsAsync();
```

## DeadLetterEntry

The `DeadLetterEntry` class represents a record of a failed event processing attempt within the event bus. It captures the original message, the handler that failed, exception details, and metadata to assist in diagnosing and managing re-processing attempts for failed events.

Example usage:
```csharp
using DotnetEventBus.Models;
using System;

// Assume 'eventMessage' and 'exception' are available
var deadLetterEntry = new DeadLetterEntry(
    eventMessage, 
    "OrderProcessingHandler", 
    exception, 
    maxRetryAttempts: 3);

// Check if the entry is still pending
if (deadLetterEntry.Status == DeadLetterStatus.Pending)
{
    Console.WriteLine(deadLetterEntry.GetSummary());
    
    // Mark as reviewed if no immediate action required
    deadLetterEntry.MarkAsReviewed("Investigation pending");
}

// Mark as reprocessed if the issue was resolved
deadLetterEntry.MarkAsReprocessed();
```

## PublishResult

The `PublishResult` class represents the outcome of publishing an event to the event bus. It tracks which handlers successfully processed the event, which failed, timing information, and any errors that occurred during processing. This result is useful for monitoring event publishing operations and implementing retry logic for failed handlers.

Example usage:
```csharp
using DotnetEventBus.Models;
using System;
using System.Threading.Tasks;

// Publish an event and get the result
var eventBus = new EventBusBuilder()
    .Build();

var publishResult = await eventBus.PublishAsync("OrderCreated", new OrderCreatedEvent {
    OrderId = 123,
    TotalAmount = 99.99m
});

// Analyze the result
Console.WriteLine(publishResult.GetSummary());

if (publishResult.Success)
{
    Console.WriteLine($"Successfully published to {publishResult.SuccessfulHandlers.Count} handlers");
    foreach (var handler in publishResult.SuccessfulHandlers)
    {
        Console.WriteLine($"  - {handler}");
    }
}
else
{
    Console.WriteLine($"Failed to publish: {publishResult.ErrorMessage}");
    Console.WriteLine($"Failed handlers: {string.Join(", ", publishResult.FailedHandlerNames)}");
}

// Create results programmatically
var successResult = PublishResult.CreateSuccess("msg-123", 3);
successResult.AddSuccessfulHandler("OrderHandler");
successResult.AddSuccessfulHandler("NotificationHandler");
successResult.AddSuccessfulHandler("AuditHandler");

var failedResult = PublishResult.CreateFailed("msg-456", new InvalidOperationException("Database unavailable"));
failedResult.AddFailedHandler("PaymentHandler", new InvalidOperationException("Payment service timeout"));
```

## PredicateSubscriptionBuilder

The `PredicateSubscriptionBuilder<TEvent>` class is a fluent builder for constructing predicate-filtered subscriptions on an <see cref="IEventBus"/>. It allows you to compose multiple conditions using AND semantics, ensuring that only events that match all specified criteria are processed by the registered handler.

Example usage:
```csharp
var eventBus = new EventBusBuilder()
    .Build();

var subscription = eventBus.CreatePredicateSubscription<OrderCreatedEvent>()
    .Where(e => e.TotalAmount > 100)
    .WhereNot(e => e.IsCancelled)
    .WithHandlerName("HighValueOrderHandler")
    .WithHandler(HandleHighValueOrderAsync)
    .WithPriority(10)
    .Register();

// Dispose the subscription when no longer needed
subscription.Dispose();
```

## RetryPolicy

RetryPolicy provides a configurable retry mechanism for transient failures. It supports exponential backoff, jitter, and custom retry conditions. Use it to wrap async operations that may fail temporarily.

Example usage:
```csharp
using DotnetEventBus.Integration;
using System;
using System.Threading.Tasks;

var retryPolicy = RetryPolicy.CreateExponentialBackoff()
    .WithMaxRetries(5)
    .WithMaxDelay(TimeSpan.FromSeconds(30))
    .WithRetryableExceptionFilter(ex => ex is TimeoutException);

int result = await retryPolicy.ExecuteAsync(async () =>
{
    // Simulate an operation that may fail
    await Task.Delay(100);
    return 42;
});

await retryPolicy.ExecuteAsync(async () =>
{
    // Void operation
    await Task.Delay(100);
});
```

## WebhookHandler

The `WebhookHandler` class manages webhook subscriptions and event routing to external endpoints. It provides functionality for subscribing/unsubscribing webhook endpoints, filtering events by type, generating and verifying HMAC-SHA256 signatures for security, and managing subscription metadata.

Use `WebhookHandler` to integrate your event bus with external systems that need to react to events in near real-time.

Example usage:
```csharp
using DotnetEventBus.Integration;
using System;
using System.Threading.Tasks;

// Create a webhook handler with optional signing secret for security
var webhookHandler = new WebhookHandler("my-secret-key");

// Create a webhook subscription for specific event types
var subscription = new WebhookSubscription
{
    Url = "https://api.example.com/webhooks/events",
    EventTypes = new List<string> { "OrderCreated", "OrderUpdated", "PaymentProcessed" },
    Headers = new Dictionary<string, string>
    {
        { "X-Custom-Header", "custom-value" }
    },
    IsActive = true,
    RetryCount = 3,
    RetryDelay = TimeSpan.FromSeconds(10)
};

// Subscribe the webhook
webhookHandler.Subscribe(subscription);

// Get all webhooks that should receive a specific event
var matchingWebhooks = webhookHandler.GetWebhooksForEvent("OrderCreated");

// Generate signature for outgoing webhook payload
var payload = "{\"eventType\":\"OrderCreated\",\"data\":{}}";
var signature = webhookHandler.GenerateSignature(payload);

// Verify incoming webhook signature
bool isValid = webhookHandler.VerifySignature(payload, signature);

// Update subscription
webhookHandler.UpdateSubscription(subscription.Id!, s =>
{
    s.IsActive = false;
    s.RetryCount = 5;
});

// Unsubscribe when no longer needed
webhookHandler.Unsubscribe(subscription.Id!);
```

## CircuitBreaker

The `CircuitBreaker` class implements the circuit breaker pattern to prevent cascading failures in distributed systems. It monitors operations for failures and, when a configurable threshold is exceeded, stops forwarding requests to failing services for a specified timeout period, allowing them to recover. The circuit breaker automatically transitions through three states: Closed (normal operation), Open (service unavailable), and HalfOpen (testing recovery).



Use `CircuitBreaker` to wrap operations that may fail temporarily due to external dependencies like databases, APIs, or message brokers.

Example usage:
```csharp
using DotnetEventBus.Integration;
using System;
using System.Threading.Tasks;

// Create a circuit breaker that opens after 5 failures
var circuitBreaker = new CircuitBreaker(failureThreshold: 5, timeout: TimeSpan.FromSeconds(30));

// Execute an operation with circuit breaker protection
try
{
    var result = await circuitBreaker.ExecuteAsync(async () =>
    {
        // Simulate an operation that may fail
        await Task.Delay(100);
        return "Success";
    });
    Console.WriteLine($"Operation succeeded: {result}");
}
catch (CircuitBreakerOpenException ex)
{
    Console.WriteLine($"Service unavailable: {ex.Message}");
}

// Execute a void operation with circuit breaker protection
try
{
    await circuitBreaker.ExecuteAsync(async () =>
    {
        // Simulate an operation that may fail
        await Task.Delay(100);
    });
    Console.WriteLine("Operation completed successfully");
}
catch (CircuitBreakerOpenException ex)
{
    Console.WriteLine($"Service unavailable: {ex.Message}");
}

// Manually reset the circuit breaker
circuitBreaker.Reset();

## MetricsCollector

The `MetricsCollector` class collects and aggregates metrics about event processing. It tracks event publishing counts, handler execution times, failure rates, and latency statistics to provide observability into system health and performance bottlenecks.

Use `MetricsCollector` to monitor your event bus performance, identify slow handlers, and track error rates in production or during load testing.

Example usage:
```csharp
using DotnetEventBus.Advanced;
using System;
using System.Threading.Tasks;

// Create a metrics collector
var metricsCollector = new MetricsCollector();

// Record event publishing
metricsCollector.RecordEventPublished("OrderCreated", 42);
metricsCollector.RecordEventPublished("OrderCreated", 38);
metricsCollector.RecordEventPublished("PaymentProcessed", 25);

// Record handler executions
metricsCollector.RecordHandlerExecution("OrderCreatedHandler", "OrderCreated", 120, true);
metricsCollector.RecordHandlerExecution("OrderCreatedHandler", "OrderCreated", 115, true);
metricsCollector.RecordHandlerExecution("PaymentHandler", "PaymentProcessed", 45, true);

// Record a failed event
try
{
    // Simulate failed event handling
    throw new InvalidOperationException("Database unavailable");
}
catch (Exception ex)
{
    metricsCollector.RecordEventFailed("OrderCreated", "OrderCreatedHandler", ex);
}

// Get metrics
var orderMetrics = metricsCollector.GetEventMetrics("OrderCreated");
Console.WriteLine($"OrderCreated: {orderMetrics?.PublishCount} published, {orderMetrics?.FailureCount} failed");

var handlerMetrics = metricsCollector.GetHandlerMetrics("OrderCreatedHandler", "OrderCreated");
Console.WriteLine($"Handler success rate: {handlerMetrics?.SuccessRate}%");

// Get system-wide metrics
var systemMetrics = metricsCollector.GetSystemMetrics();
Console.WriteLine($"Total throughput: {systemMetrics.ThroughputPerSecond} events/sec");

// Get latency statistics
var latencyStats = metricsCollector.GetLatencyStats("OrderCreated");
Console.WriteLine($"P95 latency: {latencyStats?.P95Ms}ms");

// Reset metrics when needed
metricsCollector.Reset();
```

## EventSourcedAggregate

The `EventSourcedAggregate` class serves as a base class for implementing event-sourced aggregates in your domain model. It enables state reconstruction by replaying historical events and optimizes performance through snapshotting, providing a robust foundation for building event-driven systems with a complete audit trail.

Example usage:
```csharp
using DotnetEventBus.Advanced;
using System;
using System.Collections.Generic;

// Define your aggregate by inheriting from EventSourcedAggregate
public class OrderAggregate : EventSourcedAggregate
{
    public decimal TotalAmount { get; private set; }

    public OrderAggregate(string id)
    {
        // Set the Id property
        // The Id property is inherited from EventSourcedAggregate
    }

    // Apply method is called via reflection when loading events
    private void Apply(OrderCreated e)
    {
        TotalAmount = e.Amount;
    }
}

// 1. Loading from history
var order = new OrderAggregate("order-123");
var events = new List<object> { new OrderCreated("order-123", 99.99m) };
order.LoadFromHistory(events);

// 2. Committing changes
// After applying new events, call CommitChanges to clear the internal list
order.CommitChanges();

// 3. Snapshotting
// Create a snapshot to optimize future loads
var snapshot = order.CreateSnapshot();

// Restore from snapshot
var restoredOrder = new OrderAggregate("order-123");
restoredOrder.LoadSnapshot(snapshot);
```


## SagaOrchestrator

The `SagaOrchestrator<TContext>` class implements the Saga pattern for managing distributed transactions across multiple steps. It coordinates a sequence of operations and automatically executes compensating transactions if any step fails, ensuring data consistency even in failure scenarios. The orchestrator tracks each step's status and provides detailed execution results.

Example usage:
```csharp
using DotnetEventBus.Advanced;
using System;
using System.Threading.Tasks;

// Define a context class to hold saga data
public class OrderSagaContext
{
    public int OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public bool PaymentProcessed { get; set; }
    public bool InventoryReserved { get; set; }
    public bool OrderConfirmed { get; set; }
}

// Create and execute a saga
var sagaId = Guid.NewGuid().ToString();
var orchestrator = new SagaOrchestrator<OrderSagaContext>(sagaId)
    .AddStep("ProcessPayment", async ctx =>
    {
        Console.WriteLine($"Processing payment for order {ctx.OrderId}");
        await Task.Delay(100); // Simulate payment processing
        ctx.PaymentProcessed = true;
    }, async ctx =>
    {
        Console.WriteLine($"Refunding payment for order {ctx.OrderId}");
        await Task.Delay(100); // Simulate payment refund
        ctx.PaymentProcessed = false;
    })
    .AddStep("ReserveInventory", async ctx =>
    {
        Console.WriteLine($"Reserving inventory for order {ctx.OrderId}");
        await Task.Delay(100); // Simulate inventory reservation
        ctx.InventoryReserved = true;
    }, async ctx =>
    {
        Console.WriteLine($"Releasing inventory for order {ctx.OrderId}");
        await Task.Delay(100); // Simulate inventory release
        ctx.InventoryReserved = false;
    })
    .AddStep("ConfirmOrder", async ctx =>
    {
        Console.WriteLine($"Confirming order {ctx.OrderId}");
        await Task.Delay(100); // Simulate order confirmation
        ctx.OrderConfirmed = true;
    });

// Execute the saga
var context = new OrderSagaContext
{
    OrderId = 123,
    TotalAmount = 99.99m,
    CustomerEmail = "customer@example.com"
};

var result = await orchestrator.ExecuteAsync(context);

if (result.Success)
{
    Console.WriteLine($"Saga completed successfully! Order {context.OrderId} processed");
    Console.WriteLine($"Steps executed: {string.Join(", ", orchestrator.GetStepStatus().Select(s => s.Name))}");
}
else
{
    Console.WriteLine($"Saga failed at step: {result.FailedStep}");
    Console.WriteLine($"Error: {result.Error}");
}
```
```

## EventTransformer

The `EventTransformer<TSource, TTarget>` class provides a flexible way to transform events from one type to another. It supports fluent transformation chains, allowing you to compose multiple transformation steps into a single pipeline. This is particularly useful when you need to convert events to a format that downstream handlers can process more easily.

Example usage:
```csharp
using DotnetEventBus.Advanced;
using System;
using System.Collections.Generic;

// Define source and target event types
public class OrderCreatedEvent
{
    public int OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderSummaryEvent
{
    public int OrderId { get; set; }
    public decimal TotalAmount { get; set; }
}

// Create a transformer using a mapping function
var transformer = EventTransformerBuilder.CreateTransformer<OrderCreatedEvent, OrderSummaryEvent>(
    source => new OrderSummaryEvent
    {
        OrderId = source.OrderId,
        TotalAmount = source.TotalAmount
    }
);

// Transform a single event
var orderEvent = new OrderCreatedEvent { OrderId = 123, TotalAmount = 99.99m, CreatedAt = DateTime.Now };
var summary = transformer.Transform(orderEvent);
Console.WriteLine($"Transformed: Order {summary.OrderId} for ${summary.TotalAmount}");

// Transform multiple events
var events = new List<OrderCreatedEvent> 
{
    new() { OrderId = 1, TotalAmount = 10.50m },
    new() { OrderId = 2, TotalAmount = 25.75m }
};
var summaries = transformer.TransformMany(events);

// Add post-transformation steps
var enhancedTransformer = EventTransformerBuilder.CreateTransformer<OrderCreatedEvent, OrderSummaryEvent>(
    source => new OrderSummaryEvent
    {
        OrderId = source.OrderId,
        TotalAmount = source.TotalAmount
    }
).Then(summary => 
{
    summary.TotalAmount = Math.Round(summary.TotalAmount, 2); // Ensure 2 decimal places
    return summary;
});

// Chain transformers for complex transformations
var chainedTransformer = EventTransformerBuilder.CreateTransformer<OrderCreatedEvent, OrderSummaryEvent>(
    source => new OrderSummaryEvent { OrderId = source.OrderId, TotalAmount = source.TotalAmount }
).Chain<OrderSummaryData>(summary => new OrderSummaryData 
{
    OrderId = summary.OrderId,
    Amount = summary.TotalAmount,
    FormattedAmount = $"${summary.TotalAmount:F2}"
});

// Create a property copy transformer (copies matching properties automatically)
var copyTransformer = EventTransformerBuilder.CreatePropertyCopyTransformer<OrderCreatedEvent, OrderSummaryEvent>();

// Create a dictionary transformer
var dictTransformer = EventTransformerBuilder.CreateDictionaryTransformer<OrderCreatedEvent>();
var dict = dictTransformer.Transform(orderEvent);
```

## EventMiddlewareContext

The `EventMiddlewareContext` class provides the context for an event as it flows through the middleware pipeline. It contains the event object, its type, correlation ID, the original `EventMessage`, and a `CancellationToken` for cooperative cancellation. This context is passed to each middleware in the pipeline, allowing them to inspect and modify event processing behavior.

Example usage:
```csharp
using DotnetEventBus.Middleware;
using DotnetEventBus.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

// Create an event to process
var orderEvent = new OrderCreatedEvent { OrderId = 123, Amount = 99.99m };

// Create an EventMessage for the event
var eventMessage = new EventMessage("OrderCreated", 
    System.Text.Json.JsonSerializer.Serialize(orderEvent));

// Create a cancellation token
var cts = new CancellationTokenSource();
var cancellationToken = cts.Token;

// Create the middleware context
var context = new EventMiddlewareContext(
    @event: orderEvent,
    eventType: typeof(OrderCreatedEvent),
    correlationId: "corr-12345",
    eventMessage: eventMessage,
    cancellationToken: cancellationToken
);

// Access context properties
Console.WriteLine($"Event Type: {context.EventType.Name}");
Console.WriteLine($"Correlation ID: {context.CorrelationId}");
Console.WriteLine($"Event: {context.Event}");
Console.WriteLine($"EventMessage ID: {context.EventMessage.MessageId}");

// Modify correlation ID if needed
context.CorrelationId = "updated-correlation-67890";

// Check cancellation token
if (context.CancellationToken.IsCancellationRequested)
{
    Console.WriteLine("Operation was cancelled");
}
```

## PipelineBuilder

The `PipelineBuilder` class constructs the middleware pipeline for the event bus. It allows you to add, remove, and configure middleware components that process events before they reach their handlers. The builder maintains the order of middleware execution and provides methods to build the final pipeline.

Example usage:
```csharp
using DotnetEventBus.Middleware;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

// Define middleware components
public class LoggingMiddleware : EventBusMiddleware
{
    public override async Task InvokeAsync(EventContext context, Func<Task> next)
    {
        Console.WriteLine($"Processing event {context.EventType} with correlation ID {context.CorrelationId}");
        await next();
        Console.WriteLine($"Completed processing event {context.EventType}");
    }
}

public class ValidationMiddleware : EventBusMiddleware
{
    public override async Task InvokeAsync(EventContext context, Func<Task> next)
    {
        if (context.EventData is null)
        {
            throw new InvalidOperationException("Event data cannot be null");
        }
        await next();
    }
}

// Build the pipeline with middleware
var pipelineBuilder = new PipelineBuilder();

pipelineBuilder.Use<LoggingMiddleware>();
pipelineBuilder.Use<ValidationMiddleware>();

// Build the pipeline
var middlewarePipeline = pipelineBuilder.Build();

// Use the pipeline with an event context
var eventContext = new EventContext(
    eventType: "OrderCreated",
    eventData: new { OrderId = 123, Amount = 99.99m },
    metadata: new Dictionary<string, object> { { "tenantId", "acme" } },
    correlationId: Guid.NewGuid().ToString()
);

await middlewarePipeline(eventContext);

// Clear all middleware and start fresh
pipelineBuilder.Clear();
```

## PipelineBuilderExtensions

The `PipelineBuilderExtensions` class provides extension methods for fluent pipeline configuration, simplifying the registration of common middleware components. It offers methods for adding logging, error handling, rate limiting, and custom middleware, as well as pre-configured pipeline templates for different environments (standard, high-performance, and development).


Example usage:

```csharp
using DotnetEventBus.Configuration;
using DotnetEventBus.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

// Setup DI container
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
var serviceProvider = services.BuildServiceProvider();
var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

// Create a pipeline builder
var pipelineBuilder = new PipelineBuilder();

// Add individual middleware components using extension methods
pipelineBuilder
    .AddLogging(loggerFactory, LogLevel.Debug, logPayloads: true)
    .AddErrorHandling(loggerFactory, maxRetries: 3, retryDelay: TimeSpan.FromSeconds(1))
    .AddRateLimiting(loggerFactory, requestsPerWindow: 5000, timeWindow: TimeSpan.FromSeconds(30));

// Build the pipeline
var middlewarePipeline = pipelineBuilder.Build();

// Use pre-configured pipeline templates for different environments
var standardPipelineBuilder = new PipelineBuilder()
    .CreateStandardPipeline(loggerFactory);

var highPerformancePipelineBuilder = new PipelineBuilder()
    .CreateHighPerformancePipeline(loggerFactory);

var developmentPipelineBuilder = new PipelineBuilder()
    .CreateDevelopmentPipeline(loggerFactory);

// Add custom middleware using the UseMiddleware extension
pipelineBuilder.UseMiddleware(middleware => new CustomMiddleware(middleware));

// Clear all middleware and start fresh
pipelineBuilder.Clear();
```

## EventFilter

The `EventFilter<T>` class provides a fluent filtering API for events, allowing handlers to filter events based on predicates before processing. It reduces unnecessary handler invocations by filtering at the bus level, which is particularly useful when you need to process only specific events that match certain criteria.

Example usage:
```csharp
using DotnetEventBus.Advanced;
using System;
using System.Collections.Generic;
using System.Linq;

// Define an event type
public class OrderCreatedEvent
{
    public int OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "Pending";
}

// Create and use a filter
var filter = FilterBuilder.CreateFilter<OrderCreatedEvent>()
    .Where(e => e.TotalAmount > 100)
    .WhereProperty(e => e.Status, "Completed")
    .WherePropertyContains(e => e.CustomerEmail, "@example.com")
    .WherePropertyInRange(e => e.TotalAmount, 50, 500);

// Check if an event matches all filters
var orderEvent = new OrderCreatedEvent
{
    OrderId = 123,
    TotalAmount = 150.50m,
    CustomerEmail = "user@example.com",
    CreatedAt = DateTime.Now,
    Status = "Completed"
};

bool matches = filter.Matches(orderEvent); // true
```

## BatchEventPublisher

The `BatchEventPublisher` class collects events and flushes them in batches for improved throughput. It reduces per-event overhead and significantly improves system performance by processing multiple events together. Each event in a batch is processed independently, so a handler throwing for one event does not prevent the remaining events from being processed.

The publisher automatically flushes events when either the batch size threshold is reached or the flush interval elapses. You can configure both the batch size and flush interval through the constructor.

Example usage:

```csharp
using DotnetEventBus.Models;
using DotnetEventBus.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

// Setup DI container with logging
var services = new ServiceCollection();
services.AddLogging(configure => configure.AddConsole());

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<BatchEventPublisher>>();

// Create batch publisher with custom settings (batch size: 50, flush every 5 seconds)
var batchPublisher = new BatchEventPublisher(logger, batchSize: 50, flushInterval: TimeSpan.FromSeconds(5));

// Set up a flush handler to process batches
batchPublisher.SetFlushHandler(async batch => {
    Console.WriteLine($"Processing batch of {batch.Events.Count} events");
    
    // Process all events in the batch
    foreach (var envelope in batch.Events) {
        Console.WriteLine($"Processing event {envelope.EventType} (ID: {envelope.EventId})");
        // Your batch processing logic here
    }
});

// Create and publish events
var orderCreatedEvent = EventEnvelope.Create("OrderCreated", new {
    OrderId = 123,
    TotalAmount = 99.99m,
    CustomerName = "John Doe"
});

var paymentProcessedEvent = EventEnvelope.Create("PaymentProcessed", new {
    PaymentId = 456,
    Amount = 99.99m,
    Status = "Completed"
});

// Add events to the batch (will auto-flush when batch size is reached or interval elapses)
await batchPublisher.AddEventAsync(orderCreatedEvent);
await batchPublisher.AddEventAsync(paymentProcessedEvent);

// Flush remaining events manually if needed
// await batchPublisher.FlushAsync();

// Get statistics
var stats = batchPublisher.GetStats();
Console.WriteLine($"Buffered events: {stats.BufferedEventCount}");
Console.WriteLine($"Last flush: {stats.LastFlushTime}");
```


For per-event error handling with detailed results:

```csharp
// Set up per-event handler with result aggregation
batchPublisher.SetFlushHandlerWithResult(
    async envelope => {
        try {
            // Process individual event
            Console.WriteLine($"Processing event {envelope.EventType}");
            await Task.Delay(10); // Simulate work
            return new EventBatchItemResult {
                EventId = envelope.EventId,
                EventType = envelope.EventType,
                Success = true
            };
        }
        catch (Exception ex) {
            return new EventBatchItemResult {
                EventId = envelope.EventId,
                EventType = envelope.EventType,
                Success = false,
                ErrorMessage = ex.Message,
                Exception = ex
            };
        }
    },
    onBatchComplete: result => {
        Console.WriteLine($"Batch complete: {result.SucceededCount} succeeded, {result.FailedCount} failed");
        foreach (var failedEvent in result.FailedEvents) {
            Console.WriteLine($"  Failed: {failedEvent.EventType} - {failedEvent.ErrorMessage}");
        }
    }
);
```

## EventBusApiController

The `EventBusApiController` class provides REST API endpoints for interacting with the event bus. It exposes operations for publishing individual events or batches, retrieving system statistics, and checking health status. The controller wraps results in a standardized `ApiResponse<T>` wrapper that includes success status, data payload, error messages, and timestamps.

Example usage:

```csharp
using DotnetEventBus.Api;
using DotnetEventBus.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Setup DI container with event bus and optional metrics collector
var services = new ServiceCollection();
services.AddEventBus(builder => builder
    .Configure(options => options.MaxRetryAttempts = 3)
    .AddDeadLetterService()
    .AddJsonFormatter());

var serviceProvider = services.BuildServiceProvider();
var eventBus = serviceProvider.GetRequiredService<EventBus>();

// Create controller instance (optionally with metrics)
var controller = new EventBusApiController(eventBus);

// Publish a single event
var publishResult = await controller.PublishEventAsync(
    "OrderCreated",
    new { OrderId = 123, TotalAmount = 99.99m, CustomerName = "John Doe" }
);

if (publishResult.IsSuccess)
{
    Console.WriteLine($"Event published successfully: {publishResult.Data?.EventId}");
    Console.WriteLine($"Event type: {publishResult.Data?.EventType}");
    Console.WriteLine($"Published at: {publishResult.Data?.PublishedAt}");
}
else
{
    Console.WriteLine($"Publish failed: {publishResult.ErrorMessage}");
}

// Publish a batch of events
var batchResult = await controller.PublishBatchAsync(new List<EventEnvelope>
{
    EventEnvelope.Create("OrderCreated", new { OrderId = 1, TotalAmount = 10.50m }),
    EventEnvelope.Create("PaymentProcessed", new { PaymentId = 1, Amount = 10.50m }),
    EventEnvelope.Create("InventoryUpdated", new { ProductId = 1, Quantity = 5 })
});

if (batchResult.IsSuccess)
{
    Console.WriteLine($"Batch published: {batchResult.Data?.EventCount} events");
    Console.WriteLine($"Batch ID: {batchResult.Data?.BatchId}");
}
else
{
    Console.WriteLine($"Batch publish failed: {batchResult.ErrorMessage}");
}

// Get system statistics
var statsResponse = controller.GetStats();
if (statsResponse.IsSuccess)
{
    var stats = statsResponse.Data;
    Console.WriteLine($"Status: {stats?.Status}");
    Console.WriteLine($"Total events published: {stats?.TotalEventsPublished}");
    Console.WriteLine($"Total events failed: {stats?.TotalEventsFailed}");
    Console.WriteLine($"Active subscriptions: {stats?.ActiveSubscriptions}");
}

// Get health status
var healthResponse = await controller.GetHealthAsync();
if (healthResponse.IsSuccess)
{
    var healthStatus = healthResponse.Data;
    Console.WriteLine($"Health status: {healthStatus}");
}
```

## RateLimitingMiddleware

The `RateLimitingMiddleware` class enforces rate limiting on event publishing to prevent system overload and ensure fair resource distribution across event types. It uses a sliding window algorithm to track request rates per event type, allowing you to configure the maximum number of requests allowed within a specified time window.

Example usage:
```csharp
using DotnetEventBus.Middleware;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

// Configure rate limiting middleware with 1000 requests per 60 seconds per event type
var rateLimitingMiddleware = new RateLimitingMiddleware(
    logger: new TestLogger<RateLimitingMiddleware>(),
    requestsPerWindow: 1000,
    timeWindow: TimeSpan.FromSeconds(60)
);

// Create the middleware pipeline
var middleware = rateLimitingMiddleware.Create(async context =>
{
    Console.WriteLine("Event processing continues...");
    await Task.CompletedTask;
});

// Simulate checking if an event is allowed
bool isAllowed = await rateLimitingMiddleware.IsAllowed("OrderCreated");
Console.WriteLine($"Is OrderCreated allowed? {isAllowed}");

// Record a request for an event type
await rateLimitingMiddleware.RecordRequest("OrderCreated");

// Handle rate limit exceeded exception
try
{
    // This would throw if rate limit is exceeded
    // throw new RateLimitExceededException("Rate limit exceeded for event type: OrderCreated");
}
catch (RateLimitExceededException ex)
{
    Console.WriteLine($"Rate limit exceeded: {ex.Message}");
}

// The RateLimitExceededException exception class
var exception = new RateLimitExceededException("Rate limit exceeded");
```

## Subscription

The `Subscription` class represents a subscription between an event type and its handlers. It encapsulates metadata about the handler including execution priority, timeout settings, concurrency control, and failure handling behavior. Subscriptions can be dynamically enabled, disabled, or configured with custom timeouts to control event processing behavior.

Example usage:
```csharp
using DotnetEventBus.Models;
using System;
using System.Threading.Tasks;

// Create a handler method
async Task HandleOrderCreatedAsync(OrderCreatedEvent orderEvent)
{
    Console.WriteLine($"Processing order {orderEvent.OrderId} for ${orderEvent.TotalAmount}");
    await Task.Delay(100);
}

// Create a subscription
var subscription = new Subscription(
    eventType: "OrderCreated",
    handler: (Action<OrderCreatedEvent>)HandleOrderCreatedAsync,
    handlerName: "OrderProcessingHandler",
    priority: 10
);

// Configure subscription settings
subscription.SetTimeout(TimeSpan.FromSeconds(30));
subscription.AllowConcurrent = false;
subscription.SendToDeadLetterOnFailure = true;

// Disable/enable subscription dynamically
subscription.Disable();
// ... later ...
subscription.Enable();

// Access subscription properties
Console.WriteLine($"Subscription ID: {subscription.Id}");
Console.WriteLine($"Event Type: {subscription.EventType}");
Console.WriteLine($"Handler: {subscription.HandlerName}");
Console.WriteLine($"Priority: {subscription.Priority}");
Console.WriteLine($"Is Active: {subscription.IsActive}");
Console.WriteLine($"Is Async: {subscription.IsAsync}");
Console.WriteLine($"Timeout: {subscription.Timeout}");
Console.WriteLine($"Allow Concurrent: {subscription.AllowConcurrent}");
Console.WriteLine($"Send to Dead Letter: {subscription.SendToDeadLetterOnFailure}");
Console.WriteLine($"Created At: {subscription.CreatedAtUtc}");
```

## EventFilter

Example usage:
```csharp
using DotnetEventBus.Advanced;
using System;
using System.Collections.Generic;
using System.Linq;

// Define an event type
public class OrderCreatedEvent
{
    public int OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "Pending";
}

// Create and use a filter
var filter = FilterBuilder.CreateFilter<OrderCreatedEvent>()
    .Where(e => e.TotalAmount > 100)
    .WhereProperty(e => e.Status, "Completed")
    .WherePropertyContains(e => e.CustomerEmail, "@example.com")
    .WherePropertyInRange(e => e.TotalAmount, 50, 500);

// Check if an event matches all filters
var orderEvent = new OrderCreatedEvent
{
    OrderId = 123,
    TotalAmount = 150.50m,
    CustomerEmail = "user@example.com",
    CreatedAt = DateTime.Now,
    Status = "Completed"
};

bool matches = filter.Matches(orderEvent); // true

// Filter a collection of events
var events = new List<OrderCreatedEvent>
{
    new() { OrderId = 1, TotalAmount = 50, CustomerEmail = "a@example.com", Status = "Pending" },
    new() { OrderId = 2, TotalAmount = 150, CustomerEmail = "b@example.com", Status = "Completed" },
    new() { OrderId = 3, TotalAmount = 250, CustomerEmail = "c@example.com", Status = "Completed" }
};

var filteredEvents = filter.FilterCollection(events).ToList();
// Returns only events with TotalAmount > 100, Status = "Completed", and CustomerEmail contains "@example.com"

// Clear filters when needed
filter.Clear();

// Create specialized filters using factory methods
var wildcardFilter = FilterBuilder.CreateWildcardFilter<OrderCreatedEvent>(); // matches all events
var emptyFilter = FilterBuilder.CreateEmptyFilter<OrderCreatedEvent>(); // matches no events
```

## EventRoutingConfiguration

The `EventRoutingConfiguration` class provides flexible event routing capabilities that allow you to conditionally route events to specific handlers based on metadata or event content. This enables sophisticated routing scenarios without modifying handler implementations, such as routing high-value orders to premium handlers, filtering events by tenant, or implementing A/B testing for event handlers.

Example usage:

```csharp
using DotnetEventBus.Configuration;
using System;
using System.Collections.Generic;

// Create routing configuration using the fluent builder
var routingConfig = new EventRoutingBuilder()
    .RouteEvent("OrderCreated", "StandardOrderHandler")
    .RouteEventIf("OrderCreated", "PremiumOrderHandler", 
        metadata => metadata.GetValueOrDefault("OrderAmount") as decimal? > 1000,
        priority: 10)
    .RouteByMetadata("OrderUpdated", "TenantASpecificHandler", "TenantId", "tenant-a")
    .RouteByMetadata("OrderUpdated", "TenantBSpecificHandler", "TenantId", "tenant-b")
    .Build();

// Check if an event should be routed to a specific handler
var shouldRoute = routingConfig.ShouldRoute(
    "OrderCreated",
    "PremiumOrderHandler",
    new Dictionary<string, object> { { "OrderAmount", 1500m } }
);
Console.WriteLine($"Should route to PremiumOrderHandler: {shouldRoute}"); // true

// Get all configured routes for an event type
var routes = routingConfig.GetRoutes("OrderCreated");
foreach (var route in routes)
{
    Console.WriteLine($"Route to: {route.TargetHandler}");
}

// Get all configured event types
var configuredTypes = routingConfig.GetConfiguredEventTypes();
Console.WriteLine($"Configured event types: {string.Join(", ", configuredTypes)}");

// Clear all routes when needed
routingConfig.Clear();
```

## InMemoryEventCache

The `InMemoryEventCache` class provides a thread-safe, in-memory cache implementation for event bus operations. It stores event data locally using a concurrent dictionary with automatic expiration and LRU eviction, making it ideal for single-instance deployments where external dependencies are undesirable. The cache tracks hits/misses and provides memory usage statistics for monitoring.

Example usage:

```csharp
using DotnetEventBus.Caching;
using System;
using System.Threading.Tasks;

// Create a cache instance with default capacity (10,000 items)
var cache = new InMemoryEventCache(maxCapacity: 10000);

// Define an event type for caching
public class OrderCreatedEvent
{
    public int OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
}

// Store an event in cache with 5-minute expiration
var orderEvent = new OrderCreatedEvent { OrderId = 123, TotalAmount = 99.99m, CustomerEmail = "user@example.com" };
await cache.SetAsync("order:123", orderEvent, TimeSpan.FromMinutes(5));

// Retrieve cached event
var cachedOrder = await cache.GetAsync<OrderCreatedEvent>("order:123");
if (cachedOrder != null)
{
    Console.WriteLine($"Retrieved order {cachedOrder.OrderId} from cache");
}

// Check if key exists
bool exists = await cache.ExistsAsync("order:123");
Console.WriteLine($"Key exists: {exists}");

// Get multiple cached items
var keys = new[] { "order:123", "order:456", "order:789" };
var cachedOrders = await cache.GetManyAsync<OrderCreatedEvent>(keys);
foreach (var kvp in cachedOrders)
{
    Console.WriteLine($"Cached order {kvp.Value.OrderId}");
}

// Remove a single item
await cache.RemoveAsync("order:123");

// Remove multiple items
await cache.RemoveManyAsync(new[] { "order:456", "order:789" });

// Get cache statistics
var stats = await cache.GetStatsAsync();
Console.WriteLine($"Cache stats: {stats.Hits} hits, {stats.Misses} misses, {stats.TotalItems} items");

// Clear the entire cache
await cache.ClearAsync();

// Cleanup is automatic via background task (expired entries every minute, LRU eviction when full)
```

## HealthCheck

The `HealthCheck` class monitors the health of the event bus system by performing periodic checks on critical components and reporting their status. It aggregates results from multiple health check probes to provide an overall system health assessment, enabling automated detection of system degradation and failures.

Example usage:

```csharp
using DotnetEventBus.Monitoring;
using System;
using System.Threading.Tasks;

// Create a health check instance
var healthCheck = new HealthCheck();

// Register built-in probes for memory and responsiveness monitoring
healthCheck.RegisterProbe("memory", BuiltInProbes.CreateMemoryProbe());
healthCheck.RegisterProbe("responsiveness", BuiltInProbes.CreateResponsivenessProbe());

// Perform health check
var result = await healthCheck.CheckHealthAsync();

// Analyze the result
Console.WriteLine($"Overall status: {result.OverallStatus}");
Console.WriteLine($"Checked at: {result.CheckedAt}");

foreach (var kvp in result.Checks)
{
    Console.WriteLine($"Probe '{kvp.Key}': {kvp.Value.Status}");
    if (kvp.Value.Message != null)
    {
        Console.WriteLine($"  Message: {kvp.Value.Message}");
    }
}

// Get last status without re-checking
var lastStatus = healthCheck.GetLastStatus();
var lastCheckTime = healthCheck.GetLastCheckTime();

// Create custom probes for specific components
public class DatabaseHealthProbe : IHealthCheckProbe
{
    public async Task<ProbeResult> CheckAsync()
    {
        // Implement actual database health check logic
        try
        {
            // Test database connection
            await Task.Delay(50); // Simulate check
            return new ProbeResult
            {
                Status = HealthStatus.Healthy,
                Message = "Database connection successful"
            };
        }
        catch (Exception ex)
        {
            return new ProbeResult
            {
                Status = HealthStatus.Unhealthy,
                Message = $"Database connection failed: {ex.Message}"
            };
        }
    }
}

// Register custom probe
healthCheck.RegisterProbe("database", new DatabaseHealthProbe());
```

Example usage:

```csharp
using DotnetEventBus.Models;
using DotnetEventBus.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

// Setup DI container
var services = new ServiceCollection();
services.AddEventBus(builder => builder
    .Configure(options => options.MaxRetryAttempts = 3)
    .AddDeadLetterService()
    .AddJsonFormatter());

var serviceProvider = services.BuildServiceProvider();
var subscriptionRepository = serviceProvider.GetRequiredService<ISubscriptionRepository>();

// Define event types
public class OrderCreatedEvent { public int OrderId { get; set; } }
public class PaymentProcessedEvent { public decimal Amount { get; set; } }

// Create and register handlers (typically via EventBus.Subscribe())
var orderHandler = new Subscription(
    eventType: typeof(OrderCreatedEvent).FullName!,
    handlerName: "OrderCreatedHandler",
    priority: 10
);
var paymentHandler = new Subscription(
    eventType: typeof(PaymentProcessedEvent).FullName!,
    handlerName: "PaymentHandler",
    priority: 5
);

// Add subscriptions to repository
// (In real usage, subscriptions are typically added via EventBus.Subscribe())

// Query subscriptions by event type
var orderSubscriptions = await subscriptionRepository.GetByEventTypeAsync(typeof(OrderCreatedEvent).FullName!);
Console.WriteLine($"Found {orderSubscriptions.Count()} subscriptions for OrderCreatedEvent");

// Query active subscriptions for a specific event type
var activeOrderSubscriptions = await subscriptionRepository.GetActiveByEventTypeAsync(typeof(OrderCreatedEvent).FullName!);
Console.WriteLine($"Found {activeOrderSubscriptions.Count()} active subscriptions for OrderCreatedEvent");

// Get subscriptions by handler name
var paymentHandlerSubscriptions = await subscriptionRepository.GetByHandlerNameAsync("PaymentHandler");
Console.WriteLine($"Found {paymentHandlerSubscriptions.Count()} subscriptions for PaymentHandler");

// Get all active/inactive subscriptions
var allActive = await subscriptionRepository.GetAllActiveAsync();
var allInactive = await subscriptionRepository.GetAllInactiveAsync();
Console.WriteLine($"Active: {allActive.Count()}, Inactive: {allInactive.Count()}");

// Get subscriptions ordered by priority (highest first)
var orderedSubscriptions = await subscriptionRepository.GetByEventTypeOrderedByPriorityAsync(typeof(OrderCreatedEvent).FullName!);
foreach (var sub in orderedSubscriptions)
{
    Console.WriteLine($"Subscription {sub.HandlerName} with priority {sub.Priority}");
}

// Count subscriptions for an event type
var count = await subscriptionRepository.CountByEventTypeAsync(typeof(OrderCreatedEvent).FullName!);
Console.WriteLine($"Total subscriptions for OrderCreatedEvent: {count}");

// Disable/enable handlers
await subscriptionRepository.DisableHandlerAsync("PaymentHandler");
await subscriptionRepository.EnableHandlerAsync("PaymentHandler");
```

## InMemoryRepository

The `InMemoryRepository<T>` class provides a thread-safe, in-memory repository implementation using a dictionary as the underlying storage. It is designed for testing scenarios, development environments, and single-process deployments where persistence is not required. The repository uses a `ReaderWriterLockSlim` to ensure thread-safe operations and provides all standard CRUD operations with pagination support.

Example usage:

```csharp
using DotnetEventBus.Repositories;
using System;
using System.Threading.Tasks;

// Define a simple entity
public class Product
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

// Create repository instance
var repository = new InMemoryRepository<Product>();

// Add entities
var product1 = await repository.AddAsync(new Product { Name = "Laptop", Price = 999.99m });
var product2 = await repository.AddAsync(new Product { Name = "Mouse", Price = 29.99m });

// Get all entities
var allProducts = await repository.GetAllAsync();
Console.WriteLine($"Total products: {allProducts.Count()}");

// Get by ID
var foundProduct = await repository.GetByIdAsync(product1.Id);
Console.WriteLine($"Found: {foundProduct?.Name}");

// Update entity
foundProduct!.Price = 899.99m;
await repository.UpdateAsync(foundProduct);

// Check existence
bool exists = await repository.ExistsAsync(product2.Id);
Console.WriteLine($"Product 2 exists: {exists}");

// Count entities
int count = await repository.CountAsync();
Console.WriteLine($"Total count: {count}");

// Pagination
var page = await repository.GetPagedAsync(pageNumber: 1, pageSize: 10);
Console.WriteLine($"Page 1 has {page.Items.Count()} items, total {page.TotalCount}");

// Delete entity
bool deleted = await repository.DeleteAsync(product2.Id);
Console.WriteLine($"Product 2 deleted: {deleted}");

// Clear all data
await repository.ClearAsync();
```

## IEventMessageRepository

The `IEventMessageRepository` interface provides data access operations for querying and managing persisted event messages. It supports filtering messages by event type, time range, correlation ID, source, and failure status, as well as bulk cleanup operations for message retention policies. This repository is essential for operational tasks like auditing, debugging, and implementing message retention workflows.

Example usage:

```csharp
using DotnetEventBus.Models;
using DotnetEventBus.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

// Setup DI container
var services = new ServiceCollection();
services.AddEventBus(builder => builder
  .Configure(options => options.MaxRetryAttempts = 3)
  .AddDeadLetterService()
  .AddJsonFormatter());

var serviceProvider = services.BuildServiceProvider();
var eventMessageRepository = serviceProvider.GetRequiredService<IEventMessageRepository>();

// Define event types
public class OrderCreatedEvent { public int OrderId { get; set; } }
public class PaymentProcessedEvent { public decimal Amount { get; set; } }

// Query messages by event type
var orderMessages = await eventMessageRepository.GetByEventTypeAsync(typeof(OrderCreatedEvent).FullName!);
Console.WriteLine($"Found {orderMessages.Count()} OrderCreated messages");

// Query messages by time range (last 24 hours)
var yesterday = DateTime.UtcNow.AddDays(-1);
var today = DateTime.UtcNow;
var recentMessages = await eventMessageRepository.GetByTimeRangeAsync(yesterday, today);
Console.WriteLine($"Found {recentMessages.Count()} messages from last 24 hours");

// Query messages by correlation ID
var correlationMessages = await eventMessageRepository.GetByCorrelationIdAsync("corr-12345");
Console.WriteLine($"Found {correlationMessages.Count()} messages with correlation ID");

// Query messages by source
var sourceMessages = await eventMessageRepository.GetBySourceAsync("order-service");
Console.WriteLine($"Found {sourceMessages.Count()} messages from order-service");

// Query failed messages
var failedMessages = await eventMessageRepository.GetFailedMessagesAsync();
Console.WriteLine($"Found {failedMessages.Count()} failed messages");

// Delete old messages (older than 30 days)
int deletedCount = await eventMessageRepository.DeleteOldMessagesAsync(TimeSpan.FromDays(30));
Console.WriteLine($"Deleted {deletedCount} old messages");
```

## IDeadLetterRepository

The `IDeadLetterRepository` interface provides data access operations for querying and managing dead letter queue entries. It extends the base repository functionality with specialized query methods for finding entries by status, handler, event type, time range, and aggregated statistics. This repository is essential for operational tasks like monitoring failed events, analyzing failure patterns, and implementing cleanup workflows.

Example usage:

```csharp
using DotnetEventBus.Models;
using DotnetEventBus.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

// Setup DI container
var services = new ServiceCollection();
services.AddEventBus(builder => builder
  .Configure(options => options.MaxRetryAttempts = 3)
  .AddDeadLetterService()
  .AddJsonFormatter());

var serviceProvider = services.BuildServiceProvider();
var deadLetterRepository = serviceProvider.GetRequiredService<IDeadLetterRepository>();

// Get statistics about dead letter entries
var pendingCount = await deadLetterRepository.CountByStatusAsync(DeadLetterStatus.Pending);
var failedCount = await deadLetterRepository.CountByStatusAsync(DeadLetterStatus.Failed);
Console.WriteLine($"Pending entries: {pendingCount}, Failed entries: {failedCount}");

// Query entries by event type
var orderFailedEntries = await deadLetterRepository.GetByEventTypeAsync("OrderCreated");
foreach (var entry in orderFailedEntries.Take(5)) // Show first 5
{
    Console.WriteLine($"Failed OrderCreated entry: {entry.Id} - {entry.ErrorMessage}");
}

// Query entries by handler name
var handlerFailedEntries = await deadLetterRepository.GetByHandlerAsync("OrderProcessingHandler");
Console.WriteLine($"Entries failed in OrderProcessingHandler: {handlerFailedEntries.Count()}");

// Get aggregated counts by event type
var countsByEventType = await deadLetterRepository.GetCountsByEventTypeAsync();
foreach (var kvp in countsByEventType)
{
    Console.WriteLine($"Event {kvp.Key}: {kvp.Value} failed entries");
}

// Get aggregated counts by handler
var countsByHandler = await deadLetterRepository.GetCountsByHandlerAsync();
foreach (var kvp in countsByHandler)
{
    Console.WriteLine($"Handler {kvp.Key}: {kvp.Value} failed entries");
}

// Get entries within a time range
var yesterday = DateTime.UtcNow.AddDays(-1);
var today = DateTime.UtcNow;
var recentEntries = await deadLetterRepository.GetByTimeRangeAsync(yesterday, today);
Console.WriteLine($"Recent entries (last 24h): {recentEntries.Count()}");

// Archive old entries (older than 30 days)
int archivedCount = await deadLetterRepository.ArchiveOldEntriesAsync(TimeSpan.FromDays(30));
Console.WriteLine($"Archived {archivedCount} old entries");
```

## ISubscriptionManager

The `ISubscriptionManager` interface provides centralized management and monitoring capabilities for event subscriptions. It allows you to query subscriptions, enable/disable handlers, and retrieve detailed statistics about subscription patterns across your event bus. This service is particularly useful for operational tasks like debugging, monitoring, and dynamic configuration of event handlers.

Example usage:

```csharp
using DotnetEventBus.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

// Setup DI container
var services = new ServiceCollection();
services.AddEventBus(builder => builder
    .Configure(options => options.MaxRetryAttempts = 3)
    .AddDeadLetterService()
    .AddJsonFormatter());

var serviceProvider = services.BuildServiceProvider();
var subscriptionManager = serviceProvider.GetRequiredService<ISubscriptionManager>();

// Define event types
public class OrderCreatedEvent { public int OrderId { get; set; } }
public class PaymentProcessedEvent { public decimal Amount { get; set; } }

// Register handlers (typically done via EventBus.Subscribe())
// For this example, assume we have these handlers registered:
// - OrderCreatedHandler
// - PaymentHandler
// - NotificationHandler

// Get all subscriptions
var allSubscriptions = await subscriptionManager.GetAllSubscriptionsAsync();
foreach (var sub in allSubscriptions)
{
    Console.WriteLine($"Subscription: {sub.HandlerName} for {sub.EventType} (Active: {sub.IsActive})");
}

// Get subscriptions for a specific event type
var orderSubscriptions = await subscriptionManager.GetSubscriptionsAsync(typeof(OrderCreatedEvent).FullName!);
Console.WriteLine($"Found {orderSubscriptions.Count()} subscriptions for OrderCreatedEvent");

// Get subscription count
var count = await subscriptionManager.GetSubscriptionCountAsync(typeof(OrderCreatedEvent).FullName!);
Console.WriteLine($"Total subscriptions for OrderCreatedEvent: {count}");

// Get statistics
var stats = await subscriptionManager.GetStatisticsAsync();
Console.WriteLine($"Total: {stats.TotalSubscriptions}, Active: {stats.ActiveSubscriptions}");
Console.WriteLine($"Unique event types: {stats.UniqueEventTypes}, Unique handlers: {stats.UniqueHandlers}");

// Disable a problematic handler
await subscriptionManager.DisableHandlerAsync("ProblematicHandler");

// Enable a handler after investigation
await subscriptionManager.EnableHandlerAsync("ProblematicHandler");
```

## IHandlerInvoker

The `IHandlerInvoker` interface is responsible for invoking event handlers using reflection and type safety. It provides methods for both regular event handling and request/reply pattern invocation, along with type checking capabilities to determine handler compatibility.

Example usage:

```csharp
using DotnetEventBus.Handlers;
using DotnetEventBus.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

// Define event types
public class OrderCreatedEvent
{
    public int OrderId { get; set; }
    public decimal TotalAmount { get; set; }
}

public class OrderCreatedHandler : IEventHandler<OrderCreatedEvent>
{
    public async Task Handle(OrderCreatedEvent @event, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Processing order {@event.OrderId} for ${@event.TotalAmount}");
        await Task.Delay(100, cancellationToken);
    }
}

public class OrderRequest
{
    public int OrderId { get; set; }
}

public class OrderResponse
{
    public bool Success { get; set; }
}

public class OrderRequestHandler : IEventHandler<OrderRequest>
{
    public async Task<OrderResponse> Handle(OrderRequest request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Processing order request {request.OrderId}");
        await Task.Delay(100, cancellationToken);
        return new OrderResponse { Success = true };
    }
}

// Setup DI container
var services = new ServiceCollection();
services.AddEventBus(builder => builder
    .Configure(options => options.MaxRetryAttempts = 3)
    .AddDeadLetterService()
    .AddJsonFormatter());

var serviceProvider = services.BuildServiceProvider();
var handlerInvoker = serviceProvider.GetRequiredService<IHandlerInvoker>();

// Create handler instance
var handler = new OrderCreatedHandler();

// Invoke event handler
var orderEvent = new OrderCreatedEvent { OrderId = 123, TotalAmount = 99.99m };
await handlerInvoker.InvokeAsync(handler, orderEvent);

// Check if handler can handle event type
bool canHandle = handlerInvoker.CanHandle(handler, typeof(OrderCreatedEvent));
Console.WriteLine($"Can handle OrderCreatedEvent: {canHandle}");

// Get supported event types
var supportedTypes = handlerInvoker.GetSupportedEventTypes(handler);
foreach (var type in supportedTypes)
{
    Console.WriteLine($"Handler supports: {type.Name}");
}

// Invoke request handler (request/reply pattern)
var requestHandler = new OrderRequestHandler();
var request = new OrderRequest { OrderId = 456 };
var response = await handlerInvoker.InvokeRequestAsync(requestHandler, request) as OrderResponse;
Console.WriteLine($"Request handled successfully: {response?.Success}");
```

## IDeadLetterService

The `IDeadLetterService` interface provides operations for managing the dead letter queue (DLQ), which stores failed event processing attempts. It allows you to query, reprocess, review, and archive dead letter entries, providing visibility into failed events and enabling recovery workflows.

Example usage:

```csharp
using DotnetEventBus.Integration;
using DotnetEventBus.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

// Setup DI (typically done in Program.cs or Startup.cs)
var services = new ServiceCollection();
services.AddEventBus(builder => builder
    .AddDeadLetterService()
    // ... other configuration
);

var serviceProvider = services.BuildServiceProvider();
var deadLetterService = serviceProvider.GetRequiredService<IDeadLetterService>();

// Get statistics about the dead letter queue
var stats = await deadLetterService.GetStatisticsAsync();
Console.WriteLine($"Total entries: {stats.TotalEntries}");
Console.WriteLine($"Pending entries: {stats.PendingEntries}");
Console.WriteLine($"Failed reprocessing: {stats.ReprocessFailedEntries}");

// List entries by event type
foreach (var kvp in stats.EntriesByEventType)
{
    Console.WriteLine($"Event {kvp.Key}: {kvp.Value} entries");
}

// Get all pending entries
var pendingEntries = await deadLetterService.GetPendingEntriesAsync();
foreach (var entry in pendingEntries.Take(5)) // Show first 5
{
    Console.WriteLine($"Pending: {entry.Id} - {entry.Message.EventType} - {entry.Status}");
}

// Reprocess a specific entry
bool success = await deadLetterService.ReprocessEntryAsync("entry-id-123");
Console.WriteLine(success ? "Reprocessed successfully" : "Failed to reprocess");

// Batch reprocess all pending entries for a specific event type
var batchResult = await deadLetterService.ReprocessByEventTypeAsync("OrderCreated", maxEntries: 10);
Console.WriteLine($"Batch reprocess: {batchResult.SucceededCount} succeeded, {batchResult.FailedCount} failed");

if (batchResult.FailedCount > 0)
{
    Console.WriteLine("Failed entry IDs:");
    foreach (var id in batchResult.FailedEntryIds)
    {
        Console.WriteLine($"  - {id}");
    }
}

// Mark an entry as reviewed (skip reprocessing)
await deadLetterService.MarkAsReviewedAsync("entry-id-456", "Will fix in next deployment");

// Archive old entries (older than 30 days)
int archivedCount = await deadLetterService.ArchiveOldEntriesAsync(TimeSpan.FromDays(30));
Console.WriteLine($"Archived {archivedCount} old entries");
```

## EventMessage

The `EventMessage` class represents the fundamental unit of communication in the event bus. It encapsulates event data with metadata such as unique identifiers, timestamps, correlation IDs, and headers, enabling reliable event processing, retry mechanisms, and distributed tracing across the system.

Example usage:
```csharp
using DotnetEventBus.Models;
using System;
using System.Collections.Generic;

// Create a new event message for an order creation event
var orderCreatedEvent = new OrderCreated
{
    OrderId = 123,
    CustomerName = "John Doe",
    Amount = 99.99m,
    Items = new List<OrderItem> { new() { ProductId = 1, Quantity = 2, Price = 49.99m } }
};

// Serialize the payload (typically JSON)
var payload = System.Text.Json.JsonSerializer.Serialize(orderCreatedEvent);

// Create the event message
var eventMessage = new EventMessage(
    "OrderCreated",
    payload
)
{
    CorrelationId = "corr-12345",
    Source = "order-service",
    Scope = MessageScope.Distributed
};

// Add custom headers for additional context
// Headers are useful for passing metadata like tenant information, user IDs, etc.
eventMessage.AddHeader("tenantId", "acme-corp");
eventMessage.AddHeader("userId", "user-42");
eventMessage.AddHeader("environment", "production");

// Validate the message before publishing
try
{
    eventMessage.Validate();
    Console.WriteLine("Event message is valid and ready for publishing");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}

// Access message properties
Console.WriteLine($"Message ID: {eventMessage.MessageId}");
Console.WriteLine($"Event Type: {eventMessage.EventType}");
Console.WriteLine($"Created At: {eventMessage.CreatedAtUtc}");
Console.WriteLine($"Correlation ID: {eventMessage.CorrelationId}");
Console.WriteLine($"Source: {eventMessage.Source}");
Console.WriteLine($"Scope: {eventMessage.Scope}");
Console.WriteLine($"Processing Attempts: {eventMessage.ProcessingAttempts}");

// Retrieve a header value
var tenantId = eventMessage.GetHeader("tenantId");
Console.WriteLine($"Tenant ID: {tenantId}");

// Create a retry message when processing fails
// This creates a new message with a new MessageId but preserves correlation, source, and headers
var retryMessage = eventMessage.CreateRetry();
Console.WriteLine($"Retry message ID: {retryMessage.MessageId}");
Console.WriteLine($"Retry attempts: {retryMessage.ProcessingAttempts}");
```

## DeadLetterProcessor

The `DeadLetterProcessor` is a worker component that processes items in the dead letter queue (DLQ). It provides methods to enqueue failed event processing attempts, retrieve statistics about DLQ items, and remove items from the queue. The processor tracks retry attempts, errors, and status information for each dead letter item.

Example usage:

```csharp
using DotnetEventBus.Workers;
using System;
using System.Threading.Tasks;

// Create a dead letter processor
var processor = new DeadLetterProcessor("order-processing-dlq");

// Enqueue a failed event processing attempt
var failedEvent = new DeadLetterItem
{
    Id = Guid.NewGuid().ToString(),
    EventType = "OrderCreated",
    EventData = new { OrderId = 123, Amount = 99.99m },
    ErrorMessage = "Database timeout",
    StackTrace = "...",
    CreatedAt = DateTime.UtcNow,
    LastRetryAt = null,
    RetryCount = 0,
    Status = DeadLetterStatus.Pending
};

processor.Enqueue(failedEvent);

// Get statistics about the dead letter queue
var stats = processor.GetStats();
Console.WriteLine($"Total items: {stats.TotalItems}");
Console.WriteLine($"Pending items: {stats.PendingItems}");
Console.WriteLine($"Retrying items: {stats.RetryingItems}");
Console.WriteLine($"Failed items: {stats.FailedItems}");
Console.WriteLine($"Successful items: {stats.SuccessfulItems}");

// Get all items in the queue
var allItems = processor.GetAllItems();
foreach (var item in allItems)
{
    Console.WriteLine($"Item {item.Id}: {item.EventType} - {item.Status}");
}

// Remove an item after processing
bool removed = processor.RemoveItem("item-id-123");
Console.WriteLine(removed ? "Item removed successfully" : "Item not found");

// Access item properties directly
var itemToProcess = allItems.FirstOrDefault();
if (itemToProcess != null)
{
    Console.WriteLine($"Processing item: {itemToProcess.EventType}");
    Console.WriteLine($"Error: {itemToProcess.ErrorMessage}");
    Console.WriteLine($"Retry count: {itemToProcess.RetryCount}");
}
```

## EventEnvelope

The `EventEnvelope` class wraps an event with metadata and context information, providing a standardized container for event serialization, transmission, and audit trail. It decouples the event payload from infrastructure concerns and includes tracking information like correlation IDs, causation IDs, and processing metadata.

Example usage:
```csharp
using DotnetEventBus.Models;
using System;
using System.Collections.Generic;

// Create a new event envelope
var orderCreatedEvent = new OrderCreated
{
    OrderId = 123,
    CustomerName = "John Doe",
    Amount = 99.99m,
    Items = new List<OrderItem> { new() { ProductId = 1, Quantity = 2, Price = 49.99m } }
};

var envelope = EventEnvelope.Create("order.created", orderCreatedEvent)
{
    Version = 1,
    CorrelationId = "corr-12345",
    Source = "order-service",
    Actor = "user-42",
    IsCritical = true,
    Priority = 90,
    Metadata = new Dictionary<string, object>
    {
        { "tenantId", "acme-corp" },
        { "region", "us-east-1" }
    }
};

// Create a causally linked event (e.g., triggered by another event)
var linkedEvent = EventEnvelope.CreateLinked(
    "order.payment.processed",
    new PaymentProcessed { OrderId = 123, Amount = 99.99m, Status = "completed" },
    causationId: envelope.EventId!,
    correlationId: envelope.CorrelationId
);

// Get headers for transmission
var headers = envelope.GetHeaders();
foreach (var header in headers)
{
    Console.WriteLine($"{header.Key}: {header.Value}");
}

// Validate the envelope
if (envelope.IsValid())
{
    Console.WriteLine("Event envelope is valid and ready for processing");
}
```

## CommandLineInterface

The `CommandLineInterface` class provides a command-line interface for interacting with the event bus. It allows system operators to execute commands for publishing, subscribing, querying, and managing events without writing code. The CLI supports registering custom commands, executing commands with arguments, and retrieving help text for available commands.

Example usage:
```csharp
using DotnetEventBus.Cli;
using System;
using System.Threading.Tasks;

// Create a CLI instance
var cli = new CommandLineInterface();

// Register a custom command
cli.RegisterCommand(new MyCustomCommand());

// Execute a command
var result = await cli.ExecuteAsync("publish", new[] { "OrderCreated", "--data", "{\"OrderId\": 123}" });

if (result.Success)
{
    Console.WriteLine(result.Message);
}
else
{
    Console.WriteLine($"Error: {result.Message}");
}

// Get help text for a specific command
var helpText = cli.GetCommandHelp("publish");
Console.WriteLine(helpText);

// Get all available commands
var allCommands = cli.GetAllCommands();
foreach (var command in allCommands)
{
    Console.WriteLine($"{command.Name}: {command.Description}");
}
```
using DotnetEventBus.Integration;
using System;
using System.Threading.Tasks;

// Create a circuit breaker that opens after 5 failures
var circuitBreaker = new CircuitBreaker(failureThreshold: 5, timeout: TimeSpan.FromSeconds(30));

// Execute an operation with circuit breaker protection
try
{
    var result = await circuitBreaker.ExecuteAsync(async () =>
    {
        // Simulate an operation that may fail
        await Task.Delay(100);
        return "Success";
    });
    Console.WriteLine($"Operation succeeded: {result}");
}
catch (CircuitBreakerOpenException ex)
{
    Console.WriteLine($"Service unavailable: {ex.Message}");
}

// Execute a void operation with circuit breaker protection
try
{
    await circuitBreaker.ExecuteAsync(async () =>
    {
        // Simulate an operation that may fail
        await Task.Delay(100);
    });
    Console.WriteLine("Operation completed successfully");
}
catch (CircuitBreakerOpenException ex)
{
    Console.WriteLine($"Service unavailable: {ex.Message}");
}

// Manually reset the circuit breaker
circuitBreaker.Reset();

## MetricsCollector

The `MetricsCollector` class collects and aggregates metrics about event processing. It tracks event publishing counts, handler execution times, failure rates, and latency statistics to provide observability into system health and performance bottlenecks.

Use `MetricsCollector` to monitor your event bus performance, identify slow handlers, and track error rates in production or during load testing.

Example usage:
```csharp
using DotnetEventBus.Advanced;
using System;
using System.Threading.Tasks;

// Create a metrics collector
var metricsCollector = new MetricsCollector();

// Record event publishing
metricsCollector.RecordEventPublished("OrderCreated", 42);
metricsCollector.RecordEventPublished("OrderCreated", 38);
metricsCollector.RecordEventPublished("PaymentProcessed", 25);

// Record handler executions
metricsCollector.RecordHandlerExecution("OrderCreatedHandler", "OrderCreated", 120, true);
metricsCollector.RecordHandlerExecution("OrderCreatedHandler", "OrderCreated", 115, true);
metricsCollector.RecordHandlerExecution("PaymentHandler", "PaymentProcessed", 45, true);

// Record a failed event
try
{
    // Simulate failed event handling
    throw new InvalidOperationException("Database unavailable");
}
catch (Exception ex)
{
    metricsCollector.RecordEventFailed("OrderCreated", "OrderCreatedHandler", ex);
}

// Get metrics
var orderMetrics = metricsCollector.GetEventMetrics("OrderCreated");
Console.WriteLine($"OrderCreated: {orderMetrics?.PublishCount} published, {orderMetrics?.FailureCount} failed");

var handlerMetrics = metricsCollector.GetHandlerMetrics("OrderCreatedHandler", "OrderCreated");
Console.WriteLine($"Handler success rate: {handlerMetrics?.SuccessRate}%");

// Get system-wide metrics
var systemMetrics = metricsCollector.GetSystemMetrics();
Console.WriteLine($"Total throughput: {systemMetrics.ThroughputPerSecond} events/sec");

// Get latency statistics
var latencyStats = metricsCollector.GetLatencyStats("OrderCreated");
Console.WriteLine($"P95 latency: {latencyStats?.P95Ms}ms");

// Reset metrics when needed
metricsCollector.Reset();
```

## RequestResponseBus

The `RequestResponseBus` class implements the request-response pattern on top of an asynchronous event bus. It allows handlers to return responses to specific requests and clients to wait for these replies synchronously with configurable timeouts.

Example usage:
```csharp
using DotnetEventBus.Advanced;
using System;
using System.Threading.Tasks;

// Create the bus, optionally providing a publisher function to link with your transport
var bus = new RequestResponseBus(async (eventType, message) =>
{
    Console.WriteLine($"Publishing request {message.RequestId} to {eventType}");
    await Task.CompletedTask;
});

// Send a request and wait for a response
try
{
    var request = new MyRequest { Data = "Hello" };
    var response = await bus.RequestAsync<MyRequest, MyResponse>("MyEvent", request);
    Console.WriteLine($"Received response: {response?.Payload}");
}
catch (TimeoutException)
{
    Console.WriteLine("Request timed out");
}

// In a handler, use RequestResponseHandler to process requests and send responses
public class MyRequestHandler : RequestResponseHandler<MyRequest, MyResponse>
{
    public override async Task<MyResponse> HandleAsync(MyRequest request)
    {
        await Task.Delay(100);
        return new MyResponse { Payload = "World" };
    }
}
```
