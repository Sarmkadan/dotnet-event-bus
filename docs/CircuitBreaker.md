# CircuitBreaker

The `CircuitBreaker` class provides a resilience pattern implementation designed to prevent cascading failures in distributed systems by temporarily halting execution when a threshold of errors is detected. It wraps asynchronous operations, monitoring their success or failure, and automatically transitions to an open state to fail fast when errors accumulate, thereby allowing downstream services time to recover before attempts are resumed.

## API

### `public CircuitBreaker`
Initializes a new instance of the `CircuitBreaker` class. This constructor sets up the internal state machine required to track failure counts and manage the transition between closed, open, and half-open states. No parameters are required for initialization.

### `public async Task<T> ExecuteAsync<T>`
Executes a specified asynchronous function that returns a result of type `T` within the circuit breaker context.
*   **Parameters**: Accepts a `Func<Task<T>>` representing the operation to execute.
*   **Return Value**: Returns a `Task<T>` that completes with the result of the executed function if successful.
*   **Exceptions**: Throws `CircuitBreakerOpenException` if the circuit is currently open. May also propagate exceptions thrown by the provided function if they trigger the failure threshold or occur while the circuit is closed.

### `public async Task ExecuteAsync`
Executes a specified asynchronous action that does not return a result within the circuit breaker context.
*   **Parameters**: Accepts a `Func<Task>` representing the operation to execute.
*   **Return Value**: Returns a `Task` that completes when the operation finishes successfully.
*   **Exceptions**: Throws `CircuitBreakerOpenException` if the circuit is currently open. May also propagate exceptions thrown by the provided action if they trigger the failure threshold or occur while the circuit is closed.

### `public void Reset`
Manually resets the circuit breaker to its closed state, clearing any accumulated failure counts and allowing requests to flow through immediately. This method is useful for administrative intervention when external conditions indicate that the protected service has recovered before the automatic timeout expires.

### `public CircuitBreakerOpenException(string message) : base(message)`
Represents the specific exception thrown when an execution is attempted while the circuit breaker is in the open state.
*   **Parameters**: Accepts a `string message` describing the error condition.
*   **Inheritance**: Inherits from `Exception`, passing the provided message to the base constructor.
*   **Usage**: This exception indicates that the call was rejected proactively by the pattern rather than failing due to an underlying infrastructure error.

## Usage

### Example 1: Executing a Query with Return Value
The following example demonstrates wrapping a database query in the circuit breaker. If the database is unavailable repeatedly, the circuit opens, and subsequent calls immediately throw `CircuitBreakerOpenException` without hitting the database.

```csharp
public class UserService
{
    private readonly CircuitBreaker _circuitBreaker;
    private readonly IDatabase _database;

    public UserService(CircuitBreaker circuitBreaker, IDatabase database)
    {
        _circuitBreaker = circuitBreaker;
        _database = database;
    }

    public async Task<User> GetUserAsync(int id)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                // Simulated async database call
                return await _database.FetchUserByIdAsync(id);
            });
        }
        catch (CircuitBreakerOpenException ex)
        {
            // Handle fail-fast scenario, perhaps returning cached data
            Console.WriteLine($"Circuit is open: {ex.Message}");
            throw; 
        }
    }
}
```

### Example 2: Executing a Fire-and-Forget Command
This example illustrates using the non-generic overload for operations that do not return data, such as sending a notification or logging an audit event.

```csharp
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
            await _circuitBreaker.ExecuteAsync(async () =>
            {
                await _queue.PublishAsync("alerts", message);
            });
        }
        catch (CircuitBreakerOpenException)
        {
            // Fallback logic: store locally or drop based on policy
            Console.WriteLine("Alert queue circuit is open; message dropped.");
        }
    }
    
    public void ForceRecovery()
    {
        // Manually reset the circuit if the message queue is confirmed healthy
        _circuitBreaker.Reset();
    }
}
```

## Notes

*   **Thread Safety**: The `CircuitBreaker` instance is designed to be thread-safe. Multiple concurrent calls to `ExecuteAsync` from different threads will correctly share the same internal state regarding failure counts and circuit status.
*   **Exception Propagation**: When the circuit is closed, exceptions thrown by the delegated function are propagated to the caller. Depending on the internal configuration (not exposed in this signature), these exceptions may increment the failure counter. When the circuit is open, the delegated function is never invoked, and `CircuitBreakerOpenException` is thrown immediately.
*   **Manual Reset Risks**: Calling `Reset()` forces the circuit to the closed state regardless of the current failure history. If the underlying service has not actually recovered, this may lead to an immediate spike in failures as new requests flood the unstable resource. It should be used only when external health checks confirm service availability.
*   **Asynchronous Context**: Both `ExecuteAsync` overloads fully support asynchronous continuations. The state transitions occur upon the completion (success or fault) of the returned `Task`, not when the task is started.
