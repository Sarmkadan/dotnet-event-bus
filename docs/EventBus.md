# Dotnet EventBus Architecture

## Overview

The `EventBus` is an in-process event bus implementation supporting publish/subscribe and request/reply patterns. It is designed to be lightweight, extensible, and integrates with dependency injection, providing middleware support for cross-cutting concerns.

## Architecture

### Key Components

1. **EventBus Service** (`DotnetEventBus.Services.EventBus`)
   - Core implementation of the event bus
   - Implements `IEventBus` interface
   - Handles publishing, subscribing, and request/reply patterns

2. **Repositories** (Abstracted via interfaces)
   - `IEventMessageRepository` - Stores event messages
   - `ISubscriptionRepository` - Manages event subscriptions
   - `IDeadLetterRepository` - Stores failed event deliveries

3. **Services**
   - `IDeadLetterService` - Handles dead letter queue operations

4. **Formatters**
   - `IEventFormatter` - Serializes/deserializes event payloads (default: JSON)

5. **Middleware**
   - `IEventBusMiddleware` - Pipeline for cross-cutting concerns (logging, validation, etc.)

6. **Configuration**
   - `EventBusOptions` - Configurable behavior (concurrency, retries, dead letter, etc.)

### Dependency Injection

The EventBus is designed to work with .NET's dependency injection container. Key dependencies include:
- `IEventMessageRepository`
- `ISubscriptionRepository`
- `IDeadLetterRepository`
- `IDeadLetterService`
- `IEventFormatter`
- `IServiceProvider` (for resolving middleware)

When repositories are not provided via DI, the EventBus falls back to in-memory implementations.

## Publish/Subscribe Flow

### Publishing Events

1. **Validation**
   - Checks for null event
   - Validates event message

2. **Message Creation**
   - Serializes event to JSON payload
   - Creates `EventMessage` with metadata (MessageId, CorrelationId, Timestamp)

3. **Subscription Resolution**
   - Looks up subscriptions for the event type and all base types/interfaces
   - Orders handlers by priority (descending)
   - Avoids duplicate handler invocation for the same handler instance

4. **Middleware Pipeline**
   - Constructs event context
   - Builds middleware chain (from inside out)
   - Executes pipeline with terminal delegate that invokes handlers

5. **Handler Invocation**
   - Supports parallel or sequential handling (configurable)
   - Implements retry logic with exponential backoff
   - Enforces concurrency limits via semaphore
   - Handles timeouts per handler

6. **Result Tracking**
   - Tracks successful/failed handlers
   - Updates publish result with timing and status
   - Logs publication summary

7. **Error Handling**
   - On no handlers: logs warning, optionally throws exception or sends to dead letter
   - On handler failure: retries configurable times, then sends to dead letter if enabled
   - On unexpected failure: returns failed publish result or throws based on configuration

### Subscribing to Events

1. **Subscription Creation**
   - Creates `Subscription` object with:
     - Event type
     - Handler delegate (async or sync)
     - Handler name (auto-generated if not provided)
     - Priority (default 0)
     - Timeout (uses default if not specified)

2. **Thread Safety**
   - Uses lock on `_subscriptionLock` to ensure thread-safe subscription management

3. **Disposal Pattern**
   - Returns `IDisposable` (`SubscriptionDisposable`) for automatic unsubscription
   - Disposal calls `UnsubscribeAsync` to remove subscription

### Request/Reply Pattern

> **Note**: The request/reply pattern requires distributed transport configuration and is currently not implemented (throws `NotImplementedException`). 
> To use request/reply, you must configure distributed transport settings in `EventBusOptions`.

## Usage Examples

### Basic Setup with Dependency Injection

```csharp
// In Startup.cs or Program.cs
services.AddSingleton<EventBusOptions>(options => 
{
    var opts = new EventBusOptions();
    opts.MaxConcurrentHandlers = 10;
    opts.MaxRetryAttempts = 3;
    opts.EnableDeadLetterQueue = true;
    return opts;
});

services.AddSingleton<IEventBus, EventBus>();
services.AddSingleton<IEventMessageRepository, InMemoryEventMessageRepository>();
services.AddSingleton<ISubscriptionRepository, InMemorySubscriptionRepository>();
services.AddSingleton<IDeadLetterRepository, InMemoryDeadLetterRepository>();
services.AddSingleton<IDeadLetterService, DeadLetterService>();
services.AddSingleton<IEventFormatter, JsonEventFormatter>();

// Add middleware
services.AddScoped<IEventBusMiddleware, LoggingMiddleware>();
services.AddScoped<IEventBusMiddleware, ValidationMiddleware>();
```

