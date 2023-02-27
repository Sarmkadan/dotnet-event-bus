// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetEventBus.Advanced;

/// <summary>
/// Implements request-response pattern for synchronous event handling.
/// Allows handlers to return responses and clients to wait for replies.
/// Why: Enables synchronous communication patterns while using an async event bus.
/// </summary>
public class RequestResponseBus
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<object?>> _pendingRequests = [];
    private readonly TimeSpan _defaultTimeout;

    public RequestResponseBus(TimeSpan? defaultTimeout = null)
    {
        _defaultTimeout = defaultTimeout ?? TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Sends a request and waits for a response.
    /// </summary>
    public async Task<TResponse?> RequestAsync<TRequest, TResponse>(
        string eventType,
        TRequest request,
        TimeSpan? timeout = null) where TRequest : class where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(request);

        var requestId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<object?>();
        var actualTimeout = timeout ?? _defaultTimeout;

        if (!_pendingRequests.TryAdd(requestId, tcs))
        {
            throw new InvalidOperationException("Failed to register request");
        }

        try
        {
            using (var cts = new CancellationTokenSource(actualTimeout))
            {
                cts.Token.Register(() =>
                {
                    tcs.TrySetException(new TimeoutException($"Request {requestId} timed out after {actualTimeout.TotalSeconds}s"));
                });

                // TODO: Publish request event with requestId
                // await eventBus.PublishAsync(eventType, request, metadata: { requestId });

                var response = await tcs.Task;
                return response as TResponse;
            }
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
        }
    }

    /// <summary>
    /// Sends a response to a pending request.
    /// </summary>
    public bool SendResponse(string requestId, object response)
    {
        ArgumentNullException.ThrowIfNull(requestId);
        ArgumentNullException.ThrowIfNull(response);

        if (_pendingRequests.TryRemove(requestId, out var tcs))
        {
            tcs.SetResult(response);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Fails a pending request with an exception.
    /// </summary>
    public bool FailRequest(string requestId, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(requestId);
        ArgumentNullException.ThrowIfNull(exception);

        if (_pendingRequests.TryRemove(requestId, out var tcs))
        {
            tcs.SetException(exception);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the number of pending requests.
    /// </summary>
    public int GetPendingRequestCount() => _pendingRequests.Count;

    /// <summary>
    /// Cancels all pending requests.
    /// </summary>
    public void CancelAllRequests(string reason = "Bus shutting down")
    {
        var exception = new OperationCanceledException(reason);

        foreach (var kvp in _pendingRequests)
        {
            kvp.Value.TrySetException(exception);
            _pendingRequests.TryRemove(kvp.Key, out _);
        }
    }
}

/// <summary>
/// Handler base for request-response event processing.
/// </summary>
public abstract class RequestResponseHandler<TRequest, TResponse> where TRequest : class where TResponse : class
{
    protected string? RequestId { get; set; }
    protected RequestResponseBus? Bus { get; set; }

    /// <summary>
    /// Handles the request and returns a response.
    /// </summary>
    public abstract Task<TResponse> HandleAsync(TRequest request);

    /// <summary>
    /// Processes a request and sends the response back.
    /// </summary>
    public async Task ProcessRequestAsync(TRequest request, string requestId, RequestResponseBus bus)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(requestId);
        ArgumentNullException.ThrowIfNull(bus);

        RequestId = requestId;
        Bus = bus;

        try
        {
            var response = await HandleAsync(request);
            bus.SendResponse(requestId, response);
        }
        catch (Exception ex)
        {
            bus.FailRequest(requestId, ex);
        }
    }
}

/// <summary>
/// Request message for RPC-style calls.
/// </summary>
public class RequestMessage<T> where T : class
{
    public string? RequestId { get; set; }
    public required T Payload { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Response message for RPC-style calls.
/// </summary>
public class ResponseMessage<T> where T : class
{
    public string? RequestId { get; set; }
    public required T Payload { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
