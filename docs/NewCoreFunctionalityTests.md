# NewCoreFunctionalityTests

Test class that validates the newly added core functionality of the `dotnet-event-bus` library, including handler prioritization, middleware execution, retry mechanisms, dead‑letter queuing, and unsubscription behavior.

## API

### `NewCoreFunctionalityTests`
- **Purpose**: Container for unit tests that verify the core event‑bus features introduced in recent changes.
- **Parameters**: None.
- **Return value**: N/A (type constructor).
- **Exceptions**: None thrown by the constructor itself.

### `PublishAsync_ShouldInvokeHandlersInPriorityOrder`
- **Purpose**: Confirms that when multiple handlers are registered for the same event, they are invoked in the order defined by their priority values.
- **Parameters**: None.
- **Return value**: `Task` representing the asynchronous test operation.
- **Exceptions**: Throws an exception (typically `AssertFailedException` or similar) if the handlers are not invoked in the expected priority order.

### `PublishAsync_ShouldExecuteMiddlewarePipeline`
- **Purpose**: Verifies that the middleware pipeline is correctly applied to an event before it reaches the handlers.
- **Parameters**: None.
- **Return value**: `Task`.
- **Exceptions**: Throws an exception if any middleware component is not executed or if the event is altered incorrectly.

### `PublishAsync_ShouldRetryFailedHandlers`
- **Purpose**: Ensures that a handler that throws an exception is retried the configured number of times before being considered permanently failed.
- **Parameters**: None.
- **Return value**: `Task`.
- **Exceptions**: Throws an exception if the handler is not retried the expected number of times or if the failure handling deviates from the policy.

### `PublishAsync_ShouldMoveFailedHandlerToDeadLetterQueue`
- **Purpose**: Checks that after exhausting retry attempts, a failing handler is moved to the dead‑letter queue for later inspection.
- **Parameters**: None.
- **Return value**: `Task`.
- **Exceptions**: Throws an exception if the failed handler is not found in the dead‑letter queue or if the queue contains unexpected entries.

### `Unsubscribe_ShouldPreventFutureInvocations`
- **Purpose**: Validates that unsubscribing a handler prevents it from receiving any further events published after the unsubscription point.
- **Parameters**: None.
- **Return value**: `Task`.
- **Exceptions**: Throws an exception if the handler receives an event after unsubscription or if the subscription state is incorrectly reported.

## Usage

### Example 1: Running the test suite with the .NET CLI
```bash
# From the repository root
dotnet test --filter FullyQualifiedName~NewCoreFunctionalityTests
```
This command discovers and executes all test methods in `NewCoreFunctionalityTests`, reporting success or failure for each scenario.

### Example 2: Invoking a specific test method programmatically (e.g., in a custom test harness)
```csharp
using System.Threading.Tasks;
using Xunit;

public class CustomTestRunner
{
    [Fact]
    public async Task RunPriorityOrderTest()
    {
        var testInstance = new NewCoreFunctionalityTests();
        await testInstance.PublishAsync_ShouldInvokeHandlersInPriorityOrder();
        // If the method completes without throwing, the test passed.
    }
}
```
The test class can be instantiated and its async methods awaited directly; any assertion failure will propagate as an exception.

## Notes

- Each test method is designed to be **independent**; they should not rely on shared mutable state. Running tests in parallel may lead to flaky results if static or singleton objects within the event bus are not properly reset between tests.
- The test class itself is **not thread‑safe**. Concurrent invocation of its methods from multiple threads is not supported and may produce undefined behavior.
- Edge cases covered implicitly by the tests include:
  - Handlers with identical priority values (stable ordering guarantees).
  - Middleware that short‑circuits the pipeline.
  - Transient vs. permanent failures influencing retry counts.
  - Dead‑letter queue capacity limits and message ordering.
  - Unsubscribing while a handler is currently executing (the current invocation completes, but subsequent publications are skipped).
- To ensure reliable test outcomes, the test suite should be executed with a clean event‑bus instance per test method (typically achieved via test class constructors or `IAsyncLifetime` fixtures in xUnit).
