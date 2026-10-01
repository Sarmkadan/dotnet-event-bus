#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotnetEventBus.Configuration;
using DotnetEventBus.Handlers;
using DotnetEventBus.Models;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Services;

/// <summary>
/// Lightweight in-memory event bus for testing and single-process scenarios.
/// Does not persist messages or support distributed transports.
/// </summary>
public sealed class InMemoryBus : IEventBus, IDisposable
{
    private readonly ConcurrentDictionary<string, List<Subscription>> _subscriptions = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<object?>> _pendingRequests = new();
    private readonly ILogger<InMemoryBus> _logger;
    private readonly EventBusOptions _options;
    private readonly SemaphoreSlim _publishLock = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryBus"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="options">Event bus configuration options.</param>
    public InMemoryBus(ILogger<InMemoryBus> logger, EventBusOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<PublishResult> PublishAsync<TEvent>(
        TEvent @event,
        string? correlationId = null,
        CancellationToken cancellationToken = default) where TEvent : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(@event);

        var eventType = typeof(TEvent).FullName ?? typeof(TEvent).Name;
        var handlersInvoked = 0;

        if (_subscriptions.TryGetValue(eventType, out var subs))
        {
            List<Subscription> snapshot;
            lock (subs) { snapshot = [.. subs]; }

            foreach (var sub in snapshot.OrderByDescending(s => s.Priority))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (sub.Handler is Func<TEvent, CancellationToken, Task> asyncHandler)
                    {
                        await asyncHandler(@event, cancellationToken).ConfigureAwait(false);
                        handlersInvoked++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Handler {HandlerId} failed for event {EventType}", sub.Id, eventType);
                }
            }
        }

        _logger.LogDebug("Published {EventType} to {Count} handlers", eventType, handlersInvoked);
        return PublishResult.CreateSuccess(correlationId ?? Guid.NewGuid().ToString(), handlersInvoked);
    }

    /// <inheritdoc />
    public Task<PublishResult> PublishAsync(
        object @event,
        Type eventType,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var method = typeof(InMemoryBus)
            .GetMethod(nameof(PublishAsync), [typeof(object), typeof(string), typeof(CancellationToken)])!;
        var generic = method.MakeGenericMethod(eventType);
        return (Task<PublishResult>)generic.Invoke(this, [@event, correlationId, cancellationToken])!;
    }

    /// <inheritdoc />
    public async Task<TResponse> SendAsync<TRequest, TResponse>(
        TRequest request,
        string? correlationId = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var requestType = typeof(TRequest).FullName ?? typeof(TRequest).Name;
        var tcs = new TaskCompletionSource<object?>();
        var requestId = correlationId ?? Guid.NewGuid().ToString();

        _pendingRequests[requestId] = tcs;
        try
        {
            await PublishAsync(request, requestId, cancellationToken).ConfigureAwait(false);
            var effectiveTimeout = timeout ?? _options.RequestTimeout;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(effectiveTimeout);
            var result = await tcs.Task.WaitAsync(cts.Token).ConfigureAwait(false);
            return (TResponse)result!;
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : class
    {
        return Subscribe<TEvent>(
            (e, ct) => handler.Handle(e, ct),
            handler.GetHandlerName());
    }

    /// <inheritdoc />
    public IDisposable Subscribe<TEvent>(
        Func<TEvent, CancellationToken, Task> handler,
        string? handlerName = null,
        int priority = 0) where TEvent : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var eventType = typeof(TEvent).FullName ?? typeof(TEvent).Name;
        var sub = new Subscription(
            eventType,
            handler,
            handlerName ?? handler.Method.Name,
            priority);

        var subs = _subscriptions.GetOrAdd(eventType, _ => new List<Subscription>());
        lock (subs) { subs.Add(sub); }

        _logger.LogDebug("Subscribed {HandlerName} to {EventType}", sub.HandlerName, eventType);
        return new EventSubscription(() => Unsubscribe(eventType, sub.Id));
    }

    /// <inheritdoc />
    public IDisposable SubscribeSync<TEvent>(
        Action<TEvent> handler,
        string? handlerName = null,
        int priority = 0) where TEvent : class
    {
        return Subscribe<TEvent>((e, _) => { handler(e); return Task.CompletedTask; }, handlerName, priority);
    }

    /// <inheritdoc />
    public IDisposable SubscribeRequest<TRequest, TResponse>(
        IRequestHandler<TRequest, TResponse> handler)
        where TRequest : class
        where TResponse : class
    {
        return Subscribe<TRequest>(async (request, ct) =>
        {
            var response = await handler.Handle(request, ct).ConfigureAwait(false);
            // Complete any pending request/reply
            foreach (var kvp in _pendingRequests)
            {
                kvp.Value.TrySetResult(response);
            }
        }, handler.GetType().Name);
    }

    /// <inheritdoc />
    public Task UnsubscribeAsync(string handlerId, CancellationToken cancellationToken = default)
    {
        foreach (var kvp in _subscriptions)
        {
            lock (kvp.Value) { kvp.Value.RemoveAll(s => s.Id == handlerId); }
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetSubscriptionsAsync(string eventType, CancellationToken cancellationToken = default)
    {
        if (_subscriptions.TryGetValue(eventType, out var subs))
        {
            List<string> ids;
            lock (subs) { ids = subs.Select(s => s.Id).ToList(); }
            return Task.FromResult<IEnumerable<string>>(ids);
        }
        return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
    }

    /// <inheritdoc />
    public Task ClearSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        _subscriptions.Clear();
        _logger.LogInformation("All subscriptions cleared");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public EventBusOptions GetOptions() => _options;

    /// <inheritdoc />
    public Task<PublishResult> ProcessRawDistributedEventAsync(
        string eventType,
        string rawPayload,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("InMemoryBus does not support distributed event processing; payload for {EventType} discarded", eventType);
        var result = new PublishResult(correlationId ?? Guid.NewGuid().ToString());
        return Task.FromResult(result);
    }

    private void Unsubscribe(string eventType, string subscriptionId)
    {
        if (_subscriptions.TryGetValue(eventType, out var subs))
        {
            lock (subs) { subs.RemoveAll(s => s.Id == subscriptionId); }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _publishLock.Dispose();
        _subscriptions.Clear();
        _pendingRequests.Clear();
    }
}
