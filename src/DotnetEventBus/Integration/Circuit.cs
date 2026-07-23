using System;
using System.Threading;

public enum CircuitState
{
    Closed,
    Open,
    HalfOpen
}

public class Circuit
{
    private readonly object _lock = new();
    private CircuitState _state;
    private int _failureCount;
    private readonly int _maxFailures;
    private readonly TimeSpan _cooldownTime;
    private readonly TimeSpan _halfOpenTimeout;
    private DateTime _lastFailureTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="Circuit"/> class.
    /// </summary>
    /// <param name="maxFailures">The maximum number of failures before the circuit opens.</param>
    /// <param name="cooldownTime">The time the circuit stays open after a failure.</param>
    /// <param name="halfOpenTimeout">The time the circuit stays in the half-open state.</param>
    /// <exception cref="ArgumentNullException">Thrown if cooldownTime or halfOpenTimeout is null.</exception>
    public Circuit(int maxFailures, TimeSpan cooldownTime, TimeSpan halfOpenTimeout)
    {
        ArgumentNullException.ThrowIfNull(cooldownTime);
        ArgumentNullException.ThrowIfNull(halfOpenTimeout);

        _maxFailures = maxFailures;
        _cooldownTime = cooldownTime;
        _halfOpenTimeout = halfOpenTimeout;
        _state = CircuitState.Closed;
    }

    /// <summary>
    /// Attempts to execute an action within the circuit.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>True if the action was executed successfully, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown if action is null.</exception>
    public bool TryExecute(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        lock (_lock)
        {
            if (_state == CircuitState.Open)
            {
                if (DateTime.Now - _lastFailureTime > _cooldownTime)
                {
                    _state = CircuitState.HalfOpen;
                }
                else
                {
                    return false;
                }
            }

            if (_state == CircuitState.HalfOpen)
            {
                try
                {
                    action();
                    _state = CircuitState.Closed;
                    _failureCount = 0;
                    return true;
                }
                catch
                {
                    _state = CircuitState.Open;
                    _lastFailureTime = DateTime.Now;
                    return false;
                }
            }

            try
            {
                action();
                _failureCount = 0;
                return true;
            }
            catch
            {
                _failureCount++;
                if (_failureCount >= _maxFailures)
                {
                    _state = CircuitState.Open;
                    _lastFailureTime = DateTime.Now;
                }
                return false;
            }
        }
    }

    /// <summary>
    /// Resets the circuit to the closed state.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitState.Closed;
            _failureCount = 0;
        }
    }

    /// <summary>
    /// Gets the current state of the circuit.
    /// </summary>
    public CircuitState State => _state;

    /// <summary>
    /// Gets a value indicating whether the circuit is open.
    /// </summary>
    public bool IsOpen => _state == CircuitState.Open;

    /// <summary>
    /// Gets a value indicating whether the circuit is half-open.
    /// </summary>
    public bool IsHalfOpen => _state == CircuitState.HalfOpen;

    /// <summary>
    /// Gets a value indicating whether the circuit is closed.
    /// </summary>
    public bool IsClosed => _state == CircuitState.Closed;
}
