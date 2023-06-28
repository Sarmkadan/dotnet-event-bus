# BatchEventPublisherTests

The `BatchEventPublisherTests` class serves as the comprehensive test suite for validating the behavior, reliability, and edge-case handling of the `BatchEventPublisher` component within the `dotnet-event-bus` library. It verifies core functionalities including event batching logic, flush handler invocation, error isolation strategies, and constructor argument validation. By covering scenarios ranging from valid envelope processing to null reference exceptions and batch size constraints, this suite ensures the publisher maintains data integrity and operational stability under various conditions.

## API

### Constructors

#### `public BatchEventPublisherTests()`
Initializes a new instance of the `BatchEventPublisherTests` class. This constructor prepares the test context required to execute the various validation scenarios defined in the class.

### Test Methods

#### `public async Task AddEventAsync_WithValidEnvelope_ShouldAddToBatch`
Validates that an event wrapped in a valid envelope is successfully added to the current batch without triggering an immediate flush or returning an error.
*   **Parameters**: None (test context is internal).
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws an assertion failure if the event is not added to the batch.

#### `public async Task AddEventAsync_WithInvalidEnvelope_ShouldReturnFalse`
Verifies that attempting to add an event with an invalid envelope structure results in the method returning `false` rather than adding the event or throwing an exception.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws an assertion failure if the return value is not `false`.

#### `public async Task AddEventAsync_WithNullEnvelope_ShouldThrowArgumentNullException`
Ensures that passing a `null` envelope to the add operation strictly throws an `ArgumentNullException`, preventing silent failures or undefined behavior.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Expects an `ArgumentNullException` to be thrown; fails the test if no exception or a different exception type occurs.

#### `public async Task SetFlushHandler_ShouldBeInvokedWhenBatchIsFull`
Confirms that the configured flush handler is automatically invoked once the internal batch reaches its defined capacity limit.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws an assertion failure if the handler is not invoked upon reaching the batch limit.

#### `public async Task SetFlushHandler_WithNullHandler_ShouldThrowArgumentNullException`
Validates that attempting to assign a `null` flush handler results in an immediate `ArgumentNullException`.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Expects an `ArgumentNullException`; fails the test if the assignment succeeds or throws a different exception.

#### `public async Task SetFlushHandlerWithResult_ShouldInvokePerEventHandler`
Tests the variant of the flush handler that returns a result, ensuring it is invoked correctly for each event handler registered within the batch.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws an assertion failure if the handler invocation count or context does not match expectations.

#### `public async Task SetFlushHandlerWithResult_WithErrorIsolation_ShouldProcessAllEventsEvenWithFailures`
Verifies the error isolation mechanism: if one event handler within the batch fails during flushing, the system continues to process the remaining events rather than aborting the entire batch.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws an assertion failure if subsequent events are skipped due to a prior handler failure.

#### `public async Task SetFlushHandlerWithResult_WithNullHandler_ShouldThrowArgumentNullException`
Ensures that assigning a `null` handler to the result-bearing flush configuration throws an `ArgumentNullException`.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Expects an `ArgumentNullException`; fails the test otherwise.

#### `public async Task AddEventAsync_WithMultipleBatches_ShouldFlushEachBatch`
Simulates a high-volume scenario where events exceed a single batch capacity, verifying that the system correctly flushes multiple sequential batches.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws an assertion failure if the number of flush invocations does not match the expected number of batches.

#### `public void Constructor_WithInvalidBatchSize_ShouldThrowArgumentException`
Validates that instantiating the publisher with a non-positive or otherwise invalid batch size throws an `ArgumentException`.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Exceptions**: Expects an `ArgumentException`; fails the test if instantiation succeeds or throws a different exception.

#### `public void Constructor_WithNullLogger_ShouldThrowArgumentNullException`
Ensures that providing a `null` logger dependency during construction results in an `ArgumentNullException`.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Exceptions**: Expects an `ArgumentNullException`; fails the test otherwise.

#### `public async Task FlushAsync_WithoutHandler_ShouldNotThrow`
Confirms that calling `FlushAsync` explicitly when no flush handler has been configured completes gracefully without throwing exceptions.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Fails the test if any exception is thrown during the flush operation.

## Usage

The following examples demonstrate how the test cases validate specific behaviors of the `BatchEventPublisher`.

### Example 1: Validating Batch Capacity and Flush Invocation
This scenario illustrates the test logic used to verify that the flush handler triggers exactly when the batch limit is reached.

```csharp
[Test]
public async Task AddEventAsync_WithMultipleBatches_ShouldFlushEachBatch()
{
    // Arrange
    var batchSize = 5;
    var flushCount = 0;
    var publisher = new BatchEventPublisher(
        batchSize, 
        _ => flushCount++, 
        NullLogger.Instance
    );

    // Act: Add 12 events (should trigger 2 full flushes, 3rd pending)
    for (int i = 0; i < 12; i++)
    {
        await publisher.AddEventAsync(CreateValidEnvelope(i));
    }

    // Assert
    // Two full batches of 5 should have flushed. The remaining 2 stay in buffer.
    Assert.AreEqual(2, flushCount, "Expected exactly two flushes for 12 events with batch size 5.");
}
```

### Example 2: Verifying Error Isolation During Flush
This example demonstrates the test ensuring that a failure in one handler does not prevent other events in the same batch from being processed.

```csharp
[Test]
public async Task SetFlushHandlerWithResult_WithErrorIsolation_ShouldProcessAllEventsEvenWithFailures()
{
    // Arrange
    var processedIds = new ConcurrentBag<int>();
    var publisher = new BatchEventPublisher(
        3,
        async (events) => 
        {
            foreach (var evt in events)
            {
                if (evt.Id == 2) throw new InvalidOperationException("Simulated failure");
                processedIds.Add(evt.Id);
            }
        },
        NullLogger.Instance,
        enableErrorIsolation: true
    );

    await publisher.AddEventAsync(CreateEnvelope(1));
    await publisher.AddEventAsync(CreateEnvelope(2)); // Will fail
    await publisher.AddEventAsync(CreateEnvelope(3));
    await publisher.FlushAsync();

    // Assert
    // Even though ID 2 failed, 1 and 3 should still be processed.
    Assert.IsTrue(processedIds.Contains(1));
    Assert.IsTrue(processedIds.Contains(3));
    Assert.IsFalse(processedIds.Contains(2));
}
```

## Notes

*   **Thread Safety**: While the tests utilize `async`/`await` patterns extensively, the specific test methods themselves are generally executed sequentially by the test runner. However, the `SetFlushHandlerWithResult_WithErrorIsolation_ShouldProcessAllEventsEvenWithFailures` test implies that the underlying implementation must handle concurrent execution of handlers or at least guarantee that exceptions in one iteration do not halt the loop, suggesting a robust internal iteration strategy.
*   **Null Handling**: The API strictly enforces non-null constraints for critical dependencies (`ILogger`, flush handlers) and data payloads (`Envelope`). Developers should expect `ArgumentNullException` immediately upon violation of these contracts, both during construction and runtime operations.
*   **Batch Boundaries**: The flushing behavior is strictly tied to the `BatchSize` parameter. Tests confirm that partial batches remain buffered until explicitly flushed via `FlushAsync` or until the batch fills up. Calling `FlushAsync` on an empty or partially filled batch without a handler is a safe no-op.
*   **Error Isolation**: The presence of specific tests for error isolation indicates that the default behavior might be to fail fast unless explicitly configured otherwise. When error isolation is enabled, individual event processing failures are logged but do not propagate to cancel the processing of sibling events in the same batch.
