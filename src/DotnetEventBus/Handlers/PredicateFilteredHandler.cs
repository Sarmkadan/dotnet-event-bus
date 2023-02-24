// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Handlers;

/// <summary>
/// Wraps an <see cref="IEventHandler{TEvent}"/> with a predicate that gates event processing.
/// Only invokes the inner handler when the predicate evaluates to <see langword="true"/>.
/// Events that do not satisfy the predicate are silently skipped without error.
/// </summary>
/// <typeparam name="TEvent">The event type this handler processes.</typeparam>
public sealed class PredicateFilteredHandler<TEvent> : IEventHandler<TEvent>
    where TEvent : class
{
    private readonly IEventHandler<TEvent> _inner;
    private readonly Func<TEvent, bool> _predicate;
    private readonly ILogger? _logger;
    private readonly string _handlerName;

    /// <summary>
    /// Initializes a new instance of <see cref="PredicateFilteredHandler{TEvent}"/>.
    /// </summary>
    /// <param name="inner">The underlying handler to invoke when the predicate passes.</param>
    /// <param name="predicate">
    /// The filter condition. When it returns <see langword="false"/> the event is silently skipped.
    /// </param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public PredicateFilteredHandler(
        IEventHandler<TEvent> inner,
        Func<TEvent, bool> predicate,
        ILogger? logger = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        _logger = logger;
        _handlerName = $"PredicateFiltered<{inner.GetHandlerName()}>";
    }

    /// <summary>
    /// Gets the event type processed by this handler.
    /// </summary>
    public Type GetEventType() => typeof(TEvent);

    /// <summary>
    /// Gets the display name for this handler, incorporating the inner handler's name.
    /// </summary>
    public string GetHandlerName() => _handlerName;

    /// <summary>
    /// Evaluates the predicate against the event and, when it passes, delegates to the inner handler.
    /// </summary>
    /// <param name="event">The published event.</param>
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    public async Task Handle(TEvent @event, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_predicate(@event))
        {
            _logger?.LogDebug(
                "Event {EventType} skipped by predicate subscription for handler {HandlerName}",
                typeof(TEvent).Name,
                _inner.GetHandlerName());
            return;
        }

        await _inner.Handle(@event, cancellationToken);
    }
}
