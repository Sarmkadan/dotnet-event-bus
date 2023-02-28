# API Reference

Complete API documentation for DotnetEventBus v1.2.0.

## Core Interfaces

### IEventBus

Main interface for event pub-sub operations.

```csharp
public interface IEventBus
{
    /// <summary>
    /// Publishes an event to all registered subscribers.
    /// </summary>
    /// <typeparam name="TEvent">The event type</typeparam>
    /// <param name="event">The event instance</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing handler invocation metrics</returns>
    Task<PublishResult> PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default) 
        where TEvent : class;

    /// <summary>
    /// Request-reply pattern: publishes request and waits for response.
    /// </summary>
    /// <typeparam name="TRequest">Request type</typeparam>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="request">Request instance</param>
    /// <param name="timeout">Response timeout (default: 5 seconds)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The response from handler</returns>
    /// <exception cref="TimeoutException">If no response received within timeout</exception>
    Task<TResponse> RequestAsync<TRequest, TResponse>(
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class;

    /// <summary>
    /// Subscribes an async handler to an event type.
    /// </summary>
    /// <typeparam name="TEvent">Event type</typeparam>
    /// <param name="handler">Async handler function</param>
    /// <param name="handlerName">Unique handler identifier</param>
    /// <param name="priority">Execution priority (higher = earlier)</param>
    /// <param name="filter">Optional event filter</param>
    void Subscribe<TEvent>(
        Func<TEvent, CancellationToken, Task> handler,
        string handlerName,
        int priority = 0,
        IEventFilter filter = null)
        where TEvent : class;

    /// <summary>
    /// Subscribes a synchronous handler to an event type.
    /// </summary>
    /// <typeparam name="TEvent">Event type</typeparam>
    /// <param name="handler">Sync handler function</param>
    /// <param name="handlerName">Unique handler identifier</param>
    /// <param name="priority">Execution priority</param>
    /// <param name="filter">Optional event filter</param>
    void SubscribeSync<TEvent>(
        Action<TEvent> handler,
        string handlerName,
        int priority = 0,
        IEventFilter filter = null)
        where TEvent : class;

    /// <summary>
    /// Unsubscribes a handler from all event types.
    /// </summary>
    /// <param name="handlerName">Handler to unsubscribe</param>
    Task UnsubscribeAsync(string handlerName);

    /// <summary>
    /// Gets all subscriptions for an event type.
    /// </summary>
    /// <typeparam name="TEvent">Event type</typeparam>
    /// <returns>List of subscriptions</returns>
    Task<IReadOnlyList<Subscription>> GetSubscriptionsAsync<TEvent>() 
        where TEvent : class;

    /// <summary>
    /// Gets all subscriptions for an event type by name.
    /// </summary>
    /// <param name="eventTypeName">Full event type name</param>
    /// <returns>List of subscriptions</returns>
    Task<IReadOnlyList<Subscription>> GetSubscriptionsAsync(string eventTypeName);
}
```

### IEventHandler<TEvent>

Base interface for event handlers.

```csharp
public interface IEventHandler<TEvent> where TEvent : class
{
    /// <summary>
    /// Handles the event asynchronously.
    /// </summary>
    /// <param name="event">The event to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task Handle(TEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base class for event handlers with automatic DI support.
/// </summary>
public abstract class EventHandlerBase<TEvent> : IEventHandler<TEvent> where TEvent : class
{
    public abstract Task Handle(TEvent @event, CancellationToken cancellationToken = default);
}
```

### IDeadLetterService

Management of failed events.

