# EventBusIntegrationTests

`EventBusIntegrationTests` is a test suite that validates the end-to-end behavior of the `dotnet-event-bus` library under realistic, multi-component scenarios. It exercises the full lifecycle of event publishing and subscription, including middleware pipelines, retry policies, dead-letter capture, circuit breaker integration, batch publishing, priority ordering, event filtering, parallel execution, and metrics collection. Each test method targets a specific cross-cutting concern, ensuring that the event bus infrastructure behaves correctly when composed with resilience, observability, and concurrency mechanisms.

## API

### `public async Task EventBus_PublishAndSubscribe_FullWorkflow`
Validates the fundamental publish-subscribe contract: an event published to the bus is received and processed by a registered subscriber. Covers serialization, routing, handler invocation, and acknowledgment.  
**Parameters:** None.  
**Returns:** A completed task upon successful delivery and processing.  
**Throws:** Timeout or assertion failures if the event is not delivered within the expected window or the handler is not invoked.

### `public async Task EventBus_WithMultipleHandlers_AllHandlersAreInvoked`
Ensures that when multiple handlers are registered for the same event type, every handler receives a copy of the published event independently.  
**Parameters:** None.  
**Returns:** A completed task after confirming that each handler executed exactly once.  
**Throws:** Assertion failures if any handler is skipped, invoked more than once, or invoked out of expected sequence.

### `public async Task EventBus_WithFailingHandler_DeadLetterQueueCaptures`
Verifies that an event whose handler throws an unrecoverable exception is moved to the dead-letter queue rather than being silently dropped or re-delivered indefinitely.  
**Parameters:** None.  
**Returns:** A completed task once the dead-letter queue contains the failed event with preserved metadata.  
**Throws:** Assertion failures if the dead-letter queue is empty, the event payload is corrupted, or the original exception context is missing.

### `public async Task EventBus_WithRetryPolicy_RetriesOnTransientFailure`
Confirms that a transient failure in a handler triggers the configured retry policy, and that the event is successfully processed after the specified number of retries.  
**Parameters:** None.  
**Returns:** A completed task when the handler eventually succeeds within the retry budget.  
**Throws:** Assertion failures if retries are not attempted, the retry count is incorrect, or the event is prematurely sent to the dead-letter queue.

### `public async Task CircuitBreaker_WithEventBus_ProtectsFromCascadingFailures`
Tests that a circuit breaker wrapping the event handler transitions to the open state after consecutive failures exceed the threshold, preventing further handler invocations for a cooldown period.  
**Parameters:** None.  
**Returns:** A completed task after the circuit opens and subsequent events are rejected without invoking the failing handler.  
**Throws:** Assertion failures if the circuit does not open, opens too early, or allows invocations while open.

### `public async Task MetricsCollector_TrackingEndToEnd`
Validates that the metrics collector records key telemetry data—such as publish count, handler execution duration, success/failure rates—across a full publish-consume cycle.  
**Parameters:** None.  
**Returns:** A completed task once the collected metrics reflect the expected values.  
**Throws:** Assertion failures if required metrics are absent, counters are stale, or timing data falls outside acceptable bounds.

### `public async Task EventFilter_IntegratedWithEventBus`
Ensures that an event filter applied to the bus correctly excludes events that do not match the filter predicate, while allowing matching events to reach handlers.  
**Parameters:** None.  
**Returns:** A completed task after verifying that filtered-out events never trigger handlers and allowed events are processed normally.  
**Throws:** Assertion failures if a filtered event leaks through or a valid event is blocked.

### `public async Task EventBus_WithPriorities_ExecutesInOrder`
Demonstrates that when events carry priority metadata, the bus dispatches them to handlers in priority order rather than arrival order.  
**Parameters:** None.  
**Returns:** A completed task after confirming the handler execution sequence matches the expected priority ordering.  
**Throws:** Assertion failures if events are processed out of priority sequence or priority inversion occurs.

### `public async Task BatchEventPublisher_AccumulatesAndFlushes`
Verifies that the batch publisher accumulates events until a size or time threshold is reached, then flushes them as a single batch to the underlying bus.  
**Parameters:** None.  
**Returns:** A completed task after the batch is flushed and all events are delivered.  
**Throws:** Assertion failures if the batch is flushed prematurely, events are lost, or the batch size constraint is violated.

### `public async Task BatchEventPublisher_WithErrorIsolation_ProcessesAllEvents`
Confirms that when one event in a batch causes a handler failure, the remaining events in the batch are still processed successfully (error isolation within the batch).  
**Parameters:** None.  
**Returns:** A completed task after the healthy events are acknowledged and the failing event is routed to the dead-letter queue.  
**Throws:** Assertion failures if a single failure aborts the entire batch or healthy events are not delivered.

### `public async Task EventBus_WithMiddlewarePipeline_ExecutesInOrder`
Tests that a chain of middleware components registered on the bus executes in the configured order around each handler invocation, with each middleware able to inspect or modify the event context.  
**Parameters:** None.  
**Returns:** A completed task after verifying the middleware execution trace matches the expected sequence.  
**Throws:** Assertion failures if middleware order is reversed, a middleware is skipped, or the pipeline short-circuits incorrectly.

