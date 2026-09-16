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

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
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

    /// <summary>
    /// Initializes a new instance of the CircuitBreaker class.
    /// </summary>
    /// <param name="failureThreshold">The number of consecutive failures before the circuit opens. Must be positive.</param>
    /// <param name="timeout">The time to wait before transitioning from Open to HalfOpen. Defaults to 60 seconds.</param>
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

            // A half-open probe exists to test recovery with a single request; if it fails,
            // the service is still down and the circuit must reopen immediately rather than
            // waiting for _failureCount to climb back up to _failureThreshold (which would
            // let further requests through to a service already known to be failing).
            if (_state == CircuitBreakerState.HalfOpen || _failureCount >= _failureThreshold)
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

/// <summary>
/// Represents the state of the circuit breaker.
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// Normal operation - requests are allowed through.
    /// </summary>
    Closed,

    /// <summary>
    /// Failing state - all requests are rejected immediately.
    /// </summary>
    Open,

    /// <summary>
    /// Testing state - allows a single request through to test if the service has recovered.
    /// </summary>
    HalfOpen
}

/// <summary>
/// Exception thrown when the circuit breaker is open.
/// </summary>
public sealed class CircuitBreakerOpenException : Exception
{
    /// <summary>
    /// Initializes a new instance of the CircuitBreakerOpenException class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public CircuitBreakerOpenException(string message) : base(message) { }
}
