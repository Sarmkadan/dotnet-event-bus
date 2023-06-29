# TestEvent

TestEvent is a simple data transfer object used in the unit‑test suite for the dotnet-event-bus library. It carries a string payload, an integer value, and a counter that tracks how many times its handler has been invoked. The class also provides an asynchronous handler method and a set of test methods that validate the behavior of the event bus (subscription, publishing, priority handling, and option retrieval).

## API

### Data
- **Purpose:** Holds a string value that can be inspected by test handlers to verify that the event payload was delivered correctly.
- **Parameters:** None.
- **Return value:** The current string value.
- **Exceptions:** None.

### Value is a plain auto‑property; reading or writing never throws.

### Value
- **Purpose:** Holds an integer value that can be inspected by test handlers to verify that the event payload was delivered correctly.
- **Parameters:** None.
- **Return value:** The current integer value.
- **Exceptions:** None.

### CallCount
- **Purpose:** Tracks the number of times the `Handle` method has been executed for this event instance. Tests increment this counter to assert handler invocation counts.
- **Parameters:** None.
- **Return value:** The current invocation count.
- **Exceptions:** None.

### Handle
- **Purpose:** Asynchronously processes the event. The default implementation increments `CallCount` and may read the `Data` and `Value` properties for test validation.
- **Parameters:** None.
- **Return value:** A `Task` that completes when the asynchronous processing finishes.
- **Exceptions:** May throw any exception thrown by user‑provided logic inside the method (e.g., invalid operation if the event is in an unexpected state).

### EventBusTests
- **Purpose:** Provides access to the test suite’s event‑bus instance, allowing tests to configure subscriptions, publish events, and inspect internal state.
- **Parameters:** None.
- **Return value:** An instance of the `EventBusTests` type that contains the bus under test.
- **Exceptions:** None.

### PublishAsync_WithValidEvent_ShouldInvokeSubscribedHandlers
- **Purpose:** Asserts that publishing a properly constructed `TestEvent` results in all subscribed handlers being invoked exactly once.
- **Parameters:** None.
- **Return value:** A `Task` that completes when the test finishes.
- **Exceptions:** Throws if the expected handler invocations do not occur (assertion failure).

### PublishAsync_WithMultipleHandlers_ShouldInvokeAllHandlers
- **Purpose:** Verifies that when multiple handlers are subscribed to the same event type, each handler is invoked upon publishing.
- **Parameters:** None.
- **Return value:** A `Task` that completes when the test finishes.
- **Exceptions:** Throws if any handler fails to be invoked.

### Subscribe_WithDelegate_ShouldReturnDisposable
- **Purpose:** Checks that subscribing with a delegate returns a disposable object that, when disposed, removes the subscription.
- **Parameters:** None.
- **Return value:** A `Task` that completes when the test finishes.
- **Exceptions:** Throws if the returned disposable does not correctly unsubscribe the handler.

### SubscribeSync_WithSynchronousHandler_ShouldWork
- **Purpose:** Confirms that a synchronous handler (non‑async delegate) can be subscribed and invoked correctly.
- **Parameters:** None.
- **Return value:** A `Task` that completes when the test finishes.
- **Exceptions:** Throws if the synchronous handler is not invoked or throws unexpectedly.

### GetSubscriptions_WithRegisteredHandlers_ShouldReturnHandlerNames
- **Purpose:** Ensures that the event bus’s subscription inspection API returns the correct names (or identifiers) for all currently registered handlers.
- **Parameters:** None.
- **Return value:** A `Task` that completes when the test finishes.
- **Exceptions:** Throws if the returned collection does not match the expected handler names.

### PublishAsync_WithPriority_ShouldExecuteInOrder
- **Purpose:** Validates that handlers registered with different priority values are executed in the correct order when an event is published.
- **Parameters:** None.
- **Return value:** A `Task` that completes when the test finishes.
- **Exceptions:** Throws if the execution order does not follow the priority specification.

### ClearSubscriptions_ShouldRemoveAllSubscriptions
- **Purpose:** Asserts that calling the bus’s clear‑subscriptions method removes every existing subscription, so subsequent publishes invoke no handlers.
- **Parameters:** None.
- **Return value:** A `Task` that completes when the test finishes.
- **Exceptions:** Throws if any handler is still invoked after clearing.

### GetOptions_ShouldReturnCurrentOptions
- **Purpose:** Checks that the bus’s options retrieval method returns the exact options object that was last applied.
- **Parameters:** None.
- **Return value:** None (the method returns `void`).
- **Exceptions:** Throws if the retrieved options differ from the expected configuration.

## Usage

```csharp
// Example 1: Basic publish/subscribe workflow
var bus = new EventBus(); // assumed implementation from the library
var testEvent = new TestEvent { Data = "hello", Value = 42 };

using var subscription = bus.Subscribe<TestEvent>(async ev =>
{
    ev.CallCount++; // increment counter defined on the event
    Assert.Equal("hello", ev.Data);
    Assert.Equal(42, ev.Value);
});

await bus.PublishAsync(testEvent);
Assert.Equal(1, testEvent.CallCount);
```

```csharp
// Example 2: Verifying priority‑ordered execution
var bus = new EventBus(new BusOptions { EnablePriority = true });
var @event = new TestEvent();

int[] order = new int[0];
bus.Subscribe<TestEvent>(1, async _ => { order = order.Append(1).ToArray(); });
bus.Subscribe<TestEvent>(10, async _ => { order = order.Append(10).ToArray(); });
bus.Subscribe<TestEvent>(5, async _ => { order = order.Append(5).ToArray(); });

await bus.PublishAsync(@event);
// Expected order: 10, 5, 1 (higher number = higher priority)
Assert.Equal(new[] { 10, 5, 1 }, order);
```

## Notes

- The `Data` and `Value` properties are simple mutable fields; they are not synchronized automatically. If multiple handlers invoke `Handle` concurrently, race conditions may occur when reading or writing these fields. Tests should ensure proper synchronization or rely on the fact that the test bus invokes handlers sequentially unless explicitly configured for parallelism.
- `CallCount` is also mutable and not thread‑safe. In scenarios where handlers might run in parallel, consider using `Interlocked.Increment` or similar mechanisms to avoid lost increments.
- The `Handle` method is declared `async` but takes no parameters in the current signature. If derived overrides introduce parameters, they must preserve the override contract; otherwise the base method will be called with its parameterless signature.
- The `EventBusTests` member provides a handle to the test fixture’s bus instance. Modifying the bus through this member (e.g., changing options) will affect all subsequent tests that share the same fixture unless the bus is reset between tests.
- All test methods follow the naming convention used by xUnit and return `Task` (except the void options test). They are intended to be executed by the test runner; invoking them directly outside of a test context may produce undefined behavior because they rely on the test class’s state.
- No member throws exceptions as part of its normal contract; exceptions arise only from failed assertions inside the test methods or from user‑provided logic within `Handle`.
