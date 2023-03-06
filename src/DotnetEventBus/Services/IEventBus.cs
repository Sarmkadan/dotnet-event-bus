#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetEventBus.Models;
using DotnetEventBus.Handlers;

namespace DotnetEventBus.Services;

/// <summary>
/// Core interface for the in-process event bus providing publish/subscribe, request/reply,
/// and dead-letter handling capabilities. Supports both strongly-typed and dynamic event publishing.
/// </summary>
/// <remarks>
/// <para>
/// The event bus supports three communication patterns:
/// <list type="bullet">
///   <item><b>Publish/Subscribe</b> - fire-and-forget broadcasting to multiple handlers via <see cref="PublishAsync{TEvent}"/></item>
///   <item><b>Request/Reply</b> - synchronous request with typed response via <see cref="SendAsync{TRequest,TResponse}"/></item>
///   <item><b>Dead Letter</b> - failed events are captured for later inspection and retry</item>
/// </list>
/// </para>
/// <para>
/// Handlers are registered via <see cref="Subscribe{TEvent}(IEventHandler{TEvent})"/> and can be
/// unregistered by disposing the returned <see cref="IDisposable"/>. Handlers support priority ordering -
/// lower priority values execute first.
/// </para>
/// <para>
/// Configure via <see cref="EventBusBuilder"/> at startup. Use the <see cref="GetOptions"/> method
/// to inspect the current configuration at runtime.
/// </para>
/// </remarks>
public interface IEventBus
{
    /// <summary>
    /// Publishes an event to all registered handlers for <typeparamref name="TEvent"/>.
    /// Returns a <see cref="PublishResult"/> indicating how many handlers processed the event.
    /// </summary>
    Task<PublishResult> PublishAsync<TEvent>(
        TEvent @event,
        string? correlationId = null,
        CancellationToken cancellationToken = default) where TEvent : class;

    /// <summary>
    /// Publishes an event as an object (for dynamic publishing).
    /// </summary>
    Task<PublishResult> PublishAsync(
        object @event,
        Type eventType,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a message and waits for a response (request/reply pattern).
    /// </summary>
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
}
