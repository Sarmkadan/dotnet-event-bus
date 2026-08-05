#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Handlers;

/// <summary>
/// Base class for implementing event handlers with built‑in logging.
/// </summary>
public abstract class EventHandlerBase<TEvent> : IEventHandler<TEvent>
    where TEvent : class
{
    protected readonly ILogger? Logger;

    protected EventHandlerBase(ILogger? logger = null)
    {
        Logger = logger;
    }

    /// <summary>
    /// Gets the type of event this handler processes.
    /// </summary>
    public virtual Type GetEventType()
    {
        Logger?.LogInformation("GetEventType called, returning {EventType}", typeof(TEvent));
        return typeof(TEvent);
    }

    /// <summary>
    /// Gets a display name for this handler.
    /// </summary>
    public virtual string GetHandlerName()
    {
        var name = GetType().Name;
        Logger?.LogInformation("GetHandlerName called, returning {HandlerName}", name);
        return name;
    }

    /// <summary>
    /// Handles the event. Override this method to implement your handler logic.
    /// </summary>
    public abstract Task Handle(TEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called before handling the event. Override to add custom logic.
    /// </summary>
    protected virtual Task OnBeforeHandle(TEvent @event, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called after successful event handling. Override to add custom logic.
    /// </summary>
    protected virtual Task OnAfterHandle(TEvent @event, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when an exception occurs during handling. Override to add custom error handling.
    /// </summary>
    protected virtual Task OnError(TEvent @event, Exception exception, CancellationToken cancellationToken = default)
    {
        Logger?.LogError(exception, "Error handling event {EventType}", typeof(TEvent).Name);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes the handler with before/after hooks and error handling.
    /// </summary>
    protected async Task ExecuteWithHooks(TEvent @event, CancellationToken cancellationToken)
    {
        Logger?.LogInformation("ExecuteWithHooks started for event {EventType}", typeof(TEvent).Name);
        try
        {
            await OnBeforeHandle(@event, cancellationToken);
            await Handle(@event, cancellationToken);
            await OnAfterHandle(@event, cancellationToken);
            Logger?.LogInformation("ExecuteWithHooks completed for event {EventType}", typeof(TEvent).Name);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "ExecuteWithHooks failed for event {EventType}", typeof(TEvent).Name);
            await OnError(@event, ex, cancellationToken);
            throw;
        }
    }
}

/// <summary>
/// Base class for notification handlers that don't expect a response.
/// </summary>
public abstract class NotificationHandlerBase<TNotification> : INotificationHandler<TNotification>
    where TNotification : class
{
    protected readonly ILogger? Logger;

    protected NotificationHandlerBase(ILogger? logger = null)
    {
        Logger = logger;
    }

    public virtual Type GetEventType()
    {
        Logger?.LogInformation("GetEventType called, returning {EventType}", typeof(TNotification));
        return typeof(TNotification);
    }

    public virtual string GetHandlerName()
    {
        var name = GetType().Name;
        Logger?.LogInformation("GetHandlerName called, returning {HandlerName}", name);
        return name;
    }

    public abstract Task Handle(TNotification notification, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base class for request/response handlers.
/// </summary>
public abstract class RequestHandlerBase<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    protected readonly ILogger? Logger;

    protected RequestHandlerBase(ILogger? logger = null)
    {
        Logger = logger;
    }

    public virtual Type GetEventType()
    {
        Logger?.LogInformation("GetEventType called, returning {EventType}", typeof(TRequest));
        return typeof(TRequest);
    }

    public virtual string GetHandlerName()
    {
        var name = GetType().Name;
        Logger?.LogInformation("GetHandlerName called, returning {HandlerName}", name);
        return name;
    }

    public abstract Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the request before processing.
    /// Return null if validation passed, or an error response if validation failed.
    /// </summary>
    protected virtual Task<TResponse?> ValidateRequest(TRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<TResponse?>(null);
    }
}

/// <summary>
/// Base class for polymorphic handlers that process multiple event types.
/// </summary>
public abstract class PolymorphicHandlerBase : IPolymorphicHandler
{
    protected readonly ILogger? Logger;
    protected readonly HashSet<Type> SupportedTypes = new();

    protected PolymorphicHandlerBase(ILogger? logger = null)
    {
        Logger = logger;
    }

    public virtual Type GetEventType()
    {
        Logger?.LogInformation("GetEventType called, returning {EventType}", typeof(object));
        return typeof(object);
    }

    public virtual string GetHandlerName()
    {
        var name = GetType().Name;
        Logger?.LogInformation("GetHandlerName called, returning {HandlerName}", name);
        return name;
    }

    public abstract Task HandleDynamic(object @event, CancellationToken cancellationToken = default);

    public virtual bool CanHandle(Type eventType)
    {
        var canHandle = SupportedTypes.Contains(eventType) || SupportedTypes.Any(t => t.IsAssignableFrom(eventType));
        if (!canHandle)
        {
            Logger?.LogWarning("CanHandle called for unsupported event type {EventType}", eventType);
        }
        return canHandle;
    }

    public virtual IEnumerable<Type> GetSupportedEventTypes() => SupportedTypes.AsReadOnly();

    /// <summary>
    /// Register a type that this handler can process.
    /// </summary>
    protected void RegisterHandledType(Type eventType)
    {
        if (eventType is null)
            throw new ArgumentNullException(nameof(eventType));

        Logger?.LogInformation("RegisterHandledType called for {EventType}", eventType);
        SupportedTypes.Add(eventType);
    }

    /// <summary>
    /// Register multiple types that this handler can process.
    /// </summary>
    protected void RegisterHandledTypes(params Type[] eventTypes)
    {
        var count = eventTypes?.Length ?? 0;
        Logger?.LogInformation("RegisterHandledTypes called for {Count} types", count);
        foreach (var type in eventTypes ?? Array.Empty<Type>())
        {
            RegisterHandledType(type);
        }
    }
}
