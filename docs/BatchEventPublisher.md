# BatchEventPublisher

The `BatchEventPublisher` type buffers incoming events and periodically flushes them to an underlying event sink. It provides configurable flush handlers, asynchronous addition APIs, and detailed statistics about each batch operation.

## API

### BatchEventPublisher()
Initializes a new instance of the publisher with default buffer settings. No parameters are required. Throws `ObjectDisposedException` if the underlying resources have already been released.

### SetFlushHandler(Action<BatchPublisherStats> handler)
Registers a synchronous callback that is invoked each time the internal buffer is flushed.  
- **handler**: The method to call with the flush statistics; must not be `null`.  
- **Throws**: `ArgumentNullException` if `handler` is `null`.

### SetFlushHandlerWithResult(Func<BatchPublisherStats, Task<bool>> handler)
Registers an asynchronous callback that is invoked each time the internal buffer is flushed. The handler can influence further processing by returning a `Task<bool>` where `true` indicates success and `false` indicates a need to retry or abort.  
- **handler**: The async method to call with the flush statistics; must not be `null`.  
- **Throws**: `ArgumentNullException` if `handler` is `null`.

### AddEventAsync()
Attempts to enqueue a single event for later transmission.  
- **Return Value**: `Task<bool>` – `true` if the event was successfully added to the buffer, `false` if the buffer is at capacity or the operation failed.  
- **Throws**:  
  - `InvalidOperationException` if the publisher has not been initialized with a flush handler.  
  - `ObjectDisposedException` if the publisher has been disposed.

### AddEventsAsync()
Attempts to enqueue a batch of events for later transmission.  
- **Return Value**: `Task` – completes when the operation finishes.  
- **Throws**: Same exceptions as `AddEventAsync`.

### FlushAsync()
Forces an immediate flush of all buffered events to the sink.  
- **Return Value**: `Task` – completes when the flush operation finishes.  
- **Throws**:  
  - `InvalidOperationException` if no flush handler is configured.  
  - `ObjectDisposedException` if the publisher has been disposed.

### GetBufferSize()
Retrieves the maximum number of events the internal buffer can hold before automatic flushing occurs.  
- **Return Value**: `int` – the buffer capacity.

### GetStats()
Returns statistical information about the publisher’s lifetime operation.  
- **Return Value**: `BatchPublisherStats` – an object containing counts of events processed, flushed, and any errors encountered.

### BufferedEventCount
Gets the current number of events stored in the buffer awaiting transmission.  
- **Return Value**: `int`.

### BufferedEventSize
Gets the approximate total size in bytes of all events currently buffered.  
- **Return Value**: `int`.

### LastFlushTime
Gets the UTC timestamp of the most recent successful flush operation.  
- **Return Value**: `DateTime`.

### EventId
Gets the identifier of the event that was processed in the last flush batch, if available.  
- **Return Value**: `string?` – `null` when no batch has been flushed or the identifier is not applicable.

### EventType
Gets the type name of the event that was processed in the last flush batch, if available.  
- **Return Value**: `string?` – `null` when no batch has been flushed or the type is not applicable.

### Success
Indicates whether the most recent flush batch completed without errors.  
- **Return Value**: `bool`.

### ErrorMessage
Contains a descriptive message if the most recent flush batch encountered an error.  
- **Return Value**: `string?` – `null` when `Success` is `true`.

### Exception
Holds the exception thrown during the most recent flush batch, if any.  
- **Return Value**: `Exception?` – `null` when `Success` is `true`.

### BatchId
Gets the identifier assigned to the most recent flush batch.  
- **Return Value**: `string?` – `null` when no batch has been flushed.

### SucceededCount
Gets the number of events that were successfully processed in the most recent flush batch.  
- **Return Value**: `int`.

### FailedCount
Gets the number of events that failed during the most recent flush batch.  
- **Return Value**: `int`.

### EventResults
Provides a read‑only list of per‑item results for the most recent flush batch, detailing success or failure for each event.  
- **Return Value**: `List<EventBatchItemResult>` – empty list when no batch has been flushed.

## Usage

```csharp
using System;
using System.Threading.Tasks;
using DotNetEventBus; // namespace containing BatchEventPublisher

class Program
{
    static async Task Main()
    {
        var publisher = new BatchEventPublisher();

        // Configure a simple flush handler that writes stats to console
        publisher.SetFlushHandler(stats =>
        {
            Console.WriteLine($"Flushed {stats.SucceededCount} events, {stats.FailedCount} failed.");
        });

        // Add events asynchronously
        await publisher.AddEventAsync(); // returns true if buffered
        await publisher.AddEventsAsync();

        // Force a flush when needed
        await publisher.FlushAsync();

        // Inspect the outcome of the last flush
        if (publisher.Success)
        {
            Console.WriteLine($"Last batch succeeded: {publisher.SucceededCount} events.");
        }
        else
        {
            Console.WriteLine($"Last batch failed: {publisher.ErrorMessage}");
        }
    }
}
```

```csharp
using System;
using System.Threading.Tasks;
using DotNetEventBus;

class Service
{
    private readonly BatchEventPublisher _publisher;

    public Service()
    {
        _publisher = new BatchEventPublisher();

        // Use an async flush handler that can signal retry needs
        _publisher.SetFlushHandlerWithResult(async stats =>
        {
            try
            {
                await SendToExternalSystemAsync(stats);
                return true; // indicate success
            }
            catch (Exception ex)
            {
                // Log and indicate failure; the publisher may retry based on its policy
                Console.Error.WriteLine($"Flush failed: {ex}");
                return false;
            }
        });
    }

    public async Task PublishAsync(object @event)
    {
        // Attempt to buffer the event; if the buffer is full we could apply back‑pressure
        bool added = await _publisher.AddEventAsync();
        if (!added)
        {
            // Handle buffer full scenario (e.g., wait, drop, or apply back‑pressure)
            await Task.Delay(10);
            await PublishAsync(@event); // retry simplistically
        }
    }

    private Task SendToExternalSystemAsync(BatchPublisherStats stats)
    {
        // Placeholder for actual transmission logic
        return Task.CompletedTask;
    }
}
```

## Notes

- The publisher is **not** thread‑safe by default; concurrent calls to `AddEventAsync`/`AddEventsAsync` from multiple threads may lead to race conditions. External synchronization is required if shared across threads.  
- Flush handlers are invoked on the publisher’s internal flushing thread; they should avoid performing long‑running or blocking work to prevent delaying subsequent flushes.  
- If no flush handler is set via either `SetFlushHandler` or `SetFlushHandlerWithResult`, calls to `FlushAsync` will throw `InvalidOperationException`.  
- The `AddEventAsync` and `AddEventsAsync` methods return `false` or complete unsuccessfully only when the internal buffer has reached its capacity as reported by `GetBufferSize`; they do not block waiting for space.  
- Property values such as `EventId`, `EventType`, `Success`, `ErrorMessage`, `Exception`, `BatchId`, `SucceededCount`, `FailedCount`, and `EventResults` reflect the state of the **most recent** flush operation and are updated atomically when a flush completes. Reading these properties concurrently with a flush may yield stale or intermediate values.  
- The publisher does not expose a `Dispose` method in the documented surface; consumers should rely on the lifetime management implied by the containing application.  
- Exception information captured in the `Exception` property is cleared only after a subsequent successful flush; reading it after a failed flush will retain the exception until the next flush attempt.  
- The `EventResults` list is a snapshot; modifications to the returned list do not affect the publisher’s internal state.
