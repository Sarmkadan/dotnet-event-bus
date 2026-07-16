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