### `public async Task EventBus_WithParallelPublishing_HandlesMultipleThreads`
Stress-tests the bus by publishing events concurrently from multiple threads and asserting that all events are delivered correctly without corruption, duplication, or deadlocks.  
**Parameters:** None.  
**Returns:** A completed task after all parallel publishes are acknowledged and every handler receives the correct number of invocations.  
**Throws:** Assertion failures if events are lost, duplicated, or internal state is corrupted under concurrent access.

### `public async Task MetricsCollector_ThreadSafe_WithConcurrentRecording`
Validates that the metrics collector remains accurate and free of race conditions when multiple threads record metrics simultaneously.  
**Parameters:** None.  
**Returns:** A completed task once concurrent recordings complete and the aggregated metrics match the sum of individual contributions.  
**Throws:** Assertion failures if counters are inconsistent, updates are lost, or concurrent access causes exceptions.

## Usage

```csharp
// Example 1: Full integration test combining retry policy, dead-letter queue, and metrics
[Test]
public async Task FullResiliencePipeline_EndToEnd()
{
    var bus = new EventBus(config =>
    {
        config.UseRetryPolicy(new RetryPolicy { MaxRetries = 3, Backoff = TimeSpan.FromMilliseconds(50) });
        config.UseDeadLetterQueue(new InMemoryDeadLetterQueue());
        config.UseMetricsCollector(new InMemoryMetricsCollector());
    });

    bus.Subscribe<OrderPlacedEvent>(async (evt, ctx) =>
    {
        if (evt.Attempt == 1) throw new TransientException("Simulated glitch");
        // Process successfully on retry
    });

    var metricsCollector = bus.GetMetricsCollector();
    await bus.PublishAsync(new OrderPlacedEvent { Attempt = 1 });

    await Task.Delay(500); // Allow retries and processing

    Assert.That(metricsCollector.GetCounter("events.processed"), Is.EqualTo(1));
    Assert.That(metricsCollector.GetCounter("events.retried"), Is.EqualTo(1));
    Assert.That(bus.GetDeadLetterQueue().Count, Is.EqualTo(0));
}
```

```csharp
// Example 2: Batch publishing with error isolation and priority ordering
[Test]
public async Task BatchWithPriorities_IsolatesFailures()
{
    var deadLetter = new InMemoryDeadLetterQueue();
    var bus = new EventBus(config =>
    {
        config.UseBatchPublisher(new BatchOptions { MaxBatchSize = 5, FlushInterval = TimeSpan.FromSeconds(1) });
        config.UsePriorityOrdering();
        config.UseDeadLetterQueue(deadLetter);
    });

    var processed = new ConcurrentBag<string>();
    bus.Subscribe<PriorityEvent>(async (evt, ctx) =>
    {
        if (evt.Payload == "fail") throw new InvalidOperationException("Poison event");
        processed.Add(evt.Payload);
    });

    var batchPublisher = bus.GetBatchPublisher();
    batchPublisher.Add(new PriorityEvent { Priority = 3, Payload = "low" });
    batchPublisher.Add(new PriorityEvent { Priority = 1, Payload = "fail" });
    batchPublisher.Add(new PriorityEvent { Priority = 2, Payload = "high" });

    await batchPublisher.FlushAsync();
    await Task.Delay(300);

    Assert.That(processed, Is.EquivalentTo(new[] { "high", "low" })); // Priority order, fail excluded
    Assert.That(deadLetter.Count, Is.EqualTo(1));
    Assert.That(deadLetter.First().Payload, Is.EqualTo("fail"));
}
```

## Notes

- **Thread safety:** Tests involving parallel publishing and concurrent metrics recording (`EventBus_WithParallelPublishing_HandlesMultipleThreads`, `MetricsCollector_ThreadSafe_WithConcurrentRecording`) assume that the underlying bus and collector implementations use appropriate synchronization. Failures in these tests often indicate missing locks or non-atomic counter updates.
- **Timing sensitivity:** Several tests rely on asynchronous delays to wait for retries, batch flushes, or circuit breaker cooldowns. In slow CI environments, these delays may need tuning to avoid false negatives due to premature assertions.
- **Dead-letter queue semantics:** The dead-letter queue is expected to preserve the original event and exception context. Tests that inspect the dead-letter queue assume the implementation stores both the event payload and the failure metadata immutably.
- **Middleware ordering:** Middleware pipeline tests depend on deterministic registration order. If middleware components are added via reflection or convention-based discovery, the test must guarantee a stable order.
- **Batch error isolation:** The batch publisher’s error isolation behavior must not allow a poisoned event to prevent flushing of the remaining healthy events. Tests verify that the batch processor continues iterating after catching a handler exception.
- **Circuit breaker state:** Circuit breaker tests assume a closed-to-open transition after a fixed failure threshold. Shared circuit state across tests must be reset between runs to avoid cross-test contamination.
- **Priority inversion:** Priority ordering tests assume that higher-priority events (lower numeric value) are dequeued first. If the implementation uses a stable sort, events with equal priority should maintain insertion order.
