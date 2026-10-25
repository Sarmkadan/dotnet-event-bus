# CircuitBreaker

`CircuitBreaker` (namespace `DotnetEventBus.Integration`) is a resilience primitive that wraps asynchronous operations and stops calling a failing dependency for a while. After a configurable number of consecutive failures the circuit opens and calls fail fast with `CircuitBreakerOpenException`, without invoking the operation. After a timeout the breaker lets a probe call through to check whether the dependency has recovered.

Source: `src/DotnetEventBus/Integration/CircuitBreaker.cs`. Related types: `CircuitBreakerState` (same file), `CircuitBreakerOpenException` (same file), `CircuitBreakerValidation` (`CircuitBreakerValidation.cs`), and the defaults in `EventBusConstants.CircuitBreaker`.

## Constructor

### `public CircuitBreaker(int failureThreshold = EventBusConstants.CircuitBreaker.DefaultFailureThreshold, TimeSpan? timeout = null)`

| Parameter | Default | Meaning |
|-----------|---------|---------|
| `failureThreshold` | `5` (`DefaultFailureThreshold`) | Consecutive failures in the Closed state that open the circuit. Must be greater than zero. |
| `timeout` | `60` seconds (`DefaultTimeoutSeconds`) | How long the circuit stays Open before the next call is allowed through as a probe. |

Throws `ArgumentException` (parameter name `failureThreshold`) when `failureThreshold <= 0`. The `timeout` value is not validated.

The breaker starts in `CircuitBreakerState.Closed`.

## Public members

| Member | Description |
|--------|-------------|
| `CircuitBreakerState State { get; }` | Current state, read under the internal lock. Reading it does not trigger the Open → HalfOpen transition; that happens on the next `ExecuteAsync` call. |
| `Task<T> ExecuteAsync<T>(Func<Task<T>> operation)` | Runs `operation` through the breaker and returns its result. |
| `Task ExecuteAsync(Func<Task> operation)` | Non-generic overload for operations without a result. |
| `void Reset()` | Forces the state to `Closed` and clears the failure count. |

Both `ExecuteAsync` overloads throw `ArgumentNullException` if `operation` is null. That check runs before any state check and does not count as a failure.

There is no public property for the failure count or the timeout.

## States

`CircuitBreakerState` has three values:

| State | Behaviour of `ExecuteAsync` |
|-------|-----------------------------|
| `Closed` | Normal operation. The operation is invoked. |
| `Open` | Rejected immediately with `CircuitBreakerOpenException`. The operation is not invoked. |
| `HalfOpen` | The operation is invoked as a probe to test recovery. |

Failures and successes are recorded for every call that is invoked. Any exception thrown by the operation (including a synchronous throw, `OperationCanceledException`, and so on) counts as a failure. The original exception is rethrown unchanged.

## Transitions

```
            failures >= failureThreshold
   Closed ─────────────────────────────────► Open
     ▲                                        │  │
     │                                        │  │ call while
     │ success while HalfOpen                 │  │ (now - lastFailure) < timeout:
     │                                        │  │ throw CircuitBreakerOpenException
     │                                        │  │
     │        (now - lastFailure) >= timeout  │  │ failure while Open
     │        on next ExecuteAsync            │  │ updates lastFailure
     │                                        ▼  │ (restarts the timeout)
     └──────────────────────────────────── HalfOpen
                                              │
                                              │ failure while HalfOpen
                                              └──────────────► Open
```

Transitions in detail:

1. **Closed → Open.** A failed call increments the failure count and stores the failure time (`DateTime.UtcNow`). When the count reaches `failureThreshold`, the state becomes `Open`.
2. **Closed, successful call.** Resets the failure count to zero. The state stays `Closed`. Failures are therefore only counted when they are consecutive.
3. **Open → HalfOpen.** Checked at the start of every `ExecuteAsync` call. If `UtcNow - lastFailureTime >= timeout`, the state becomes `HalfOpen` and the failure count is reset to zero. The call then proceeds as a probe. The transition is performed lazily, when a call arrives. There is no background timer.
4. **HalfOpen → Closed.** A successful call while `HalfOpen` closes the circuit and resets the failure count.
5. **HalfOpen → Open.** A failed call while `HalfOpen` reopens the circuit immediately, without waiting for `failureThreshold` failures. The failure time is updated, so the timeout starts again.
6. **Any state → Closed via `Reset()`.** Manual override. The failure count is cleared. The stored failure time is not changed, but it is unused while `Closed`.

