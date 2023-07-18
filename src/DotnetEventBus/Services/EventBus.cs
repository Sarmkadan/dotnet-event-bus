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

    public EventBus(
        IEventMessageRepository messageRepository,
        ISubscriptionRepository subscriptionRepository,
        IDeadLetterRepository deadLetterRepository,
        IDeadLetterService deadLetterService,
        IEventFormatter eventFormatter,
        IServiceProvider serviceProvider, // Add IServiceProvider
        EventBusOptions? options = null,
        ILogger<EventBus>? logger = null)
    {
        _options = options ?? new EventBusOptions();
        _options.Validate();
        _logger = logger;
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        _deadLetterRepository = deadLetterRepository ?? throw new ArgumentNullException(nameof(deadLetterRepository));
        _deadLetterService = deadLetterService ?? throw new ArgumentNullException(nameof(deadLetterService));
        _eventFormatter = eventFormatter ?? throw new ArgumentNullException(nameof(eventFormatter));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider)); // Assign serviceProvider
        _concurrencyLimiter = new SemaphoreSlim(_options.MaxConcurrentHandlers);
    }

    public async Task<PublishResult> PublishAsync<TEvent>(
        TEvent @event,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
        where TEvent : class
    {
        if (@event is null)
            throw new ArgumentNullException(nameof(@event));

        var eventType = typeof(TEvent).FullName ?? typeof(TEvent).Name;
        return await PublishAsync(@event, typeof(TEvent), correlationId, cancellationToken);
    }

    public async Task<PublishResult> PublishAsync(
        object @event,
        Type eventType,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        if (@event is null)
            throw new ArgumentNullException(nameof(@event));
        if (eventType is null)
            throw new ArgumentNullException(nameof(eventType));

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
                _logger?.LogWarning("No handlers registered for event type: {EventType}", eventTypeName);
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

    public async Task<TResponse> SendAsync<TRequest, TResponse>(
        TRequest request,
        string? correlationId = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

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

    public IDisposable Subscribe<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : class
    {
        if (handler is null)
            throw new ArgumentNullException(nameof(handler));

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

    public IDisposable Subscribe<TEvent>(
        Func<TEvent, CancellationToken, Task> handler,
        string? handlerName = null,
        int priority = 0)
        where TEvent : class
    {
        if (handler is null)
            throw new ArgumentNullException(nameof(handler));

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

    public IDisposable SubscribeSync<TEvent>(
        Action<TEvent> handler,
        string? handlerName = null,
        int priority = 0)
        where TEvent : class
    {
        return Subscribe<TEvent>(
            (e, ct) =>
            {
                handler(e);
                return Task.CompletedTask;
            },
            handlerName,
            priority);
    }

    public IDisposable SubscribeRequest<TRequest, TResponse>(
        IRequestHandler<TRequest, TResponse> handler)
        where TRequest : class
        where TResponse : class
    {
        throw new NotImplementedException("Request handlers require distributed transport configuration");
    }

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

    public async Task ClearSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        lock (_subscriptionLock)
        {
            _subscriptions.Clear();
        }

        _logger?.LogWarning("All subscriptions cleared");
    }

    public EventBusOptions GetOptions() => _options.Clone();

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
