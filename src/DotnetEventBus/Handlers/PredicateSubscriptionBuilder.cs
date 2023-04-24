#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using DotnetEventBus.Advanced;
using DotnetEventBus.Services;

namespace DotnetEventBus.Handlers;

/// <summary>
/// Fluent builder for constructing predicate-filtered subscriptions on an <see cref="IEventBus"/>.
/// Predicates are composed with AND semantics — every condition must be satisfied before
/// the handler is invoked.
/// </summary>
/// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
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
public sealed class PredicateSubscriptionBuilder<TEvent>
    where TEvent : class
{
    private readonly IEventBus _eventBus;
    private readonly EventFilter<TEvent> _filter = new();
    private Func<TEvent, CancellationToken, Task>? _asyncHandler;
    private string? _handlerName;
    private int _priority;
    private ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PredicateSubscriptionBuilder{TEvent}"/>.
    /// Obtain an instance via <c>IEventBus.CreatePredicateSubscription&lt;TEvent&gt;()</c>.
    /// </summary>
    /// <param name="eventBus">The event bus to register the subscription on.</param>
    internal PredicateSubscriptionBuilder(IEventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    /// <summary>
    /// Adds a predicate that must evaluate to <see langword="true"/> for the event to be processed.
    /// Multiple calls are combined with AND logic.
    /// </summary>
    /// <param name="predicate">The condition to evaluate against each published event.</param>
    public PredicateSubscriptionBuilder<TEvent> Where(Func<TEvent, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _filter.Where(predicate);
        return this;
    }

    /// <summary>
    /// Adds a negated predicate. The event is processed only when this condition evaluates to
    /// <see langword="false"/>.
    /// </summary>
    /// <param name="predicate">The condition to negate.</param>
    public PredicateSubscriptionBuilder<TEvent> WhereNot(Func<TEvent, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _filter.Not(predicate);
        return this;
    }

    /// <summary>
    /// Adds a property equality condition. The event is processed only when the selected
    /// property equals <paramref name="expectedValue"/>.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property being compared.</typeparam>
    /// <param name="propertySelector">Selector that extracts the property from the event.</param>
    /// <param name="expectedValue">The required value of the property.</param>
    public PredicateSubscriptionBuilder<TEvent> WhereProperty<TProperty>(
        Func<TEvent, TProperty> propertySelector,
        TProperty expectedValue)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);
        _filter.WhereProperty(propertySelector, expectedValue);
        return this;
    }

    /// <summary>
    /// Adds a string contains condition. The event is processed only when the selected
    /// string property contains <paramref name="value"/> (case-insensitive).
    /// </summary>
    /// <param name="propertySelector">Selector that extracts the string property from the event.</param>
    /// <param name="value">The substring that must be present.</param>
    public PredicateSubscriptionBuilder<TEvent> WherePropertyContains(
        Func<TEvent, string?> propertySelector,
        string value)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);
        ArgumentNullException.ThrowIfNull(value);
        _filter.WherePropertyContains(propertySelector, value);
        return this;
    }

    /// <summary>
    /// Configures the async handler delegate to invoke when a matching event is received.
    /// </summary>
    /// <param name="handler">The async delegate that processes the event.</param>
    public PredicateSubscriptionBuilder<TEvent> WithHandler(Func<TEvent, CancellationToken, Task> handler)
    {
        _asyncHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        return this;
    }

    /// <summary>
    /// Sets a display name for the subscription used in logging and diagnostics.
    /// </summary>
    /// <param name="name">A descriptive name that identifies this subscription.</param>
    public PredicateSubscriptionBuilder<TEvent> WithHandlerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Handler name cannot be empty.", nameof(name));

        _handlerName = name;
        return this;
    }

    /// <summary>
    /// Sets the execution priority. Subscriptions with higher values run before those
    /// with lower values when multiple handlers are registered for the same event.
    /// </summary>
    /// <param name="priority">The priority value (default is 0).</param>
    public PredicateSubscriptionBuilder<TEvent> WithPriority(int priority)
    {
        _priority = priority;
        return this;
    }

    /// <summary>
    /// Attaches a logger used to emit diagnostic messages when events are filtered out.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public PredicateSubscriptionBuilder<TEvent> WithLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        return this;
    }

    /// <summary>
    /// Registers the predicate-filtered subscription on the event bus.
    /// </summary>
    /// <returns>
    /// An <see cref="IDisposable"/> that removes the subscription when disposed.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no handler has been configured via <see cref="WithHandler"/>.
    /// </exception>
    public IDisposable Register()
    {
        if (_asyncHandler is null)
            throw new InvalidOperationException(
                $"No handler configured. Call {nameof(WithHandler)} before calling {nameof(Register)}.");

        var capturedFilter = _filter;
        var capturedLogger = _logger;
        var capturedName = _handlerName;
        var capturedHandler = _asyncHandler;

        async Task FilteredDelegate(TEvent @event, CancellationToken ct)
        {
            if (!capturedFilter.Matches(@event))
            {
                capturedLogger?.LogDebug(
                    "Event {EventType} did not match predicate subscription {HandlerName}",
                    typeof(TEvent).Name,
                    capturedName ?? "unnamed");
                return;
            }

            await capturedHandler(@event, ct);
        }

        return _eventBus.Subscribe<TEvent>(FilteredDelegate, _handlerName, _priority);
    }
}