```csharp
public interface IDeadLetterService
{
    /// <summary>
    /// Gets all pending dead letter entries waiting for reprocessing.
    /// </summary>
    /// <returns>List of dead letter entries</returns>
    Task<IReadOnlyList<DeadLetterEntry>> GetPendingEntriesAsync();

    /// <summary>
    /// Reprocesses a dead letter entry.
    /// </summary>
    /// <param name="entryId">Entry ID</param>
    Task ReprocessEntryAsync(string entryId);

    /// <summary>
    /// Permanently deletes a dead letter entry.
    /// </summary>
    /// <param name="entryId">Entry ID</param>
    Task DeleteEntryAsync(string entryId);

    /// <summary>
    /// Gets statistics about dead letter entries.
    /// </summary>
    /// <returns>Statistics object</returns>
    Task<DeadLetterStatistics> GetStatisticsAsync();
}

public class DeadLetterStatistics
{
    public int PendingEntries { get; set; }
    public int TotalFailedEntries { get; set; }
    public int ReprocessedEntries { get; set; }
    public DateTime OldestEntry { get; set; }
    public Dictionary<string, int> FailuresByEventType { get; set; }
}
```

### ISubscriptionManager

Manages event subscriptions at runtime.

```csharp
public interface ISubscriptionManager
{
    /// <summary>
    /// Gets all subscriptions for an event type.
    /// </summary>
    /// <param name="eventTypeName">Event type name</param>
    /// <returns>List of subscriptions</returns>
    Task<IReadOnlyList<Subscription>> GetSubscriptionsAsync(string eventTypeName);

    /// <summary>
    /// Disables a specific handler.
    /// </summary>
    /// <param name="handlerName">Handler name</param>
    Task DisableHandlerAsync(string handlerName);

    /// <summary>
    /// Enables a disabled handler.
    /// </summary>
    /// <param name="handlerName">Handler name</param>
    Task EnableHandlerAsync(string handlerName);

    /// <summary>
    /// Gets statistics for all handlers.
    /// </summary>
    /// <returns>Dictionary of handler statistics</returns>
    Task<IReadOnlyDictionary<string, SubscriptionStatistics>> GetStatisticsAsync();
}

public class Subscription
{
    public string HandlerName { get; set; }
    public string EventTypeName { get; set; }
    public int Priority { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime RegisteredAt { get; set; }
}

public class SubscriptionStatistics
{
    public int InvocationCount { get; set; }
    public int FailureCount { get; set; }
    public int SuccessCount { get; set; }
    public double AverageDuration { get; set; }
    public double MaxDuration { get; set; }
    public double MinDuration { get; set; }
}
```

### IBatchEventPublisher

Efficient batch event publishing.

```csharp
public interface IBatchEventPublisher
{
    /// <summary>
    /// Adds an event to the batch without publishing.
    /// </summary>
    /// <typeparam name="TEvent">Event type</typeparam>
    /// <param name="event">Event instance</param>
    Task AddEventAsync<TEvent>(TEvent @event) where TEvent : class;

    /// <summary>
    /// Publishes all accumulated events in the batch.
    /// </summary>
    Task FlushAsync();

    /// <summary>
    /// Clears the batch without publishing.
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// Gets the current number of events in the batch.
    /// </summary>
    int GetBatchSize();
}
```

## Configuration

### EventBusOptions

Configuration options for event bus behavior.

```csharp
public class EventBusOptions
{
    /// <summary>
    /// Maximum number of retry attempts for failed handlers. Default: 3
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Allow handlers to execute in parallel. Default: true
    /// </summary>
    public bool AllowParallelHandling { get; set; } = true;

    /// <summary>
    /// Maximum concurrent handler executions. Default: CPU count
    /// </summary>
    public int MaxConcurrentHandlers { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Individual handler execution timeout. Default: 30 seconds
    /// </summary>
    public TimeSpan DefaultHandlerTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Enable dead letter queue for failed events. Default: true
    /// </summary>
    public bool EnableDeadLetterQueue { get; set; } = true;

    /// <summary>
    /// Exponential backoff multiplier. Default: 2.0
    /// </summary>
    public double RetryDelayMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Initial retry delay in milliseconds. Default: 100
    /// </summary>
    public int InitialRetryDelayMs { get; set; } = 100;

    /// <summary>
    /// Enable metrics collection. Default: true
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Enable detailed logging. Default: false
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;
}
```

### EventBusBuilder

Fluent configuration API.

