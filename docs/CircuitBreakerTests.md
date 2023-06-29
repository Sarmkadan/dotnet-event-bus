# CircuitBreakerTests

The `CircuitBreakerTests` class serves as the comprehensive test suite for validating the behavior, state transitions, and exception handling logic of the Circuit Breaker implementation within the `dotnet-event-bus` project. It verifies that the circuit correctly transitions between Closed, Open, and Half-Open states based on configured failure thresholds and timeout durations, ensuring resilience patterns function as expected under various operational conditions including successful executions, exception bursts, and null argument scenarios.

## API

### `ExecuteAsync_WithSuccessfulOperation_ShouldReturnResult`
Validates that when an operation executes successfully without throwing exceptions, the circuit breaker returns the expected result and maintains the Closed state.
*   **Parameters**: None (uses internally configured test fixtures).
*   **Return Value**: `Task` completing upon successful assertion.
*   **Throws**: Throws an assertion failure if the result does not match expectations or the circuit state changes unexpectedly.

### `ExecuteAsync_WithExceptionsBelowThreshold_ShouldContinueProcessing`
Ensures that if the number of consecutive exceptions remains below the configured failure threshold, the circuit breaker remains in the Closed state and continues to allow operation execution.
*   **Parameters**: None.
*   **Return Value**: `Task` completing upon verification.
*   **Throws**: Throws an assertion failure if the circuit transitions to Open prematurely.

### `ExecuteAsync_WhenExceptionsExceedThreshold_ShouldTransitionToOpen`
Verifies that once the count of consecutive exceptions meets or exceeds the defined failure threshold, the circuit breaker transitions from Closed to Open.
*   **Parameters**: None.
*   **Return Value**: `Task` completing upon state verification.
*   **Throws**: Throws an assertion failure if the state does not become Open after the threshold is breached.

### `ExecuteAsync_WhenCircuitIsOpen_ShouldThrowCircuitBreakerOpenException`
Confirms that any attempt to execute an operation while the circuit is in the Open state immediately results in a `CircuitBreakerOpenException` without invoking the underlying operation.
*   **Parameters**: None.
*   **Return Value**: `Task` completing after verifying the exception type.
*   **Throws**: Expects `CircuitBreakerOpenException`; fails the test if a different exception or no exception is thrown.

### `ExecuteAsync_AfterTimeoutExpires_ShouldTransitionToHalfOpen`
Tests the timeout mechanism by verifying that after the configured duration elapses while in the Open state, the circuit automatically transitions to the Half-Open state to allow a trial execution.
*   **Parameters**: None.
*   **Return Value**: `Task` completing after the timeout period and state check.
*   **Throws**: Throws an assertion failure if the state does not transition to Half-Open after the timeout.

### `ExecuteAsync_VoidOperation_WithSuccess_ShouldCompleteSuccessfully`
Validates the handling of `Func<Task>` (void-returning) operations, ensuring that successful completion results in a completed task and maintains the Closed state.
*   **Parameters**: None.
*   **Return Value**: `Task` completing upon success.
*   **Throws**: Throws an assertion failure if the task does not complete successfully.

### `ExecuteAsync_VoidOperation_WhenCircuitIsOpen_ShouldThrow`
Ensures that attempting to execute a void-returning operation while the circuit is Open throws a `CircuitBreakerOpenException`.
*   **Parameters**: None.
*   **Return Value**: `Task` completing after exception verification.
*   **Throws**: Expects `CircuitBreakerOpenException`.

### `Constructor_WithInvalidFailureThreshold_ShouldThrowArgumentException`
Validates input arguments during instantiation, specifically ensuring that constructing a circuit breaker with an invalid failure threshold (e.g., zero or negative) throws an `ArgumentException`.
*   **Parameters**: None (instantiates internally with invalid data).
*   **Return Value**: `void`.
*   **Throws**: Expects `ArgumentException` during construction.

### `ExecuteAsync_WithNullOperation_ShouldThrowArgumentNullException`
Verifies that passing a `null` delegate for a value-returning operation results in an immediate `ArgumentNullException`.
*   **Parameters**: None.
*   **Return Value**: `Task` completing after exception verification.
*   **Throws**: Expects `ArgumentNullException`.

