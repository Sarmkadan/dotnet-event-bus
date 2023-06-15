# IEventMessageRepository

The `IEventMessageRepository` interface defines a contract for persisting and retrieving event messages in the `dotnet-event-bus` system. It provides query methods to locate messages by event type, time range, correlation identifier, or source, as well as a method to retrieve messages that have failed processing and a method to delete messages that are no longer needed. Implementations are expected to manage the underlying storage and ensure data consistency.

## API

### `GetByEventTypeAsync`
Retrieves all event messages that match the specified event type.

- **Parameters**  
  `eventType` – A string identifying the event type to filter by.
- **Returns**  
  `Task<IEnumerable<EventMessage>>` – A collection of messages with the given event type.
- **Throws**  
  `ArgumentNullException` if `eventType` is `null`.  
  `InvalidOperationException` if the underlying store is unavailable.

### `GetByTimeRangeAsync`
Retrieves all event messages whose timestamp falls within the specified time range.

- **Parameters**  
  `start` – The inclusive start of the time range.  
  `end` – The inclusive end of the time range.
- **Returns**  
  `Task<IEnumerable<EventMessage>>` – A collection of messages within the time range.
- **Throws**  
  `ArgumentOutOfRangeException` if `start` is later than `end`.  
  `InvalidOperationException` if the underlying store is unavailable.

### `GetByCorrelationIdAsync`
Retrieves all event messages that share the given correlation identifier.

- **Parameters**  
  `correlationId` – A string that groups related messages together.
- **Returns**  
  `Task<IEnumerable<EventMessage>>` – A collection of messages with the specified correlation ID.
- **Throws**  
  `ArgumentNullException` if `correlationId` is `null`.  
  `InvalidOperationException` if the underlying store is unavailable.

### `GetBySourceAsync`
Retrieves all event messages that originated from the specified source.

- **Parameters**  
  `source` – A string identifying the source system or component.
- **Returns**  
  `Task<IEnumerable<EventMessage>>` – A collection of messages from the given source.
- **Throws**  
  `ArgumentNullException` if `source` is `null`.  
  `InvalidOperationException` if the underlying store is unavailable.

### `GetFailedMessagesAsync`
Retrieves all event messages that have been marked as failed (e.g., processing errors, retry exhaustion).

- **Parameters**  
  None.
- **Returns**  
  `Task<IEnumerable<EventMessage>>` – A collection of failed messages.
- **Throws**  
  `InvalidOperationException` if the underlying store is unavailable.

### `DeleteOldMessagesAsync`
Deletes event messages that are older than the specified retention threshold.

- **Parameters**  
  `threshold` – A `DateTime` or `TimeSpan` indicating the cutoff age. Messages with a timestamp before this threshold are removed.
- **Returns**  
  `Task<int>` – The number of messages deleted.
- **Throws**  
  `ArgumentNullException` if `threshold` is `null` (when passed as a nullable type).  
  `InvalidOperationException` if the underlying store is unavailable.

## Usage

### Example 1: Querying messages by event type and processing them

```csharp
public async Task ProcessEventsByTypeAsync(IEventMessageRepository repository, string eventType)
{
    IEnumerable<EventMessage> messages = await repository.GetByEventTypeAsync(eventType);

    foreach (var message in messages)
    {
        // Process each message (e.g., deserialize, handle business logic)
        Console.WriteLine($"Processing message {message.Id} of type {eventType}");
    }
}
```

### Example 2: Cleaning up old messages and inspecting failures

```csharp
public async Task PerformMaintenanceAsync(IEventMessageRepository repository, TimeSpan retentionPeriod)
{
    // Delete messages older than the retention period
    int deletedCount = await repository.DeleteOldMessagesAsync(DateTime.UtcNow - retentionPeriod);
    Console.WriteLine($"Deleted {deletedCount} old messages.");

    // Retrieve any messages that failed processing
    IEnumerable<EventMessage> failedMessages = await repository.GetFailedMessagesAsync();
    foreach (var failed in failedMessages)
    {
        // Log or reprocess failed messages
        Console.WriteLine($"Failed message {failed.Id} from source {failed.Source}");
    }
}
```

## Notes

- **Empty results** – All query methods return an empty `IEnumerable<EventMessage>` when no messages match the criteria; they never return `null`.
- **Null parameters** – Methods that accept string or DateTime parameters throw `ArgumentNullException` if a required parameter is `null`. Callers should validate inputs before invocation.
- **Thread safety** – The interface does not mandate thread safety. Implementations should document their own concurrency guarantees. In typical usage, callers should avoid concurrent writes and reads on the same repository instance without external synchronization.
- **Asynchronous execution** – All methods are asynchronous and should be awaited. They may perform I/O operations (e.g., database queries, file reads) and can throw `InvalidOperationException` if the underlying store is temporarily unavailable.
- **Deletion semantics** – `DeleteOldMessagesAsync` performs a hard delete. Deleted messages cannot be recovered through any of the query methods. The exact definition of “old” depends on the implementation’s timestamp field (typically `CreatedAt` or `Timestamp`).
