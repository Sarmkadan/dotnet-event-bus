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
