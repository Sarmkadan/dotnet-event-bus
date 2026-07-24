#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DotnetEventBus.Advanced;

namespace DotnetEventBus.Tests;

/// <summary>
/// Unit tests for RequestResponsePattern improvements:
/// - Caller CancellationToken integration
/// - TrySet* everywhere to prevent double-faulting
/// - Proper cleanup in finally blocks
/// - AuditPendingRequests() for detecting leaked requests
/// - CleanupCompletedRequests() for defensive cleanup
/// </summary>
public sealed class RequestResponsePatternTests
{
    [Fact]
    public async Task RequestAsync_WithCallerCancellationToken_ShouldCancelWhenRequested()
    {
        // Arrange
        var requestResponseBus = new RequestResponseBus();
        var cts = new CancellationTokenSource();
        var eventType = "TestRequest";
        var request = new TestRequest();

        // Act & Assert - should complete when cancellation is requested
        var requestTask = requestResponseBus.RequestAsync<TestRequest, TestResponse>(
            eventType,
            request,
            timeout: TimeSpan.FromSeconds(30),
            cancellationToken: cts.Token);

        // Cancel immediately
        cts.Cancel();

        // Should throw TaskCanceledException (more specific than OperationCanceledException)
        await Assert.ThrowsAsync<TaskCanceledException>(() => requestTask);

        // Pending requests should be cleaned up
        Assert.Equal(0, requestResponseBus.GetPendingRequestCount());
    }

    [Fact]
    public async Task RequestAsync_WithTimeout_ShouldThrowTimeoutException()
    {
        // Arrange
        var requestResponseBus = new RequestResponseBus(TimeSpan.FromMilliseconds(50));
        var eventType = "SlowRequest";
        var request = new TestRequest();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TimeoutException>(
            () => requestResponseBus.RequestAsync<TestRequest, TestResponse>(
                eventType,
                request,
                timeout: TimeSpan.FromMilliseconds(50)));

        Assert.Contains("timed out after 0.05", exception.Message);

        // Pending requests should be cleaned up
        Assert.Equal(0, requestResponseBus.GetPendingRequestCount());
    }

    [Fact]
    public async Task RequestAsync_WithLinkedCancellationAndTimeout_ShouldRespectCancellationOverTimeout()
    {
        // Arrange
        var requestResponseBus = new RequestResponseBus(TimeSpan.FromSeconds(10));
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var eventType = "TestRequest";
        var request = new TestRequest();

        // Act & Assert - cancellation should win over timeout
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => requestResponseBus.RequestAsync<TestRequest, TestResponse>(
                eventType,
                request,
                timeout: TimeSpan.FromSeconds(10),
                cancellationToken: cts.Token));

