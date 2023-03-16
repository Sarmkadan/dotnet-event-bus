#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetEventBus.Models;
using DotnetEventBus.Handlers;

namespace DotnetEventBus.Services;

/// <summary>
/// Core interface for the event bus providing pub/sub, request/reply, and message handling capabilities.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an event to all registered handlers.
    /// </summary>
    /// <param name="event">The event object to publish.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the result of the publish operation.</returns>
    Task<PublishResult> PublishAsync<TEvent>(
        TEvent @event,
        string? correlationId = null,
        CancellationToken cancellationToken = default) where TEvent : class;

    /// <summary>
    /// Publishes an event as an object (for dynamic publishing).
    /// </summary>
    /// <param name="event">The event object to publish.</param>
    /// <param name="eventType">The type of the event.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the result of the publish operation.</returns>
    Task<PublishResult> PublishAsync(
        object @event,
        Type eventType,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a message and waits for a response (request/reply pattern).
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request message.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <param name="timeout">The optional timeout for the request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the response message.</returns>
    Task<TResponse> SendAsync<TRequest, TResponse>(
        TRequest request,
        string? correlationId = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class;

    /// <summary>
    /// Subscribes a handler to an event type.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="handler">The handler to subscribe.</param>
    /// <returns>An IDisposable to manage the subscription.</returns>
    IDisposable Subscribe<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : class;

    /// <summary>
    /// Subscribes a handler delegate to an event type.
    /// </summary>
    IDisposable Subscribe<TEvent>(
        Func<TEvent, CancellationToken, Task> handler,
        string? handlerName = null,
        int priority = 0)
        where TEvent : class;

    /// <summary>
    /// Subscribes a synchronous handler delegate to an event type.
    /// </summary>
    IDisposable SubscribeSync<TEvent>(
        Action<TEvent> handler,
        string? handlerName = null,
        int priority = 0)
        where TEvent : class;

    /// <summary>
    /// Registers a request handler for request/reply pattern.
    /// </summary>
    IDisposable SubscribeRequest<TRequest, TResponse>(
        IRequestHandler<TRequest, TResponse> handler)
        where TRequest : class
        where TResponse : class;

    /// <summary>
    /// Unsubscribes a handler from all event types.
    /// </summary>
    Task UnsubscribeAsync(string handlerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all subscriptions for a specific event type.
    /// </summary>
    Task<IEnumerable<string>> GetSubscriptionsAsync(string eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all subscriptions.
    /// </summary>
    Task ClearSubscriptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current event bus options.
    /// </summary>
    Configuration.EventBusOptions GetOptions();

    /// <summary>
    /// Processes a raw distributed event payload, attempting deserialization and then publishing.
    /// If deserialization fails, the raw event is sent to the dead letter queue.
    /// </summary>
    Task<PublishResult> ProcessRawDistributedEventAsync(
        string eventType,
        string rawPayload,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}