### Publishing an Event

```csharp
public class UserCreatedEvent
{
    public string UserId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
}

// In your service
public class UserService
{
    private readonly IEventBus _eventBus;
    
    public UserService(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }
    
    public async Task CreateUserAsync(UserCreatedEvent @event)
    {
        // Business logic...
        
        // Publish event
        await _eventBus.PublishAsync(@event);
    }
}
```

### Subscribing to an Event

```csharp
public class UserCreatedHandler : IEventHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedHandler> _logger;
    
    public UserCreatedHandler(ILogger<UserCreatedHandler> logger)
    {
        _logger = logger;
    }
    
    public string GetHandlerName() => nameof(UserCreatedHandler);
    
    public Task Handle(UserCreatedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User created: {UserId} ({Username})", 
            @event.UserId, @event.Username);
        
        // Handle the event (send welcome email, update cache, etc.)
        return Task.CompletedTask;
    }
}

// In your service registration or startup
public void ConfigureServices(IServiceCollection services)
{
    // ... other services
    
    services.AddHostedService<UserCreatedHandler>();
}

// Or manual subscription
public class SomeService
{
    private readonly IEventBus _eventBus;
    private IDisposable? _subscription;
    
    public SomeService(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }
    
    public void Start()
    {
        _subscription = _eventBus.Subscribe<UserCreatedEvent>(HandleUserCreated);
    }
    
    public void Stop()
    {
        _subscription?.Dispose();
    }
    
    private Task HandleUserCreated(UserCreatedEvent @event, CancellationToken ct)
    {
        // Handle event
        return Task.CompletedTask;
    }
}
```

### Subscribing with Lambda Expression

```csharp
var subscription = _eventBus.Subscribe<UserCreatedEvent>(
    async (@event, ct) => 
    {
        await _emailService.SendWelcomeEmailAsync(@event.UserId, @event.Email);
    },
    handlerName: "WelcomeEmailHandler",
    priority: 10
);

// Remember to dispose when done
// subscription.Dispose();
```

### Synchronous Handler Subscription

```csharp
var subscription = _eventBus.SubscribeSync<UserCreatedEvent>(@event =>
{
    _cache.UpdateUser(@event.UserId, @event.Username);
}, handlerName: "CacheUpdateHandler");
```

### Handling No Subscribers

```csharp
try
{
    var result = await _eventBus.PublishAsync(new UserCreatedEvent());
    if (!result.Success)
    {
        // Handle failed handlers
    }
}
catch (NoHandlersRegisteredException)
{
    // No handlers registered for this event type
}
```

### Configuration Options

```csharp
var options = new EventBusOptions
{
    MaxConcurrentHandlers = 5,
    DefaultHandlerTimeout = TimeSpan.FromSeconds(30),
    MaxRetryAttempts = 3,
    RetryBackoffMode = RetryBackoffMode.Exponential,
    EnableDeadLetterQueue = true,
    ThrowOnNoHandlers = false,
    DeadLetterOnNoHandlers = true,
    AllowParallelHandling = true
};
```

## Middleware Pipeline

Middleware components implement `IEventBusMiddleware` and can perform actions before and after handler execution.

```csharp
public class LoggingMiddleware : IEventBusMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;
    
    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
    }
    
    public async Task InvokeAsync(EventMiddlewareContext context, EventMiddlewareDelegate next)
    {
        _logger.LogInformation("Before handling event: {EventType}", 
            context.EventMessage.EventType);
        
        await next(context);
        
        _logger.LogInformation("After handling event: {EventType} - Success: {Success}", 
            context.EventMessage.EventType, 
            context.Result?.Success ?? false);
    }
}
```

## Thread Safety

- Subscription management is thread-safe using locks
- Handler invocation respects concurrency limits via `SemaphoreSlim`
- All repository operations are expected to be thread-safe (implementations should ensure this)

## Error Handling and Dead Letter Queue

- Failed handlers are retried according to configuration
- After max retries, failed events are sent to dead letter queue if enabled
- Events with no handlers can optionally be sent to dead letter queue
- Dead letter entries contain original event, exception details, and retry count

## Limitations

1. **In-Process Only**: Current implementation is for in-process communication only
2. **Request/Reply**: Requires distributed transport configuration (not implemented)
3. **Serialization**: Default JSON formatter; custom formatters can be plugged in
4. **Persistence**: Repositories are abstracted; in-memory implementations provided for development

## Conventional Commit

Type: docs
Scope: EventBus
Description: Document EventBus architecture, publish/subscribe flow, and usage examples