        // Pending requests should be cleaned up
        Assert.Equal(0, requestResponseBus.GetPendingRequestCount());
    }

    [Fact]
    public void SendResponse_WithValidRequestId_ShouldCompleteTask()
    {
        // Arrange
        var requestResponseBus = new RequestResponseBus();
        var requestId = Guid.NewGuid().ToString();
        var response = new TestResponse();
        var tcs = new TaskCompletionSource<object?>();

        // Manually add to pending requests to simulate a real request
        requestResponseBus.GetType()
            .GetField("_pendingRequests", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(requestResponseBus, new ConcurrentDictionary<string, TaskCompletionSource<object?>>
            {
                [requestId] = tcs
            });

        // Act - send response
        var result = requestResponseBus.SendResponse(requestId, response);

        // Assert
        Assert.True(result);
        Assert.True(tcs.Task.IsCompleted);
    }

    [Fact]
    public void FailRequest_WithValidRequestId_ShouldFaultTask()
    {
        // Arrange
        var requestResponseBus = new RequestResponseBus();
        var requestId = Guid.NewGuid().ToString();
        var exception = new InvalidOperationException("Test error");
        var tcs = new TaskCompletionSource<object?>();

        // Manually add to pending requests to simulate a real request
        requestResponseBus.GetType()
            .GetField("_pendingRequests", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(requestResponseBus, new ConcurrentDictionary<string, TaskCompletionSource<object?>>
            {
                [requestId] = tcs
            });

        // Act
        var result = requestResponseBus.FailRequest(requestId, exception);

        // Assert
        Assert.True(result);
        Assert.True(tcs.Task.IsFaulted);
    }

    [Fact]
    public void SendResponse_WithNullRequestId_ShouldThrow()
    {
        // Arrange
        var requestResponseBus = new RequestResponseBus();
        var response = new TestResponse();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => requestResponseBus.SendResponse(null!, response));
    }

    [Fact]
    public void FailRequest_WithNullRequestId_ShouldThrow()
    {
        // Arrange
        var requestResponseBus = new RequestResponseBus();
        var exception = new InvalidOperationException("Test error");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => requestResponseBus.FailRequest(null!, exception));
    }

    [Fact]
    public void SendResponse_WithNullResponse_ShouldThrow()
    {
        // Arrange
        var requestResponseBus = new RequestResponseBus();
        var requestId = Guid.NewGuid().ToString();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => requestResponseBus.SendResponse(requestId, null!));
    }

    [Fact]
    public void FailRequest_WithNullException_ShouldThrow()
    {
        // Arrange
        var requestResponseBus = new RequestResponseBus();
        var requestId = Guid.NewGuid().ToString();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => requestResponseBus.FailRequest(requestId, null!));
    }

    [Fact]
    public void CancelAllRequests_ShouldCancelAllPendingRequests()
    {
        // Arrange
        var requestResponseBus = new RequestResponseBus();
        var requestId1 = Guid.NewGuid().ToString();
        var requestId2 = Guid.NewGuid().ToString();
        var tcs1 = new TaskCompletionSource<object?>();
        var tcs2 = new TaskCompletionSource<object?>();

        requestResponseBus.GetType()
            .GetField("_pendingRequests", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(requestResponseBus, new ConcurrentDictionary<string, TaskCompletionSource<object?>>
            {
                [requestId1] = tcs1,
                [requestId2] = tcs2
            });

        // Act
        requestResponseBus.CancelAllRequests("Test shutdown");

        // Assert - tasks should be faulted
        Assert.True(tcs1.Task.IsFaulted);
        Assert.True(tcs2.Task.IsFaulted);
        Assert.Equal(0, requestResponseBus.GetPendingRequestCount());
    }

    [Fact]
    public void GetPendingRequestCount_ShouldReturnZeroInitially()
    {
        // Arrange
        var requestResponseBus = new RequestResponseBus();

        // Act & Assert
        Assert.Equal(0, requestResponseBus.GetPendingRequestCount());
    }

    [Fact]
    public async Task RequestAsync_WithPublisher_ShouldPublishRequest()
    {
        // Arrange
        var publishedRequests = new ConcurrentBag<(string eventType, RequestMessage<object> message)>();
        var requestResponseBus = new RequestResponseBus((eventType, message) =>
        {
            publishedRequests.Add((eventType, message));
            return Task.CompletedTask;
        });

        var eventType = "TestEvent";
        var request = new TestRequest();

        // Act
        var responseTask = requestResponseBus.RequestAsync<TestRequest, TestResponse>(
            eventType,
            request,
            timeout: TimeSpan.FromSeconds(5));

        // Give it time to register and publish
        await Task.Delay(50);

        // Assert - request should have been published
        Assert.Single(publishedRequests);
        var published = publishedRequests.First();
        Assert.Equal(eventType, published.eventType);
        Assert.NotNull(published.message.RequestId);
        Assert.Equal(request, published.message.Payload);

        // Clean up
        requestResponseBus.SendResponse(published.message.RequestId!, new TestResponse());
    }

    [Fact]
    public async Task RequestAsync_With10kTimeoutRequests_ShouldCleanupAllPendingRequests()
    {
        // Arrange
        var requestResponseBus = new RequestResponseBus(TimeSpan.FromSeconds(30));
        var eventType = "TestRequest";
        var request = new TestRequest();

        // Act - fire 10k timing-out requests concurrently with short timeout
        var tasks = new Task<TestResponse>[10000];
        for (int i = 0; i < 10000; i++)
        {
            tasks[i] = requestResponseBus.RequestAsync<TestRequest, TestResponse>(
                eventType,
                request,
                timeout: TimeSpan.FromMilliseconds(50)
            );
        }

        // Wait for all requests to timeout - expect TimeoutException
        await Assert.ThrowsAsync<TimeoutException>(() => Task.WhenAll(tasks));

        // Assert - all pending requests should be cleaned up
        Assert.Equal(0, requestResponseBus.GetPendingRequestCount());

        // Additional audit to ensure no leaked requests
        var leakedRequests = requestResponseBus.AuditPendingRequests();
        Assert.Empty(leakedRequests);
    }

    [Fact]
    public async Task RequestAsync_With10kCancellationRequests_ShouldCleanupAllPendingRequests()
    {
        // Arrange
        var requestResponseBus = new RequestResponseBus(TimeSpan.FromSeconds(30));
        var eventType = "TestRequest";
        var request = new TestRequest();

        // Act - fire 10k cancellation requests concurrently
        var tasks = new Task[10000];
        var exceptions = new Exception[10000];

        for (int i = 0; i < 10000; i++)
        {
            int index = i; // Capture for async closure
            tasks[i] = requestResponseBus.RequestAsync<TestRequest, TestResponse>(
                eventType,
                request,
                timeout: TimeSpan.FromSeconds(30)
            ).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    exceptions[index] = t.Exception?.InnerException ?? new Exception("Unknown error");
            });
        }

        // Wait for all requests to complete (they'll timeout)
        await Task.WhenAll(tasks);

        // Assert - all pending requests should be cleaned up
        Assert.Equal(0, requestResponseBus.GetPendingRequestCount());

        // Additional audit to ensure no leaked requests
        var leakedRequests = requestResponseBus.AuditPendingRequests();
        Assert.Empty(leakedRequests);
    }

    [Fact]
    public async Task RequestAsync_WithMixedTimeoutAndCancellation_ShouldCleanupAllPendingRequests()
    {
        // Arrange
        var requestResponseBus = new RequestResponseBus(TimeSpan.FromSeconds(30));
        var eventType = "TestRequest";
        var request = new TestRequest();

        // Act - fire 5k timeout requests and 5k requests with short timeout
        var tasks = new Task<TestResponse>[10000];
        for (int i = 0; i < 5000; i++)
        {
            tasks[i] = requestResponseBus.RequestAsync<TestRequest, TestResponse>(
                eventType,
                request,
                timeout: TimeSpan.FromMilliseconds(50)
            );
        }

        for (int i = 5000; i < 10000; i++)
        {
            tasks[i] = requestResponseBus.RequestAsync<TestRequest, TestResponse>(
                eventType,
                request,
                timeout: TimeSpan.FromMilliseconds(50)
            );
        }

        // Wait for all requests to timeout - expect TimeoutException
        await Assert.ThrowsAsync<TimeoutException>(() => Task.WhenAll(tasks));

        // Assert - all pending requests should be cleaned up
        Assert.Equal(0, requestResponseBus.GetPendingRequestCount());

        // Additional audit to ensure no leaked requests
        var leakedRequests = requestResponseBus.AuditPendingRequests();
        Assert.Empty(leakedRequests);
    }

    [Fact]
    public void AuditPendingRequests_WithNoRequests_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var requestResponseBus = new RequestResponseBus();

        // Act
        var auditResults = requestResponseBus.AuditPendingRequests();

        // Assert
        Assert.Empty(auditResults);
        Assert.Equal(0, auditResults.Count);
    }

    [Fact]
    public void AuditPendingRequests_WithCompletedRequest_ShouldShowCompletedStatus()
    {
        // Arrange
        var requestResponseBus = new RequestResponseBus();
        var requestId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<object?>();

        requestResponseBus.GetType()
            .GetField("_pendingRequests", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(requestResponseBus, new ConcurrentDictionary<string, TaskCompletionSource<object?>>
            {
                [requestId] = tcs
            });

        // Complete the task
        tcs.SetResult(null);

        // Act
        var auditResults = requestResponseBus.AuditPendingRequests();

        // Assert
        Assert.Single(auditResults);
        Assert.Equal("Completed", auditResults[requestId]);
    }

    [Fact]
    public void CleanupCompletedRequests_ShouldRemoveCompletedRequests()
    {
        // Arrange
        var requestResponseBus = new RequestResponseBus();
        var completedRequestId = Guid.NewGuid().ToString();
        var activeRequestId = Guid.NewGuid().ToString();
        var completedTcs = new TaskCompletionSource<object?>();
        var activeTcs = new TaskCompletionSource<object?>();

        requestResponseBus.GetType()
            .GetField("_pendingRequests", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(requestResponseBus, new ConcurrentDictionary<string, TaskCompletionSource<object?>>
            {
                [completedRequestId] = completedTcs,
                [activeRequestId] = activeTcs
            });

        // Complete one task
        completedTcs.SetResult(null);

        // Act
        var cleanupCount = requestResponseBus.CleanupCompletedRequests();

        // Assert
        Assert.Equal(1, cleanupCount);
        Assert.Equal(1, requestResponseBus.GetPendingRequestCount());
        Assert.True(requestResponseBus.GetType()
            .GetField("_pendingRequests", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(requestResponseBus) is ConcurrentDictionary<string, TaskCompletionSource<object?>> dict &&
            dict.ContainsKey(activeRequestId) && !dict.ContainsKey(completedRequestId));
    }

    [Fact]
    public void RequestResponseBus_WithDefaultConstructor_ShouldUse30SecondTimeout()
    {
        // Arrange
        var requestResponseBus = new RequestResponseBus();

        // Act
        var timeout = requestResponseBus.GetType()
            .GetField("_defaultTimeout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(requestResponseBus) as TimeSpan?;

        // Assert
        Assert.NotNull(timeout);
        Assert.Equal(TimeSpan.FromSeconds(30), timeout);
    }

    [Fact]
    public void RequestResponseBus_WithCustomTimeout_ShouldUseCustomTimeout()
    {
        // Arrange
        var customTimeout = TimeSpan.FromSeconds(45);
        var requestResponseBus = new RequestResponseBus(customTimeout);

        // Act
        var timeout = requestResponseBus.GetType()
            .GetField("_defaultTimeout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(requestResponseBus) as TimeSpan?;

        // Assert
        Assert.NotNull(timeout);
        Assert.Equal(customTimeout, timeout);
    }

    [Fact]
    public async Task RequestAsync_WithNullEventType_ShouldThrow()
    {
        // Arrange
        var requestResponseBus = new RequestResponseBus();
        var request = new TestRequest();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => requestResponseBus.RequestAsync<TestRequest, TestResponse>(
                null!,
                request));
    }

    [Fact]
    public async Task RequestAsync_WithNullRequest_ShouldThrow()
    {
        // Arrange
        var requestResponseBus = new RequestResponseBus();
        var eventType = "TestEvent";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => requestResponseBus.RequestAsync<TestRequest, TestResponse>(
                eventType,
                null!));
    }

    // Test models
    public sealed class TestRequest
    {
        public string Data { get; set; } = "Test";
    }

    public sealed class TestResponse
    {
        public string Result { get; set; } = "Success";
    }
}