Notes on the transitions:

* **Open rejects synchronously.** A rejected call does not wait for the timeout. It throws straight away.
* **In-flight calls can finish after a transition.** A call admitted while `Closed` may complete after the circuit has opened. If it succeeds, the failure count is reset, but the state stays `Open` because only `HalfOpen` is closed by a success. If it fails while `Open`, it updates the failure time and extends the open period.
* **Several probes can run in HalfOpen.** The admission check only rejects calls when the state is `Open`, so while the state is `HalfOpen` every concurrent caller is admitted. The first successful probe closes the circuit. A failing probe reopens it. The breaker does not limit HalfOpen to a single probe request.
* **`timeout` is measured with `DateTime.UtcNow`.** It is not injectable, so tests that need to control time have to use real waits or a small timeout.

## Usage

### Example 1: Wrapping a call that returns a value

The caller catches `CircuitBreakerOpenException` and decides on a fallback. The breaker is typically created once and shared by all callers of the same dependency.

```csharp
using DotnetEventBus.Integration;

public class UserService
{
    private readonly CircuitBreaker _circuitBreaker;
    private readonly IDatabase _database;

    public UserService(CircuitBreaker circuitBreaker, IDatabase database)
    {
        _circuitBreaker = circuitBreaker;
        _database = database;
    }

    public async Task<User?> GetUserAsync(int id)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(() => _database.FetchUserByIdAsync(id));
        }
        catch (CircuitBreakerOpenException)
        {
            // Fail fast: the database is considered unavailable. Serve cached data or rethrow.
            return null;
        }
    }
}
```

The breaker is created with explicit settings, for example in the composition root:

```csharp
var userBreaker = new CircuitBreaker(
    failureThreshold: 3,
    timeout: TimeSpan.FromSeconds(30));

services.AddSingleton(userBreaker);
```

### Example 2: Wrapping a call with no return value

Use the non-generic overload for commands such as publishing a notification.

```csharp
using DotnetEventBus.Integration;

public class NotificationService
{
    private readonly CircuitBreaker _circuitBreaker;
    private readonly IMessageQueue _queue;

    public NotificationService(CircuitBreaker circuitBreaker, IMessageQueue queue)
    {
        _circuitBreaker = circuitBreaker;
        _queue = queue;
    }

    public async Task SendAlertAsync(string message)
    {
        try
        {
            await _circuitBreaker.ExecuteAsync(() => _queue.PublishAsync("alerts", message));
        }
        catch (CircuitBreakerOpenException)
        {
            // Fallback policy: store locally or drop. Here the alert is dropped.
        }
    }

    public void ForceRecovery()
    {
        // Only call this after an out-of-band health check confirms the queue is healthy.
        _circuitBreaker.Reset();
    }
}
```

### Example 3: Observing the state

`State` is useful for diagnostics and health endpoints. Do not use it to decide whether to call the operation. Checking the state and then calling `ExecuteAsync` is not atomic, and `ExecuteAsync` already applies the correct rules.

```csharp
var status = new
{
    Dependency = "user-db",
    Circuit = userBreaker.State.ToString(), // Closed, Open or HalfOpen
};
```

## Notes

* **Thread safety.** State is guarded by a private lock. Concurrent `ExecuteAsync` calls and `Reset()` calls are safe. The operation itself runs outside the lock, so calls run in parallel.
* **Exceptions.** Exceptions from the operation propagate to the caller unchanged. Every exception counts toward the threshold, so validation or cancellation errors can open the circuit. Wrap only calls to the dependency, not the whole handler, or filter the exception type before it reaches the breaker.
* **Cancellation.** `ExecuteAsync` does not take a `CancellationToken`. Pass a token to the operation itself. A cancelled operation counts as a failure.
* **Manual `Reset()`.** It sets the state to `Closed` regardless of history. If the dependency is still failing, the next failures will open the circuit again only after `failureThreshold` consecutive failures. Use `Reset()` only when an external health check shows the dependency is available.
* **Validation.** `CircuitBreakerValidation.Validate()` and `IsValid()` currently report no problems for any instance. They check only that the instance is not null.
* **One breaker per dependency.** Sharing one instance across unrelated dependencies means one failing dependency can open the circuit for all of them.
