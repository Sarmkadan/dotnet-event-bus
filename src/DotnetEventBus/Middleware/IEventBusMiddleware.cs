#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using DotnetEventBus.Models;

namespace DotnetEventBus.Middleware;

/// <summary>
/// Represents the context for an event as it flows through the middleware pipeline.
/// </summary>
public sealed class EventContext
{
    /// <summary>
    /// The event object being processed.
    /// </summary>
    public object Event { get; }

    /// <summary>
    /// The type of the event being processed.
    /// </summary>
    public Type EventType { get; }

    /// <summary>
    /// The correlation ID associated with the event.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// The original EventMessage before handler invocation.
    /// </summary>
    public EventMessage EventMessage { get; }

    /// <summary>
    /// The CancellationToken for the current operation.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventContext"/> class.
    /// </summary>
    public EventContext(
        object @event,
        Type eventType,
        string? correlationId,
        EventMessage eventMessage,
        CancellationToken cancellationToken)
    {
        Event = @event ?? throw new ArgumentNullException(nameof(@event));
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        CorrelationId = correlationId;
        EventMessage = eventMessage ?? throw new ArgumentNullException(nameof(eventMessage));
        CancellationToken = cancellationToken;
    }
}

/// <summary>
/// Represents a middleware in the event bus pipeline.
/// </summary>
public interface IEventBusMiddleware
{
    /// <summary>
    /// Invokes the middleware with the given event context and passes control to the next middleware in the pipeline.
    /// </summary>
    /// <param name="context">The event context.</param>
    /// <param name="next">The next middleware delegate in the pipeline.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task InvokeAsync(EventContext context, EventMiddlewareDelegate next);
}

/// <summary>
/// Represents the delegate for the next middleware in the pipeline.
/// </summary>
public delegate Task EventMiddlewareDelegate(EventContext context);