### `ExecuteAsync_VoidWithNullOperation_ShouldThrowArgumentNullException`
Verifies that passing a `null` delegate for a void-returning operation results in an immediate `ArgumentNullException`.
*   **Parameters**: None.
*   **Return Value**: `Task` completing after exception verification.
*   **Throws**: Expects `ArgumentNullException`.

### `ExecuteAsync_SuccessfulOperationAfterFailures_ShouldResetCircuit`
Confirms that if a trial execution in the Half-Open state succeeds, the circuit breaker resets the failure count and transitions back to the Closed state.
*   **Parameters**: None.
*   **Return Value**: `Task` completing upon state reset verification.
*   **Throws**: Throws an assertion failure if the circuit remains Half-Open or transitions to Open.

### `ExecuteAsync_HalfOpenToOpenTransition_OnFailure`
Ensures that if a trial execution in the Half-Open state fails, the circuit breaker immediately transitions back to the Open state and resets the timeout window.
*   **Parameters**: None.
*   **Return Value**: `Task` completing upon state transition verification.
*   **Throws**: Throws an assertion failure if the circuit does not return to the Open state.

## Usage

The following examples demonstrate how the test cases validate specific resilience behaviors within the test suite context.

### Example 1: Validating State Transition on Threshold Breach
This scenario illustrates the test logic used to verify that the circuit opens after a specific number of failures.

```csharp
[Test]
public async Task ExecuteAsync_WhenExceptionsExceedThreshold_ShouldTransitionToOpen()
{
    // Arrange
    var failureThreshold = 3;
    var circuitBreaker = new CircuitBreaker(failureThreshold, TimeSpan.FromMinutes(1));
    var failingOperation = new Func<Task>(() => throw new InvalidOperationException("Simulated failure"));

    // Act: Execute the failing operation enough times to exceed the threshold
    for (int i = 0; i < failureThreshold; i++)
    {
        try 
        {
            await circuitBreaker.ExecuteAsync(failingOperation);
        }
        catch (InvalidOperationException) { /* Expected */ }
    }

    // Assert: Verify the internal state has transitioned to Open
    // The test method internally asserts this state change
    await VerifyStateAsync(circuitBreaker, CircuitState.Open);
}
```

### Example 2: Verifying Exception Handling for Null Operations
This scenario demonstrates the validation of argument guards within the execution method.

```csharp
[Test]
public async Task ExecuteAsync_WithNullOperation_ShouldThrowArgumentNullException()
{
    // Arrange
    var circuitBreaker = new CircuitBreaker(3, TimeSpan.FromMinutes(1));
    Func<Task<int>> nullOperation = null;

    // Act & Assert
    // The test verifies that passing 'nullOperation' triggers the specific exception
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(
        () => circuitBreaker.ExecuteAsync(nullOperation)
    );
    
    Assert.That(exception.ParamName, Is.EqualTo("operation"));
}
```

## Notes

*   **Thread Safety**: While the test methods themselves are asynchronous, the underlying circuit breaker implementation tested here must handle concurrent access correctly. Tests like `ExecuteAsync_WhenExceptionsExceedThreshold_ShouldTransitionToOpen` implicitly rely on the atomicity of the failure counter increment and state transition logic. If race conditions exist in the implementation, these tests may exhibit flakiness under high-concurrency load runners.
*   **Time Dependencies**: The test `ExecuteAsync_AfterTimeoutExpires_ShouldTransitionToHalfOpen` is sensitive to system clock changes and thread scheduling delays. In environments with significant CPU contention, the actual elapsed time might slightly exceed the configured timeout, though the logic should tolerate minor variances.
*   **State Coupling**: Several tests (e.g., `ExecuteAsync_SuccessfulOperationAfterFailures_ShouldResetCircuit`) depend on the precise sequencing of states (Closed → Open → Half-Open → Closed). These tests assume the timeout mechanism is deterministic; mocking the timer provider is often required in unit test implementations to avoid real-time delays, although the signatures provided suggest integration-style verification.
*   **Exception Aggregation**: The tests distinguish between operations that throw expected business exceptions (which trip the circuit) and infrastructure exceptions like `ArgumentNullException` (which are thrown immediately by the guard clause). The circuit breaker logic should not count argument validation exceptions toward the failure threshold.
