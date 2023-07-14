#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Middleware;

/// <summary>
/// Middleware that enforces rate limiting on event publishing.
/// Uses a sliding window algorithm to track request rates per event type.
/// Why: Prevents system overload and ensures fair resource distribution across event types.
/// </summary>
public sealed class RateLimitingMiddleware
{
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly Dictionary<string, RateLimitBucket> _buckets = [];
    private readonly int _requestsPerWindow;
    private readonly TimeSpan _timeWindow;
    private readonly SemaphoreSlim _bucketLock = new(1, 1);

    public RateLimitingMiddleware(
        ILogger<RateLimitingMiddleware> logger,
        int requestsPerWindow = 1000,
        TimeSpan? timeWindow = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _requestsPerWindow = requestsPerWindow;
        _timeWindow = timeWindow ?? TimeSpan.FromSeconds(60);
    }

    public EventBusMiddleware Create(EventBusMiddleware next)
    {
        return async (context) =>
        {
            if (!await IsAllowed(context.EventType))
            {
                _logger.LogWarning(
                    "Event {EventType} rate limited - quota exceeded for time window",
                    context.EventType);

                throw new RateLimitExceededException(
                    $"Rate limit exceeded for event type: {context.EventType}");
            }

            // Track this request
            await RecordRequest(context.EventType);

            // Proceed to next middleware
            await next(context);
        };
    }

    private async Task<bool> IsAllowed(string eventType)
    {
        await _bucketLock.WaitAsync();
        try
        {
            if (!_buckets.ContainsKey(eventType))
            {
                _buckets[eventType] = new RateLimitBucket(_timeWindow);
            }

            var bucket = _buckets[eventType];
            return bucket.IsAllowed(_requestsPerWindow);
        }
        finally
        {
            _bucketLock.Release();
        }
    }

    private async Task RecordRequest(string eventType)
    {
        await _bucketLock.WaitAsync();
        try
        {
            if (_buckets.TryGetValue(eventType, out var bucket))
            {
                bucket.RecordRequest();
            }
        }
        finally
        {
            _bucketLock.Release();
        }
    }

    private class RateLimitBucket
    {
        private readonly Queue<long> _timestamps = [];
        private readonly TimeSpan _window;

        public RateLimitBucket(TimeSpan window)
        {
            _window = window;
        }

        public bool IsAllowed(int limit)
        {
            var now = Stopwatch.GetTimestamp();
            var windowStart = now - (long)(_window.TotalMilliseconds * Stopwatch.Frequency / 1000);

            // Remove old timestamps outside the window
            while (_timestamps.TryPeek(out var timestamp) && timestamp < windowStart)
            {
                _timestamps.Dequeue();
            }

            return _timestamps.Count < limit;
        }

        public void RecordRequest()
        {
            _timestamps.Enqueue(Stopwatch.GetTimestamp());
        }
    }
}

public sealed class RateLimitExceededException : Exception
{
    public RateLimitExceededException(string message) : base(message) { }
}
