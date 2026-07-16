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
