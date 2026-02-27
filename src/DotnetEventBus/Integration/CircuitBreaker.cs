#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetEventBus.Integration;

/// <summary>
/// Circuit breaker pattern implementation for resilience.
/// Stops hammering failing endpoints and allows them time to recover.
/// Why: Prevents cascading failures and improves overall system stability.
/// </summary>
public sealed class CircuitBreaker
{
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private readonly int _failureThreshold;
    private readonly TimeSpan _timeout;
    private readonly object _lock = new();

    public CircuitBreakerState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    public CircuitBreaker(int failureThreshold = 5, TimeSpan? timeout = null)
    {
        if (failureThreshold <= 0)
            throw new ArgumentException("Failure threshold must be positive", nameof(failureThreshold));

        _failureThreshold = failureThreshold;
        _timeout = timeout ?? TimeSpan.FromSeconds(60);
    }

    /// <summary>
    /// Executes an operation through the circuit breaker.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        lock (_lock)
        {
            CheckStateTransition();

            if (_state == CircuitBreakerState.Open)
            {
                throw new CircuitBreakerOpenException("Circuit breaker is open. Service is unavailable.");
            }
        }

        try
        {
            var result = await operation();
            RecordSuccess();
            return result;
        }
        catch (Exception ex)
        {
            RecordFailure();
            throw;
        }
    }

    /// <summary>
    /// Executes an operation through the circuit breaker (void).
    /// </summary>
    public async Task ExecuteAsync(Func<Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        lock (_lock)
        {
            CheckStateTransition();

            if (_state == CircuitBreakerState.Open)
            {
                throw new CircuitBreakerOpenException("Circuit breaker is open. Service is unavailable.");
            }
        }

        try
        {
            await operation();
            RecordSuccess();
        }
        catch (Exception ex)
        {
            RecordFailure();
            throw;
        }
    }

    private void CheckStateTransition()
    {
        // Open -> HalfOpen: If enough time has passed, allow a test request through
        // Note: HalfOpen -> Closed is handled by RecordSuccess() only after
        // a successful probe, not by timeout
        if (_state == CircuitBreakerState.Open &&
            DateTime.UtcNow - _lastFailureTime >= _timeout)
        {
            _state = CircuitBreakerState.HalfOpen;
            _failureCount = 0;
        }
    }

    private void RecordSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;

            if (_state == CircuitBreakerState.HalfOpen)
            {
                _state = CircuitBreakerState.Closed;
            }
        }
    }

    private void RecordFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_failureCount >= _failureThreshold)
            {
                _state = CircuitBreakerState.Open;
            }
        }
    }

    /// <summary>
    /// Manually closes the circuit breaker.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitBreakerState.Closed;
            _failureCount = 0;
        }
    }
}

public enum CircuitBreakerState
{
    Closed,     // Normal operation
    Open,       // Failing, reject all requests
    HalfOpen    // Testing if service recovered
}

public sealed class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message) { }
}
