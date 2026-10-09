#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Reflection;
using System.Text.Json;
using DotnetEventBus.Configuration;
using DotnetEventBus.Exceptions;
using DotnetEventBus.Formatters;
using DotnetEventBus.Handlers;
using DotnetEventBus.Middleware;
using DotnetEventBus.Models;
using DotnetEventBus.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Services;

/// <summary>
/// In-process event bus implementation supporting pub/sub and request/reply patterns.
/// </summary>
public sealed class EventBus : IEventBus
{
    private readonly EventBusOptions _options;
    private readonly ILogger<EventBus>? _logger;
    private readonly IEventMessageRepository _messageRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IDeadLetterRepository _deadLetterRepository;
    private readonly IDeadLetterService _deadLetterService;
    private readonly IEventFormatter _eventFormatter;
    private readonly IServiceProvider _serviceProvider;
    private readonly SemaphoreSlim _concurrencyLimiter;
    private readonly Dictionary<string, List<Subscription>> _subscriptions = new();
    private readonly Dictionary<string, TaskCompletionSource<object?>> _pendingRequests = new();
    private readonly object _subscriptionLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBus"/> class.
    /// </summary>
    /// <param name="options">Bus configuration. A default <see cref="EventBusOptions"/> is used when <c>null</c>. The options are validated on construction.</param>
    /// <param name="logger">Optional logger. When <c>null</c>, no logging is performed.</param>
    /// <param name="deadLetterService">Optional dead letter service. When <c>null</c>, a <see cref="DeadLetterService"/> bound to this instance is created.</param>
    /// <param name="eventFormatter">Optional payload formatter. Defaults to <see cref="Formatters.JsonEventFormatter"/>.</param>
    /// <param name="serviceProvider">Service provider used to resolve middleware types. Required; <c>null</c> throws <see cref="ArgumentNullException"/>.</param>
    /// <param name="messageRepository">Optional message repository. Defaults to a private in-memory repository.</param>
    /// <param name="subscriptionRepository">Optional subscription repository. Defaults to a private in-memory repository.</param>
    /// <param name="deadLetterRepository">Optional dead letter repository. Defaults to a private in-memory repository.</param>
    /// <remarks>
    /// When resolved through DI, pass the same repository singletons that <see cref="IDeadLetterService"/> uses.
    /// Otherwise dead letters written by this instance are not visible to the rest of the application.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider"/> is <c>null</c>.</exception>
    public EventBus(
        EventBusOptions? options = null,
        ILogger<EventBus>? logger = null,
        IDeadLetterService? deadLetterService = null,
        IEventFormatter? eventFormatter = null,
        IServiceProvider? serviceProvider = null,
        IEventMessageRepository? messageRepository = null,
        ISubscriptionRepository? subscriptionRepository = null,
        IDeadLetterRepository? deadLetterRepository = null)
    {
        _options = options ?? new EventBusOptions();
        _options.Validate();
        _logger = logger;
        // Falls back to private, per-instance repositories only when none are supplied.
        // When wired through DI, the caller must pass the same repository singletons that
        // IDeadLetterService/ISubscriptionManager use - otherwise dead letters written here
        // land in a repository nobody else can see them in.
        _messageRepository = messageRepository ?? new InMemoryEventMessageRepository();
        _subscriptionRepository = subscriptionRepository ?? new InMemorySubscriptionRepository();
        _deadLetterRepository = deadLetterRepository ?? new InMemoryDeadLetterRepository();
        _deadLetterService = deadLetterService ?? new DeadLetterService(_deadLetterRepository, this);
        _eventFormatter = eventFormatter ?? new Formatters.JsonEventFormatter();
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _concurrencyLimiter = new SemaphoreSlim(_options.MaxConcurrentHandlers);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBus"/> class with all dependencies supplied explicitly.
    /// </summary>
    /// <param name="messageRepository">Repository for published event messages. Must not be <c>null</c>.</param>
    /// <param name="subscriptionRepository">Repository for subscriptions. Must not be <c>null</c>.</param>
    /// <param name="deadLetterRepository">Repository for dead letter entries. Must not be <c>null</c>.</param>
    /// <param name="deadLetterService">Dead letter service. Must not be <c>null</c>.</param>
    /// <param name="eventFormatter">Payload formatter. Must not be <c>null</c>.</param>
    /// <param name="serviceProvider">Service provider used to resolve middleware. Must not be <c>null</c>.</param>
    /// <param name="options">Bus configuration. A default <see cref="EventBusOptions"/> is used when <c>null</c>.</param>
    /// <param name="logger">Optional logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is <c>null</c>.</exception>
    public EventBus(
        IEventMessageRepository messageRepository,
        ISubscriptionRepository subscriptionRepository,
        IDeadLetterRepository deadLetterRepository,
        IDeadLetterService deadLetterService,
        IEventFormatter eventFormatter,
        IServiceProvider serviceProvider, // Add IServiceProvider
        EventBusOptions? options = null,
        ILogger<EventBus>? logger = null)
        : this(
            options,
            logger,
            deadLetterService ?? throw new ArgumentNullException(nameof(deadLetterService)),
            eventFormatter ?? throw new ArgumentNullException(nameof(eventFormatter)),
            serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider)),
            messageRepository ?? throw new ArgumentNullException(nameof(messageRepository)),
            subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository)),
            deadLetterRepository ?? throw new ArgumentNullException(nameof(deadLetterRepository)))
    {
    }

    /// <summary>
    /// Publishes a strongly typed event to all matching subscribers.
    /// </summary>
    /// <typeparam name="TEvent">The event type. Handlers registered for base types and implemented interfaces are also invoked.</typeparam>
    /// <param name="event">The event to publish. Serialized to JSON for the message payload.</param>
    /// <param name="correlationId">Correlation identifier. A new GUID is generated when <c>null</c>.</param>
    /// <param name="cancellationToken">Token that cancels publishing and handler invocation.</param>
    /// <returns>A <see cref="PublishResult"/> describing handler outcomes and elapsed time.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="event"/> is <c>null</c>.</exception>
    /// <exception cref="NoHandlersRegisteredException">Thrown when no handlers match and <c>ThrowOnNoHandlers</c> is enabled.</exception>
    public async Task<PublishResult> PublishAsync<TEvent>(
        TEvent @event,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventType = typeof(TEvent).FullName ?? typeof(TEvent).Name;
        return await PublishAsync(@event, typeof(TEvent), correlationId, cancellationToken);
    }

    /// <summary>
    /// Publishes an event using a runtime type. Handlers are resolved from <paramref name="eventType"/>,
    /// its base types and implemented interfaces, then invoked through the configured middleware pipeline.
    /// </summary>
    /// <param name="event">The event instance to publish.</param>
    /// <param name="eventType">The runtime type used for subscription lookup and payload naming.</param>
    /// <param name="correlationId">Correlation identifier. A new GUID is generated when <c>null</c>.</param>
    /// <param name="cancellationToken">Token that cancels publishing and handler invocation.</param>
    /// <returns>A <see cref="PublishResult"/> describing handler outcomes and elapsed time.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="event"/> or <paramref name="eventType"/> is <c>null</c>.</exception>
    /// <exception cref="NoHandlersRegisteredException">Thrown when no handlers match and <c>ThrowOnNoHandlers</c> is enabled.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a configured middleware type cannot be resolved.</exception>
    /// <remarks>
    /// Unless <c>ThrowOnHandlerFailure</c> is enabled, exceptions raised during publishing are logged
    /// and returned as a failed <see cref="PublishResult"/> instead of being thrown.
    /// </remarks>
    public async Task<PublishResult> PublishAsync(
        object @event,
        Type eventType,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(eventType);

        var startTime = DateTime.UtcNow;
        var eventTypeName = eventType.FullName ?? eventType.Name;
        var payload = JsonSerializer.Serialize(@event);

        var message = new EventMessage(eventTypeName, payload)
        {
            CorrelationId = correlationId ?? Guid.NewGuid().ToString(),
            Scope = MessageScope.InProcess
        };

        message.Validate();

        try
        {
            await _messageRepository.AddAsync(message, cancellationToken);

            List<Subscription> applicableSubscriptions = new List<Subscription>();
            HashSet<string> invokedHandlerNames = new HashSet<string>();

            Type? currentEventType = eventType;
            while (currentEventType != null && currentEventType != typeof(object))
            {
                lock (_subscriptionLock)
                {
                    if (_subscriptions.TryGetValue(currentEventType.FullName ?? currentEventType.Name, out var subs))
                    {
                        foreach (var sub in subs.Where(s => s.IsActive).OrderByDescending(s => s.Priority))
                        {
                            // Add if not already added to avoid duplicate invocation for the same handler instance
                            if (!invokedHandlerNames.Contains(sub.HandlerName))
                            {
                                applicableSubscriptions.Add(sub);
                                invokedHandlerNames.Add(sub.HandlerName);
                            }
                        }
                    }
                }
                currentEventType = currentEventType.BaseType;
            }

            // Also consider interfaces
            foreach (var iface in eventType.GetInterfaces())
            {
                lock (_subscriptionLock)
                {
                    if (_subscriptions.TryGetValue(iface.FullName ?? iface.Name, out var subs))
                    {
                        foreach (var sub in subs.Where(s => s.IsActive).OrderByDescending(s => s.Priority))
                        {
                            if (!invokedHandlerNames.Contains(sub.HandlerName))
                            {
                                applicableSubscriptions.Add(sub);
                                invokedHandlerNames.Add(sub.HandlerName);
                            }
                        }
                    }
                }
            }

            if (applicableSubscriptions.Count == 0)
            {
                if (_options.ThrowOnNoHandlers)
                    throw new NoHandlersRegisteredException(eventTypeName);

                _logger?.LogWarning("No handlers registered for event type: {EventType}", eventTypeName);

                if (_options.EnableDeadLetterQueue && _options.DeadLetterOnNoHandlers)
                {
                    await _deadLetterService.AddDeadLetterEntryAsync(
                        eventTypeName,
                        payload,
                        new NoHandlersRegisteredException(eventTypeName),
                        correlationId,
                        "NoHandlersRegistered",
                        cancellationToken);
                }
            }

            var result = new PublishResult(message.MessageId);

            // Construct the EventContext for the middleware pipeline
            var eventContext = new Middleware.EventMiddlewareContext(@event, eventType, correlationId, message, cancellationToken);

            // Create the terminal delegate (actual handler invocation logic)
            EventMiddlewareDelegate terminalDelegate = async ctx =>
            {
                if (_options.AllowParallelHandling)
                {
                    await InvokeHandlersInParallel(applicableSubscriptions, ctx.Event, ctx.EventMessage, result, ctx.CancellationToken);
                }
                else
                {
                    await InvokeHandlersSequentially(applicableSubscriptions, ctx.Event, ctx.EventMessage, result, ctx.CancellationToken);
                }
            };

            // Build the middleware pipeline
            EventMiddlewareDelegate pipeline = terminalDelegate;
            foreach (var middlewareType in Enumerable.Reverse(_options.MiddlewareTypes)) // Reverse to build from inside out
            {
                var middlewareInstance = _serviceProvider.GetRequiredService(middlewareType) as Middleware.IEventBusMiddleware;
                if (middlewareInstance is null)
                {
                    _logger?.LogError("Middleware type {MiddlewareType} could not be resolved or is not an IEventBusMiddleware.", middlewareType.FullName);
                    throw new InvalidOperationException($"Middleware type {middlewareType.FullName} is not registered or does not implement IEventBusMiddleware.");
                }

                var currentNext = pipeline;
                pipeline = async ctx => await middlewareInstance.InvokeAsync(ctx, currentNext);
            }

            // Execute the pipeline
            await pipeline(eventContext);

            result.ElapsedTime = DateTime.UtcNow.Subtract(startTime);
            result.Success = result.FailedHandlers == 0;

            _logger?.LogInformation(
                "Published event {EventType} with {HandlersInvoked} handlers, {FailedHandlers} failed",
                eventTypeName,
                result.HandlersInvoked,
                result.FailedHandlers);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error publishing event {EventType}", eventTypeName);
            if (_options.ThrowOnHandlerFailure)
                throw;

            return PublishResult.CreateFailed(message.MessageId, ex);
        }
    }

    /// <summary>
    /// Sends a request and awaits a response. Intended for request/reply over a distributed transport.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="correlationId">Correlation identifier for the request.</param>
    /// <param name="timeout">Overrides the request timeout. Takes precedence over <c>RequestTimeoutAttribute</c> on <typeparamref name="TRequest"/> and the configured default.</param>
    /// <param name="cancellationToken">Token that cancels the request.</param>
    /// <returns>The response produced by the remote handler.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <c>null</c>.</exception>
    /// <exception cref="ConfigurationException">Thrown when no distributed transport is configured.</exception>
    /// <exception cref="NotImplementedException">Thrown always in the current version; the distributed transport is not implemented.</exception>
    public async Task<TResponse> SendAsync<TRequest, TResponse>(
        TRequest request,
        string? correlationId = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = typeof(TRequest);
        TimeSpan effectiveTimeout = _options.RequestTimeout;

        // 1. Check method parameter timeout
        if (timeout.HasValue)
        {
            effectiveTimeout = timeout.Value;
        }
        else
        {
            // 2. Check RequestTimeoutAttribute on TRequest
            var requestTimeoutAttribute = requestType.GetCustomAttribute<RequestTimeoutAttribute>();
            if (requestTimeoutAttribute is not null)
            {
                effectiveTimeout = TimeSpan.FromMilliseconds(requestTimeoutAttribute.TimeoutMilliseconds);
            }
            // 3. Fallback to _options.RequestTimeout (already set as initial effectiveTimeout)
        }

        if (_options.IsDistributed == false || _options.DistributedTransportType == null)
        {
            throw new ConfigurationException(
                "Request/reply pattern requires distributed transport configuration. " +
                $"Configured timeout for this request: {effectiveTimeout.TotalSeconds} seconds.");
        }

        // The actual request/reply implementation would go here, utilizing effectiveTimeout
        // For now, we keep the NotImplementedException as the distributed transport itself is not implemented.
        throw new NotImplementedException("Request/reply pattern requires distributed transport configuration");
    }

    /// <summary>
    /// Subscribes an <see cref="IEventHandler{TEvent}"/> instance to events of type <typeparamref name="TEvent"/>.
    /// </summary>
    /// <typeparam name="TEvent">The event type to handle.</typeparam>
    /// <param name="handler">The handler to invoke for each matching event.</param>
    /// <returns>A disposable that removes the subscription when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is <c>null</c>.</exception>
    public IDisposable Subscribe<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent).FullName ?? typeof(TEvent).Name;
        var subscription = new Subscription(
            eventType,
            new Func<TEvent, CancellationToken, Task>((e, ct) => handler.Handle(e, ct)),
            handler.GetHandlerName());

        lock (_subscriptionLock)
        {
            if (!_subscriptions.ContainsKey(eventType))
                _subscriptions[eventType] = new List<Subscription>();

            _subscriptions[eventType].Add(subscription);
        }

        _logger?.LogInformation(
            "Handler {HandlerName} subscribed to event {EventType}",
            subscription.HandlerName,
            eventType);

        return new SubscriptionDisposable(this, subscription.Id);
    }

    /// <summary>
    /// Subscribes an asynchronous delegate to events of type <typeparamref name="TEvent"/>.
    /// </summary>
    /// <typeparam name="TEvent">The event type to handle.</typeparam>
    /// <param name="handler">The delegate to invoke for each matching event.</param>
    /// <param name="handlerName">Name used in logs, results and dead letter entries. A generated name is used when <c>null</c>.</param>
    /// <param name="priority">Dispatch priority. Higher values are invoked first.</param>
    /// <returns>A disposable that removes the subscription when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is <c>null</c>.</exception>
    public IDisposable Subscribe<TEvent>(
        Func<TEvent, CancellationToken, Task> handler,
        string? handlerName = null,
        int priority = 0)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent).FullName ?? typeof(TEvent).Name;
        var subscription = new Subscription(
            eventType,
            handler,
            handlerName ?? $"{eventType}_Handler_{Guid.NewGuid().ToString().Substring(0, 8)}",
            priority);

        lock (_subscriptionLock)
        {
            if (!_subscriptions.ContainsKey(eventType))
                _subscriptions[eventType] = new List<Subscription>();

            _subscriptions[eventType].Add(subscription);
        }

        return new SubscriptionDisposable(this, subscription.Id);
    }

    /// <summary>
    /// Subscribes a synchronous delegate to events of type <typeparamref name="TEvent"/>.
    /// </summary>
    /// <typeparam name="TEvent">The event type to handle.</typeparam>
    /// <param name="handler">The delegate to invoke for each matching event.</param>
    /// <param name="handlerName">Name used in logs, results and dead letter entries. A generated name is used when <c>null</c>.</param>
    /// <param name="priority">Dispatch priority. Higher values are invoked first.</param>
    /// <returns>A disposable that removes the subscription when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is <c>null</c>.</exception>
    public IDisposable SubscribeSync<TEvent>(
        Action<TEvent> handler,
        string? handlerName = null,
        int priority = 0)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Subscribe<TEvent>(
            (e, ct) =>
            {
                handler(e);
                return Task.CompletedTask;
            },
            handlerName,
            priority);
    }

    /// <summary>
    /// Registers a request handler for request/reply messaging.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="handler">The request handler.</param>
    /// <returns>A disposable that removes the registration when disposed.</returns>
    /// <exception cref="NotImplementedException">Thrown always in the current version; requires a distributed transport that is not implemented.</exception>
    public IDisposable SubscribeRequest<TRequest, TResponse>(
        IRequestHandler<TRequest, TResponse> handler)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(handler);

        throw new NotImplementedException("Request handlers require distributed transport configuration");
    }

    /// <summary>
    /// Removes the subscription with the given identifier from all event types.
    /// </summary>
    /// <param name="handlerId">The subscription identifier. Unknown identifiers are ignored.</param>
    /// <param name="cancellationToken">Token for cancelling the operation.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="handlerId"/> is null, empty or whitespace.</exception>
    public async Task UnsubscribeAsync(string handlerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(handlerId))
            throw new ArgumentException("Handler ID cannot be empty", nameof(handlerId));

        lock (_subscriptionLock)
        {
            var allSubscriptions = _subscriptions.Values.SelectMany(s => s).ToList();
            var toRemove = allSubscriptions.Where(s => s.Id == handlerId).ToList();

            foreach (var sub in toRemove)
            {
                foreach (var list in _subscriptions.Values)
                {
                    list.RemoveAll(s => s.Id == handlerId);
                }
            }
        }

        _logger?.LogInformation("Handler {HandlerId} unsubscribed", handlerId);
    }

    /// <summary>
    /// Returns the handler names currently subscribed to the given event type.
    /// </summary>
    /// <param name="eventType">The full event type name.</param>
    /// <param name="cancellationToken">Token for cancelling the operation.</param>
    /// <returns>Handler names for the event type. Empty when there are none.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="eventType"/> is null, empty or whitespace.</exception>
    public async Task<IEnumerable<string>> GetSubscriptionsAsync(string eventType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty", nameof(eventType));

        lock (_subscriptionLock)
        {
            return _subscriptions.TryGetValue(eventType, out var subs)
                ? subs.Select(s => s.HandlerName).ToList()
                : new List<string>();
        }
    }

    /// <summary>
    /// Removes all subscriptions for all event types.
    /// </summary>
    /// <param name="cancellationToken">Token for cancelling the operation.</param>
    public async Task ClearSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        lock (_subscriptionLock)
        {
            _subscriptions.Clear();
        }

        _logger?.LogWarning("All subscriptions cleared");
    }

    /// <summary>
    /// Returns a copy of the bus configuration. Changes to the returned instance do not affect this bus.
    /// </summary>
    /// <returns>A cloned <see cref="EventBusOptions"/>.</returns>
    public EventBusOptions GetOptions() => _options.Clone();

    /// <summary>
    /// Invokes all subscriptions concurrently, subject to the concurrency limiter.
    /// </summary>
    /// <param name="subscriptions">Subscriptions to invoke.</param>
    /// <param name="event">The event instance.</param>
    /// <param name="message">The message associated with the event.</param>
    /// <param name="result">Result object updated with handler outcomes.</param>
    /// <param name="cancellationToken">Token that cancels handler invocation.</param>
    private async Task InvokeHandlersInParallel(
        List<Subscription> subscriptions,
        object @event,
        EventMessage message,
        PublishResult result,
        CancellationToken cancellationToken)
    {
        var tasks = subscriptions.Select(sub =>
            InvokeHandlerWithRetry(sub, @event, message, result, cancellationToken));

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Invokes subscriptions one at a time in the order given.
    /// </summary>
    /// <param name="subscriptions">Subscriptions to invoke.</param>
    /// <param name="event">The event instance.</param>
    /// <param name="message">The message associated with the event.</param>
    /// <param name="result">Result object updated with handler outcomes.</param>
    /// <param name="cancellationToken">Token that cancels handler invocation.</param>
    private async Task InvokeHandlersSequentially(
        List<Subscription> subscriptions,
        object @event,
        EventMessage message,
        PublishResult result,
        CancellationToken cancellationToken)
    {
        foreach (var subscription in subscriptions)
        {
            await InvokeHandlerWithRetry(subscription, @event, message, result, cancellationToken);
        }
    }

    /// <summary>
    /// Invokes a single subscription, retrying on failure up to <c>MaxRetryAttempts</c> times.
    /// Failures after the final attempt are recorded on <paramref name="result"/> and, when enabled,
    /// written to the dead letter repository. The exception is not rethrown.
    /// </summary>
    /// <param name="subscription">The subscription to invoke.</param>
    /// <param name="event">The event instance.</param>
    /// <param name="message">The message associated with the event.</param>
    /// <param name="result">Result object updated with handler outcomes.</param>
    /// <param name="cancellationToken">Token that cancels handler invocation and retry delays.</param>
    private async Task InvokeHandlerWithRetry(
        Subscription subscription,
        object @event,
        EventMessage message,
        PublishResult result,
        CancellationToken cancellationToken)
    {
        await _concurrencyLimiter.WaitAsync(cancellationToken);
        try
        {
            for (int attempt = 0; attempt <= _options.MaxRetryAttempts; attempt++)
            {
                try
                {
                    await InvokeHandler(subscription, @event, cancellationToken);
                    result.AddSuccessfulHandler(subscription.HandlerName);
                    return;
                }
                catch (Exception ex) when (attempt < _options.MaxRetryAttempts)
                {
                    var delay = _options.CalculateRetryDelay(attempt);
                    _logger?.LogWarning(
                        ex,
                        "Handler {HandlerName} failed (attempt {Attempt}/{MaxAttempts}), retrying after {DelayMs}ms",
                        subscription.HandlerName,
                        attempt + 1,
                        _options.MaxRetryAttempts,
                        delay.TotalMilliseconds);

                    await Task.Delay(delay, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(
                        ex,
                        "Handler {HandlerName} failed after {MaxAttempts} attempts",
                        subscription.HandlerName,
                        _options.MaxRetryAttempts);

                    result.AddFailedHandler(subscription.HandlerName, ex);

                    if (_options.EnableDeadLetterQueue && subscription.SendToDeadLetterOnFailure)
                    {
                        var deadLetterEntry = new DeadLetterEntry(
                            message,
                            subscription.HandlerName,
                            ex,
                            _options.MaxRetryAttempts);

                        await _deadLetterRepository.AddAsync(deadLetterEntry, cancellationToken);
                    }
                }
            }
        }
        finally
        {
            _concurrencyLimiter.Release();
        }
    }

    /// <summary>
    /// Invokes a single handler once, applying the subscription timeout (or the default handler timeout).
    /// </summary>
    /// <param name="subscription">The subscription to invoke.</param>
    /// <param name="event">The event instance.</param>
    /// <param name="cancellationToken">Token that cancels the invocation.</param>
    /// <exception cref="InvalidOperationException">Thrown when an async handler returns <c>null</c>.</exception>
    private async Task InvokeHandler(Subscription subscription, object @event, CancellationToken cancellationToken)
    {
        var method = subscription.Handler.Method;
        var timeoutCts = new CancellationTokenSource(subscription.Timeout ?? _options.DefaultHandlerTimeout);
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            if (subscription.IsAsync)
            {
                var task = (Task?)subscription.Handler.DynamicInvoke(@event, linkedCts.Token)
                    ?? throw new InvalidOperationException("Handler returned null task");
                await task;
            }
            else
            {
                subscription.Handler.DynamicInvoke(@event);
            }
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
        finally
        {
            timeoutCts.Dispose();
            linkedCts.Dispose();
        }
    }

    /// <summary>
    /// Deserializes a raw payload received from a distributed transport and publishes it locally.
    /// </summary>
    /// <param name="eventType">Assembly-qualified type name of the event, resolved with <c>Type.GetType</c>.</param>
    /// <param name="rawPayload">The serialized event payload.</param>
    /// <param name="correlationId">Correlation identifier for the event.</param>
    /// <param name="cancellationToken">Token that cancels publishing.</param>
    /// <returns>
    /// The <see cref="PublishResult"/> from publishing, or a failed result when the type is unknown,
    /// the payload cannot be deserialized, or deserialization yields <c>null</c>.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="eventType"/> or <paramref name="rawPayload"/> is null, empty or whitespace.</exception>
    /// <remarks>
    /// Unresolvable or undeserializable events are written to the dead letter service before a failed result is returned.
    /// </remarks>
    public async Task<PublishResult> ProcessRawDistributedEventAsync(
        string eventType,
        string rawPayload,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty", nameof(eventType));
        if (string.IsNullOrWhiteSpace(rawPayload))
            throw new ArgumentException("Raw payload cannot be empty", nameof(rawPayload));

        Type? eventActualType = Type.GetType(eventType);
        if (eventActualType is null)
        {
            _logger?.LogWarning("Unknown event type {EventType}. Sending to dead letter queue.", eventType);
            await _deadLetterService.AddDeadLetterEntryAsync(
                eventType,
                rawPayload,
                new InvalidOperationException($"Unknown event type: {eventType}"),
                correlationId,
                "ProcessRawDistributedEventAsync",
                cancellationToken);
            return PublishResult.CreateFailed(correlationId, new InvalidOperationException($"Unknown event type: {eventType}"));
        }

        try
        {
            object? deserializedEvent = _eventFormatter.Deserialize(rawPayload, eventActualType);

            if (deserializedEvent is null)
            {
                _logger?.LogError("Failed to deserialize event of type {EventType}. Sending to dead letter queue.", eventType);
                await _deadLetterService.AddDeadLetterEntryAsync(
                    eventType,
                    rawPayload,
                    new InvalidOperationException($"Deserialized event is null for type: {eventType}"),
                    correlationId,
                    "ProcessRawDistributedEventAsync",
                    cancellationToken);
                return PublishResult.CreateFailed(correlationId, new InvalidOperationException($"Deserialized event is null for type: {eventType}"));
            }

            return await PublishAsync(deserializedEvent, eventActualType, correlationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "Failed to deserialize distributed event of type {EventType}. Sending to dead letter queue.",
                eventType);

            await _deadLetterService.AddDeadLetterEntryAsync(
                eventType,
                rawPayload,
                ex,
                correlationId,
                "ProcessRawDistributedEventAsync",
                cancellationToken);

            return PublishResult.CreateFailed(correlationId, ex);
        }
    }
    /// <summary>
    /// Disposable handle returned from subscribe methods. Disposing it removes the subscription.
    /// </summary>
    private class SubscriptionDisposable : IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly string _subscriptionId;
        private bool _disposed;

        public SubscriptionDisposable(EventBus eventBus, string subscriptionId)
        {
            _eventBus = eventBus;
            _subscriptionId = subscriptionId;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _eventBus.UnsubscribeAsync(_subscriptionId).GetAwaiter().GetResult();
                _disposed = true;
            }
        }
    }
}
