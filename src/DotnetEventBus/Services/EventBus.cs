#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Reflection;
using System.Text.Json;
using DotnetEventBus.Configuration;
using DotnetEventBus.Exceptions;
using DotnetEventBus.Handlers;
using DotnetEventBus.Models;
using DotnetEventBus.Repositories;
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
    private readonly SemaphoreSlim _concurrencyLimiter;
    private readonly Dictionary<string, List<Subscription>> _subscriptions = new();
    private readonly Dictionary<string, TaskCompletionSource<object?>> _pendingRequests = new();
    private readonly object _subscriptionLock = new();

    public EventBus(
        EventBusOptions? options = null,
        ILogger<EventBus>? logger = null)
    {
        _options = options ?? new EventBusOptions();
        _options.Validate();
        _logger = logger;
        _messageRepository = new InMemoryEventMessageRepository();
        _subscriptionRepository = new InMemorySubscriptionRepository();
        _deadLetterRepository = new InMemoryDeadLetterRepository();
        _concurrencyLimiter = new SemaphoreSlim(_options.MaxConcurrentHandlers);
    }

    public EventBus(
        IEventMessageRepository messageRepository,
        ISubscriptionRepository subscriptionRepository,
        IDeadLetterRepository deadLetterRepository,
        EventBusOptions? options = null,
        ILogger<EventBus>? logger = null)
    {
        _options = options ?? new EventBusOptions();
        _options.Validate();
        _logger = logger;
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        _deadLetterRepository = deadLetterRepository ?? throw new ArgumentNullException(nameof(deadLetterRepository));
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

            List<Subscription> subscriptions;
            lock (_subscriptionLock)
            {
                subscriptions = _subscriptions.TryGetValue(eventTypeName, out var subs)
                    ? subs.Where(s => s.IsActive).OrderByDescending(s => s.Priority).ToList()
                    : new List<Subscription>();
            }

            if (subscriptions.Count == 0)
            {
                _logger?.LogWarning("No handlers registered for event type: {EventType}", eventTypeName);
            }

            var result = new PublishResult(message.MessageId);

            if (_options.AllowParallelHandling)
            {
                await InvokeHandlersInParallel(subscriptions, @event, message, result, cancellationToken);
            }
            else
            {
                await InvokeHandlersSequentially(subscriptions, @event, message, result, cancellationToken);
            }

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
