#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DotnetEventBus.Integration;

/// <summary>
/// Configurable retry policy for resilient event processing.
/// Supports exponential backoff, jitter, and custom retry conditions.
/// Why: Handles transient failures gracefully without losing events.
/// </summary>
public sealed class RetryPolicy
{
    private int _maxRetries = 3;
    private TimeSpan _initialDelay = TimeSpan.FromSeconds(1);
    private double _backoffMultiplier = 2.0;
    private TimeSpan _maxDelay = TimeSpan.FromMinutes(5);
    private bool _useJitter = true;
    private Func<Exception, bool>? _retryableExceptionFilter;
    private readonly Random _random = new();

    /// <summary>
    /// Sets the maximum number of retry attempts.
    /// </summary>
    public RetryPolicy WithMaxRetries(int maxRetries)
    {
        if (maxRetries < 0)
            throw new ArgumentException("Max retries must be non-negative", nameof(maxRetries));

        _maxRetries = maxRetries;
        return this;
    }

    /// <summary>
    /// Sets the initial delay before the first retry.
    /// </summary>
    public RetryPolicy WithInitialDelay(TimeSpan delay)
    {
        if (delay < TimeSpan.Zero)
            throw new ArgumentException("Initial delay must be non-negative", nameof(delay));

        _initialDelay = delay;
        return this;
    }

    /// <summary>
    /// Sets the exponential backoff multiplier.
    /// </summary>
    public RetryPolicy WithBackoffMultiplier(double multiplier)
    {
        if (multiplier <= 1.0)
            throw new ArgumentException("Backoff multiplier must be greater than 1", nameof(multiplier));

        _backoffMultiplier = multiplier;
        return this;
    }

    /// <summary>
    /// Sets the maximum delay between retries.
    /// </summary>
    public RetryPolicy WithMaxDelay(TimeSpan maxDelay)
    {
        if (maxDelay <= TimeSpan.Zero)
            throw new ArgumentException("Max delay must be positive", nameof(maxDelay));

        _maxDelay = maxDelay;
        return this;
    }

    /// <summary>
    /// Enables or disables jitter (randomization) in retry delays.
    /// Prevents thundering herd problem.
    /// </summary>
    public RetryPolicy WithJitter(bool enabled)
    {
        _useJitter = enabled;
        return this;
    }

    /// <summary>
    /// Sets a filter to determine which exceptions are retryable.
    /// </summary>
    public RetryPolicy WithRetryableExceptionFilter(Func<Exception, bool>? filter)
    {
        _retryableExceptionFilter = filter;
        return this;
    }

    /// <summary>
    /// Executes an async operation with retry logic.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="onRetry">
    /// Optional callback invoked before each retry delay.
    /// Parameters: attempt number (1-based), exception that triggered the retry, calculated delay.
    /// </param>
    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        Func<int, Exception, TimeSpan, Task>? onRetry = null)
    {
        ArgumentNullException.ThrowIfNull(operation);

        int attempt = 0;
        Exception? lastException = null;

        while (attempt <= _maxRetries)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (!IsRetryable(ex) || attempt >= _maxRetries)
                {
                    throw;
                }

                var delay = CalculateDelay(attempt);

                if (onRetry is not null)
                {
                    await onRetry(attempt + 1, ex, delay);
                }

                await Task.Delay(delay);
                attempt++;
            }
        }

        throw lastException ?? new InvalidOperationException("Operation failed");
    }

    /// <summary>
    /// Executes an async operation with retry logic (void).
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="onRetry">
    /// Optional callback invoked before each retry delay.
    /// Parameters: attempt number (1-based), exception that triggered the retry, calculated delay.
    /// </param>
    public async Task ExecuteAsync(
        Func<Task> operation,
        Func<int, Exception, TimeSpan, Task>? onRetry = null)
    {
        ArgumentNullException.ThrowIfNull(operation);

        int attempt = 0;
        Exception? lastException = null;

        while (attempt <= _maxRetries)
        {
            try
            {
                await operation();
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (!IsRetryable(ex) || attempt >= _maxRetries)
                {
                    throw;
                }

                var delay = CalculateDelay(attempt);

                if (onRetry is not null)
                {
                    await onRetry(attempt + 1, ex, delay);
                }

                await Task.Delay(delay);
                attempt++;
            }
        }

        throw lastException ?? new InvalidOperationException("Operation failed");
    }

    private TimeSpan CalculateDelay(int attemptNumber)
    {
        // Calculate exponential backoff: initialDelay * (multiplier ^ attemptNumber)
        var exponentialDelay = _initialDelay.TotalMilliseconds * Math.Pow(_backoffMultiplier, attemptNumber);
        var delayMs = Math.Min(exponentialDelay, _maxDelay.TotalMilliseconds);

        if (_useJitter)
        {
            // Add jitter: ±10% of the calculated delay
            var jitterRange = delayMs * 0.1;
            delayMs += (_random.NextDouble() * jitterRange * 2) - jitterRange;
        }

        return TimeSpan.FromMilliseconds(delayMs);
    }

    private bool IsRetryable(Exception ex)
    {
        // Use custom filter if provided
        if (_retryableExceptionFilter is not null)
        {
            return _retryableExceptionFilter(ex);
        }

        // Default: retry on transient exceptions
        return ex is TimeoutException or InvalidOperationException;
    }
}

/// <summary>
/// Fluent builder for creating retry policies.
/// </summary>
public static class RetryPolicyBuilder
{
    public static RetryPolicy CreateDefault() => new();

    public static RetryPolicy CreateExponentialBackoff(int maxRetries = 3)
    {
        return new RetryPolicy()
            .WithMaxRetries(maxRetries)
            .WithInitialDelay(TimeSpan.FromSeconds(1))
            .WithBackoffMultiplier(2.0)
            .WithJitter(true);
    }

    public static RetryPolicy CreateLinearBackoff(int maxRetries = 3)
    {
        return new RetryPolicy()
            .WithMaxRetries(maxRetries)
            .WithInitialDelay(TimeSpan.FromSeconds(1))
            .WithBackoffMultiplier(1.5);
    }

    public static RetryPolicy CreateImmediate(int maxRetries = 3)
    {
        return new RetryPolicy()
            .WithMaxRetries(maxRetries)
            .WithInitialDelay(TimeSpan.Zero);
    }
}
