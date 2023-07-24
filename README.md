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