```csharp
public class EventBusBuilder
{
    public EventBusBuilder WithMaxRetries(int attempts);
    public EventBusBuilder WithParallelHandling(bool allow);
    public EventBusBuilder WithMaxConcurrentHandlers(int max);
    public EventBusBuilder WithHandlerTimeout(TimeSpan timeout);
    public EventBusBuilder WithDeadLetterQueue(bool enable);
    public EventBusBuilder WithRetryBackoff(double multiplier);
    public EventBusBuilder WithMetrics(bool enable);
    public EventBusBuilder WithDetailedLogging(bool enable);
    public void Build();
}
```

## Models & DTOs

### PublishResult

Result information from publishing an event.

```csharp
public class PublishResult
{
    public string EventId { get; set; }
    public string EventTypeName { get; set; }
    public int HandlersInvoked { get; set; }
    public int HandlersSucceeded { get; set; }
    public int HandlersFailed { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime PublishedAt { get; set; }
    public List<HandlerResult> HandlerResults { get; set; }
}

public class HandlerResult
{
    public string HandlerName { get; set; }
    public bool IsSuccess { get; set; }
    public Exception Exception { get; set; }
    public TimeSpan Duration { get; set; }
}
```

### EventMessage

Represents an event in the event store.

```csharp
public class EventMessage
{
    public string Id { get; set; }
    public string EventTypeName { get; set; }
    public object Payload { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public string CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ProcessedAt { get; set; }
    public int RetryCount { get; set; }
}
```

### DeadLetterEntry

Represents a failed event in dead letter queue.

```csharp
public class DeadLetterEntry
{
    public string Id { get; set; }
    public string EventType { get; set; }
    public object EventData { get; set; }
    public Exception LastException { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; }
    public DateTime FailedAt { get; set; }
    public DateTime NextRetryAt { get; set; }
}
```

## Extensions

### Service Collection Extensions

```csharp
public static class ServiceCollectionExtensions
{
    // Add event bus with options
    public static IServiceCollection AddEventBus(
        this IServiceCollection services,
        Action<EventBusOptions> configure = null);

    // Add with custom repositories
    public static IServiceCollection AddEventBus(
        this IServiceCollection services,
        IEventMessageRepository messageRepo,
        ISubscriptionRepository subscriptionRepo,
        IDeadLetterRepository deadLetterRepo,
        Action<EventBusOptions> configure = null);

    // Fluent builder
    public static EventBusBuilder AddEventBusBuilder(
        this IServiceCollection services);
}
```

## Utilities

### Validation Helper

Fluent validation API.

```csharp
public static class ValidationHelper
{
    public static ValidationRule BeRequired();
    public static ValidationRule BeEmail();
    public static ValidationRule BeMinLength(int length);
    public static ValidationRule BeMaxLength(int length);
    public static ValidationRule Match(string pattern);
}
```

### Type Extensions

Reflection utilities for event types.

```csharp
public static class TypeExtensions
{
    public static bool IsEvent(this Type type);
    public static bool IsHandler(this Type type);
    public static IEnumerable<Type> GetEventTypes(this Type type);
    public static object CreateInstance(this Type type);
}
```

### Collection Extensions

Collection manipulation utilities.

```csharp
public static class CollectionExtensions
{
    public static IEnumerable<IEnumerable<T>> Batch<T>(
        this IEnumerable<T> source, 
        int batchSize);
    
    public static IEnumerable<T> Page<T>(
        this IEnumerable<T> source, 
        int pageNumber, 
        int pageSize);
    
    public static Dictionary<TKey, List<TValue>> GroupByList<T, TKey, TValue>(
        this IEnumerable<T> source, 
        Func<T, TKey> keySelector, 
        Func<T, TValue> valueSelector);
}
```

## Middleware

### Pipeline Middleware

```csharp
public interface IPipelineMiddleware
{
    Task<PublishResult> ExecuteAsync(
        IPublishContext context,
        Func<Task<PublishResult>> next);
}

public interface IPublishContext
{
    object Event { get; }
    Type EventType { get; }
    string CorrelationId { get; }
    Dictionary<string, object> Metadata { get; }
    CancellationToken CancellationToken { get; }
}
```

---

For examples and detailed usage, see the `/examples` directory and `getting-started.md`.
