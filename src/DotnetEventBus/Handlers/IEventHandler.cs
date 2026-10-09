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
/// <typeparam name="TEvent">The event type.</typeparam>
public interface IEventHandler<in TEvent> : IEventHandler
    where TEvent : class
{
    /// <summary>
    /// Handles the event.
    /// </summary>
    /// <param name="event">The event to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task Handle(TEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handles request/reply patterns with a request and response type.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IRequestHandler<in TRequest, TResponse> : IEventHandler
    where TRequest : class
    where TResponse : class
{
    /// <summary>
    /// Handles the request and returns a response.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the response.</returns>
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
    /// <param name="notification">The notification to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
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
    /// <param name="event">The event object to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task HandleDynamic(object @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if this handler can process the given event type.
    /// </summary>
    /// <param name="eventType">The event type to check.</param>
    /// <returns><c>true</c> if this handler can process the event type; otherwise, <c>false</c>.</returns>
    bool CanHandle(Type eventType);

    /// <summary>
    /// Gets all event types this handler can process.
    /// </summary>
    /// <returns>The supported event types.</returns>
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
    /// <param name="eventType">The name of the event type being processed.</param>
    /// <param name="eventData">The event data, or <c>null</c> if unavailable.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task HandleException(
        string eventType,
        object? eventData,
        Exception exception,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if this exception handler can process the given exception type.
    /// </summary>
    /// <param name="exceptionType">The exception type to check.</param>
    /// <returns><c>true</c> if this handler can process the exception type; otherwise, <c>false</c>.</returns>
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
    /// <param name="message">The message about to be published.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task OnBeforePublish(DotnetEventBus.Models.EventMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called after a message is successfully published.
    /// </summary>
    /// <param name="message">The published message.</param>
    /// <param name="result">The result of the publish operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task OnAfterPublish(DotnetEventBus.Models.EventMessage message, DotnetEventBus.Models.PublishResult result, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when publishing fails.
    /// </summary>
    /// <param name="message">The message that failed to publish.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task OnPublishFailed(DotnetEventBus.Models.EventMessage message, Exception exception, CancellationToken cancellationToken = default);
}
