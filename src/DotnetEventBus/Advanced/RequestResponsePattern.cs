#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

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
public sealed class RequestResponseBus
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<object?>> _pendingRequests = [];
    private readonly TimeSpan _defaultTimeout;
    private readonly Func<string, RequestMessage<object>, Task>? _publishRequest;

    public RequestResponseBus(TimeSpan? defaultTimeout = null)
    {
        _defaultTimeout = defaultTimeout ?? TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Creates a bus that dispatches outgoing requests through the supplied publisher.
    /// The publisher receives the event type and the request message (including the request id)
    /// and is responsible for delivering it to the underlying transport.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="publishRequest"/> is null.</exception>
    public RequestResponseBus(Func<string, RequestMessage<object>, Task> publishRequest, TimeSpan? defaultTimeout = null)
        : this(defaultTimeout)
    {
        ArgumentNullException.ThrowIfNull(publishRequest);
        _publishRequest = publishRequest;
    }

    /// <summary>
    /// Sends a request and waits for a response.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="eventType">The event type to publish the request as.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="timeout">Optional timeout for the request. Uses <see cref="_defaultTimeout"/> if not specified.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The response from the handler.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="eventType"/> or <paramref name="request"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the request cannot be registered.</exception>
    /// <exception cref="TimeoutException">Thrown when the request times out.</exception>
    public async Task<TResponse?> RequestAsync<TRequest, TResponse>(
        string eventType,
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default) where TRequest : class where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(request);

        var requestId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var actualTimeout = timeout ?? _defaultTimeout;

        if (!_pendingRequests.TryAdd(requestId, tcs))
        {
            throw new InvalidOperationException("Failed to register request");
        }

        // Create a linked cancellation token source that combines the caller's token with the timeout
        using var timeoutCts = new CancellationTokenSource(actualTimeout);
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        // Register timeout callback that uses TrySetException to avoid double-faulting the TCS
        timeoutCts.Token.Register(() =>
        {
            // Only set exception if the task hasn't completed yet
            if (!tcs.Task.IsCompleted)
            {
                tcs.TrySetException(new TimeoutException($"Request {requestId} timed out after {actualTimeout.TotalSeconds}s"));
            }
        });

        // Register caller cancellation callback - use linkedCts.Token to ensure it's tied to the combined token
        linkedCts.Token.Register(() =>
        {
            // Only set exception if the task hasn't completed yet
            if (!tcs.Task.IsCompleted)
            {
                tcs.TrySetCanceled(linkedCts.Token);
            }
        });

        try
        {
            if (_publishRequest is not null)
            {
                var message = new RequestMessage<object>
                {
                    RequestId = requestId,
                    Payload = request,
                    Timeout = actualTimeout
                };

                await _publishRequest(eventType, message);
            }

            var response = await tcs.Task;
            return response as TResponse;
        }
        finally
        {
            // Ensure cleanup happens even if an exception occurs
            // Use TryRemove to safely clean up - this handles cases where timeout/cancellation callbacks already removed the entry
            _pendingRequests.TryRemove(requestId, out _);

            // Dispose the linked cancellation token source to prevent resource leaks
            linkedCts?.Dispose();
        }
    }

    /// <summary>
    /// Sends a response to a pending request.
    /// </summary>
    /// <param name="requestId">The request ID to respond to.</param>
    /// <param name="response">The response payload.</param>
    /// <returns>True if the response was successfully sent; false if the request was not found or already completed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="requestId"/> or <paramref name="response"/> is null.</exception>
    public bool SendResponse(string requestId, object response)
    {
        ArgumentNullException.ThrowIfNull(requestId);
        ArgumentNullException.ThrowIfNull(response);

        if (_pendingRequests.TryRemove(requestId, out var tcs))
        {
            // TrySetResult: the request may have already been faulted by its timeout callback or caller cancellation
            return tcs.TrySetResult(response);
        }

        return false;
    }

    /// <summary>
    /// Fails a pending request with an exception.
    /// </summary>
    /// <param name="requestId">The request ID to fail.</param>
    /// <param name="exception">The exception to propagate to the caller.</param>
    /// <returns>True if the exception was successfully set; false if the request was not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="requestId"/> or <paramref name="exception"/> is null.</exception>
    public bool FailRequest(string requestId, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(requestId);
        ArgumentNullException.ThrowIfNull(exception);

        if (_pendingRequests.TryRemove(requestId, out var tcs))
        {
            // TrySetException: the request may have already been completed by a response or timeout
            return tcs.TrySetException(exception);
        }

        return false;
    }

    /// <summary>
    /// Gets the number of pending requests.
    /// </summary>
    /// <returns>The count of currently pending requests.</returns>
    public int GetPendingRequestCount() => _pendingRequests.Count;

    /// <summary>
    /// Cancels all pending requests.
    /// </summary>
    /// <param name="reason">The reason for cancellation, included in the exception.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reason"/> is null.</exception>
    public void CancelAllRequests(string reason = "Bus shutting down")
    {
        ArgumentNullException.ThrowIfNull(reason);

        var exception = new OperationCanceledException(reason);

        foreach (var kvp in _pendingRequests)
        {
            kvp.Value.TrySetException(exception);
            _pendingRequests.TryRemove(kvp.Key, out _);
        }
    }

    /// <summary>
    /// Audits the pending requests dictionary for potential memory leaks.
    /// Checks if any requests have timed out but their entries were not cleaned up.
    /// </summary>
    /// <returns>A dictionary of leaked request IDs and their status.</returns>
    public Dictionary<string, string> AuditPendingRequests()
    {
        var leakedRequests = new Dictionary<string, string>();

        foreach (var kvp in _pendingRequests)
        {
            var requestId = kvp.Key;
            var tcs = kvp.Value;
            var taskStatus = tcs.Task.Status;

            string status = taskStatus switch
            {
                TaskStatus.RanToCompletion => "Completed",
                TaskStatus.Faulted => "Faulted",
                TaskStatus.Canceled => "Canceled",
                TaskStatus.Running => "Running",
                TaskStatus.WaitingForActivation => "Waiting",
                TaskStatus.WaitingToRun => "WaitingToRun",
                TaskStatus.WaitingForChildrenToComplete => "WaitingForChildren",
                _ => taskStatus.ToString()
            };

            leakedRequests[requestId] = status;
        }

        return leakedRequests;
    }

    /// <summary>
    /// Attempts to clean up any completed or faulted requests that were not properly removed.
    /// This is a defensive cleanup operation that should rarely be needed if the normal flow works correctly.
    /// </summary>
    /// <returns>The number of requests that were cleaned up.</returns>
    public int CleanupCompletedRequests()
    {
        var completedKeys = _pendingRequests
            .Where(kvp => kvp.Value.Task.IsCompleted)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in completedKeys)
        {
            _pendingRequests.TryRemove(key, out _);
        }

        return completedKeys.Count;
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
public sealed class RequestMessage<T> where T : class
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
public sealed class ResponseMessage<T> where T : class
{
    public string? RequestId { get; set; }
    public required T Payload { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
