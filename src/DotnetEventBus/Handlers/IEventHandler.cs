#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetEventBus.Handlers;

/// <summary>
/// Base interface for all event handlers.
/// </summary>
public interface IEventHandler
{
    /// <summary>
    /// Gets the type of event this handler processes.
    /// </summary>
    Type GetEventType();

    /// <summary>
    /// Gets a display name for this handler.
    /// </summary>
    string GetHandlerName();
}

/// <summary>
/// Handles events of a specific type synchronously.
/// </summary>
public interface IEventHandler<in TEvent> : IEventHandler
    where TEvent : class
{
    /// <summary>
    /// Handles the event.
    /// </summary>
    Task Handle(TEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handles request/reply patterns with a request and response type.
/// </summary>
public interface IRequestHandler<in TRequest, TResponse> : IEventHandler
    where TRequest : class
    where TResponse : class
{
    /// <summary>
    /// Handles the request and returns a response.
    /// </summary>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Notification handler that processes events without expecting a response.
/// </summary>
public interface INotificationHandler<in TNotification> : IEventHandler
    where TNotification : class
{
    /// <summary>
    /// Handles the notification.
    /// </summary>
    Task Handle(TNotification notification, CancellationToken cancellationToken = default);
}

/// <summary>
/// Polymorphic handler that can handle events of any type using dynamic dispatch.
/// </summary>
public interface IPolymorphicHandler : IEventHandler
{
    /// <summary>
    /// Handles any event object.
    /// </summary>
    Task HandleDynamic(object @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if this handler can process the given event type.
    /// </summary>
    bool CanHandle(Type eventType);

    /// <summary>
    /// Gets all event types this handler can process.
    /// </summary>
    IEnumerable<Type> GetSupportedEventTypes();
}

/// <summary>
/// Exception handler that processes exceptions thrown by other handlers.
/// </summary>
public interface IExceptionHandler : IEventHandler
{
    /// <summary>
    /// Handles an exception that occurred during event processing.
    /// </summary>
    Task HandleException(
        string eventType,
        object? eventData,
        Exception exception,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if this exception handler can process the given exception type.
    /// </summary>
    bool CanHandle(Type exceptionType);
}

/// <summary>
/// Interceptor handler that can inspect and modify messages before/after processing.
/// </summary>
public interface IMessageInterceptor : IEventHandler
{
    /// <summary>
    /// Called before a message is published.
    /// </summary>
    Task OnBeforePublish(DotnetEventBus.Models.EventMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called after a message is successfully published.
    /// </summary>
    Task OnAfterPublish(DotnetEventBus.Models.EventMessage message, DotnetEventBus.Models.PublishResult result, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when publishing fails.
    /// </summary>
    Task OnPublishFailed(DotnetEventBus.Models.EventMessage message, Exception exception, CancellationToken cancellationToken = default);
}
