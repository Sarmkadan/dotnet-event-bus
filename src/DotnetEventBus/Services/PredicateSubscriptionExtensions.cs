#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using DotnetEventBus.Advanced;
using DotnetEventBus.Handlers;

namespace DotnetEventBus.Services;

/// <summary>
/// Extension methods that add predicate-based subscription capabilities to <see cref="IEventBus"/>.
/// Predicate subscriptions allow handlers to declaratively specify which events they care about,
/// reducing unnecessary handler invocations without requiring manual guard clauses in handler logic.
/// </summary>
public static class PredicateSubscriptionExtensions
{
    /// <summary>
    /// Subscribes a delegate handler that is only invoked when <paramref name="predicate"/>
    /// returns <see langword="true"/> for the published event.
    /// </summary>
    /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
    /// <param name="eventBus">The event bus to subscribe on.</param>
    /// <param name="handler">The async handler invoked for events that satisfy the predicate.</param>
    /// <param name="predicate">
    /// The filter condition evaluated for each published event.
    /// Only events for which this returns <see langword="true"/> reach the handler.
    /// </param>
    /// <param name="handlerName">Optional display name used in logging and diagnostics.</param>
    /// <param name="priority">
    /// Execution priority relative to other subscriptions for the same event type.
    /// Higher values run first. Defaults to 0.
    /// </param>
    /// <returns>An <see cref="IDisposable"/> that removes the subscription when disposed.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="eventBus"/>, <paramref name="handler"/>,
    /// or <paramref name="predicate"/> is <see langword="null"/>.
    /// </exception>
    public static IDisposable SubscribeWhere<TEvent>(
        this IEventBus eventBus,
        Func<TEvent, CancellationToken, Task> handler,
        Func<TEvent, bool> predicate,
        string? handlerName = null,
        int priority = 0)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(predicate);

        async Task FilteredDelegate(TEvent @event, CancellationToken ct)
        {
            if (predicate(@event))
                await handler(@event, ct);
        }

        return eventBus.Subscribe<TEvent>(FilteredDelegate, handlerName, priority);
    }

    /// <summary>
    /// Subscribes a typed <see cref="IEventHandler{TEvent}"/> that is only invoked when
    /// <paramref name="predicate"/> returns <see langword="true"/> for the published event.
    /// </summary>
    /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
    /// <param name="eventBus">The event bus to subscribe on.</param>
    /// <param name="handler">The typed handler to wrap with the predicate.</param>
    /// <param name="predicate">
    /// The filter condition evaluated for each published event.
    /// Only events for which this returns <see langword="true"/> reach the handler.
    /// </param>
    /// <param name="logger">Optional logger for diagnostic output when events are filtered.</param>
    /// <returns>An <see cref="IDisposable"/> that removes the subscription when disposed.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="eventBus"/>, <paramref name="handler"/>,
    /// or <paramref name="predicate"/> is <see langword="null"/>.
    /// </exception>
    public static IDisposable SubscribeWhere<TEvent>(
        this IEventBus eventBus,
        IEventHandler<TEvent> handler,
        Func<TEvent, bool> predicate,
        ILogger? logger = null)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(predicate);

        return eventBus.Subscribe<TEvent>(new PredicateFilteredHandler<TEvent>(handler, predicate, logger));
    }

    /// <summary>
    /// Subscribes a delegate handler using a composable <see cref="EventFilter{TEvent}"/>
    /// for multi-condition filtering. All conditions configured on the filter must be satisfied
    /// (AND semantics) before the handler is invoked.
    /// </summary>
    /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
    /// <param name="eventBus">The event bus to subscribe on.</param>
    /// <param name="handler">The async handler invoked for events that pass the filter.</param>
    /// <param name="configureFilter">
    /// An action that receives an <see cref="EventFilter{TEvent}"/> and adds one or more
    /// filter conditions using its fluent API.
    /// </param>
    /// <param name="handlerName">Optional display name used in logging and diagnostics.</param>
    /// <param name="priority">
    /// Execution priority relative to other subscriptions for the same event type.
    /// Higher values run first. Defaults to 0.
    /// </param>
    /// <returns>An <see cref="IDisposable"/> that removes the subscription when disposed.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="eventBus"/>, <paramref name="handler"/>,
    /// or <paramref name="configureFilter"/> is <see langword="null"/>.
    /// </exception>
    public static IDisposable SubscribeWithFilter<TEvent>(
        this IEventBus eventBus,
        Func<TEvent, CancellationToken, Task> handler,
        Action<EventFilter<TEvent>> configureFilter,
        string? handlerName = null,
        int priority = 0)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(configureFilter);

        var filter = new EventFilter<TEvent>();
        configureFilter(filter);

        async Task FilteredDelegate(TEvent @event, CancellationToken ct)
        {
            if (filter.Matches(@event))
                await handler(@event, ct);
        }

        return eventBus.Subscribe<TEvent>(FilteredDelegate, handlerName, priority);
    }

    /// <summary>
    /// Creates a fluent <see cref="PredicateSubscriptionBuilder{TEvent}"/> for constructing
    /// predicate-filtered subscriptions with fine-grained control over conditions, priority,
    /// handler naming, and logging.
    /// </summary>
    /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
    /// <param name="eventBus">The event bus to register the subscription on.</param>
    /// <returns>
    /// A new <see cref="PredicateSubscriptionBuilder{TEvent}"/> instance. Call
    /// <see cref="PredicateSubscriptionBuilder{TEvent}.Register"/> to finalise the subscription.
    /// </returns>
    /// <example>
    /// <code>
    /// eventBus.CreatePredicateSubscription&lt;OrderCreatedEvent&gt;()
    ///     .Where(e => e.TotalAmount &gt; 100)
    ///     .WhereNot(e => e.IsCancelled)
    ///     .WithPriority(10)
    ///     .WithHandlerName("HighValueOrderHandler")
    ///     .WithHandler(HandleHighValueOrderAsync)
    ///     .Register();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="eventBus"/> is <see langword="null"/>.
    /// </exception>
    public static PredicateSubscriptionBuilder<TEvent> CreatePredicateSubscription<TEvent>(
        this IEventBus eventBus)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        return new PredicateSubscriptionBuilder<TEvent>(eventBus);
    }
}
