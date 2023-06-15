# IDeadLetterRepository

Interface for querying and managing dead-letter entries in the event bus system. It provides read-only access to persisted failure records—events that could not be delivered or processed successfully—along with administrative operations such as counting by status and archiving stale entries. Implementations are expected to be asynchronous and backed by a durable store.

## API

### GetPendingAsync
```csharp
Task<IEnumerable<DeadLetterEntry>> GetPendingAsync();
```
Returns all dead-letter entries that are still in a pending state (not yet resolved, retried, or archived). Useful for dashboards and retry loops.

**Parameters:** none.

**Returns:** a collection of `DeadLetterEntry` objects with pending status.

**Throws:** may throw if the underlying store is unavailable or the query times out.

---

### GetByHandlerAsync
```csharp
Task<IEnumerable<DeadLetterEntry>> GetByHandlerAsync(string handlerName);
```
Filters dead-letter entries by the name of the handler that failed to process the event.

**Parameters:**
- `handlerName` — the exact handler name to match (case sensitivity depends on the implementation).

**Returns:** matching entries, or an empty collection if none exist.

**Throws:** `ArgumentNullException` when `handlerName` is null; storage-level exceptions on connectivity failure.

---

### GetByEventTypeAsync
```csharp
Task<IEnumerable<DeadLetterEntry>> GetByEventTypeAsync(string eventType);
```
Retrieves dead-letter entries for a specific event type (typically the fully qualified CLR type name or a short alias).

**Parameters:**
- `eventType` — the event type identifier.

**Returns:** entries that failed for that event type.

**Throws:** `ArgumentNullException` when `eventType` is null; storage-level exceptions on connectivity failure.

---

### GetByStatusAsync
```csharp
Task<IEnumerable<DeadLetterEntry>> GetByStatusAsync(DeadLetterStatus status);
```
Returns entries matching the given dead-letter status (e.g., Pending, Retrying, Archived).

**Parameters:**
- `status` — a `DeadLetterStatus` enum value.

**Returns:** entries in the requested status.

**Throws:** storage-level exceptions on connectivity failure.

---

### GetByTimeRangeAsync
```csharp
Task<IEnumerable<DeadLetterEntry>> GetByTimeRangeAsync(DateTime from, DateTime to);
```
Queries dead-letter entries whose occurrence timestamp falls within the specified inclusive range.

**Parameters:**
- `from` — start of the time window (inclusive).
- `to` — end of the time window (inclusive).

**Returns:** entries ordered by timestamp (typically ascending).

**Throws:** `ArgumentException` when `from` is later than `to`; storage-level exceptions on connectivity failure.

---

### CountByStatusAsync
```csharp
Task<int> CountByStatusAsync(DeadLetterStatus status);
```
Counts the total number of dead-letter entries in a given status.

**Parameters:**
- `status` — the status to count.

**Returns:** the count as a non-negative integer.

**Throws:** storage-level exceptions on connectivity failure.

---

### GetCountsByEventTypeAsync
```csharp
Task<Dictionary<string, int>> GetCountsByEventTypeAsync();
```
Returns a breakdown of dead-letter entry counts grouped by event type. The dictionary keys are event type identifiers; values are the number of entries for each type.

**Parameters:** none.

**Returns:** a dictionary where each entry maps an event type to its count. May be empty if no dead-letter entries exist.

**Throws:** storage-level exceptions on connectivity failure.

---

### GetCountsByHandlerAsync
```csharp
Task<Dictionary<string, int>> GetCountsByHandlerAsync();
```
Returns a breakdown of dead-letter entry counts grouped by handler name. The dictionary keys are handler names; values are the number of entries each handler has failed to process.

**Parameters:** none.

**Returns:** a dictionary mapping handler names to counts. May be empty.

**Throws:** storage-level exceptions on connectivity failure.

---

### ArchiveOldEntriesAsync
```csharp
Task<int> ArchiveOldEntriesAsync(DateTime cutoff);
```
Marks all dead-letter entries older than the specified cutoff as archived (or removes them, depending on the implementation). This is a bulk administrative operation.

**Parameters:**
- `cutoff` — entries with a timestamp before this point are targeted for archival.

**Returns:** the number of entries affected.

**Throws:** storage-level exceptions on connectivity failure.

## Usage

### Example 1: Retry pending dead-letter entries
```csharp
public async Task RetryPendingAsync(IDeadLetterRepository repository, IEventBus eventBus)
{
    var pending = await repository.GetPendingAsync();
    foreach (var entry in pending)
    {
        try
        {
            await eventBus.RetryAsync(entry);
        }
        catch (Exception ex)
        {
            // Log and leave in dead-letter for manual intervention
            Console.WriteLine($"Retry failed for {entry.Id}: {ex.Message}");
        }
    }
}
```

### Example 2: Dashboard summary of dead-letter state
```csharp
public async Task<DeadLetterSummary> GetSummaryAsync(IDeadLetterRepository repository)
{
    var pendingCount = await repository.CountByStatusAsync(DeadLetterStatus.Pending);
    var countsByType = await repository.GetCountsByEventTypeAsync();
    var countsByHandler = await repository.GetCountsByHandlerAsync();

    return new DeadLetterSummary
    {
        TotalPending = pendingCount,
        BreakdownByEventType = countsByType,
        BreakdownByHandler = countsByHandler
    };
}
```

## Notes

- All methods return `Task`, indicating they are asynchronous I/O-bound operations. Callers should `await` them to avoid blocking threads.
- The `GetByTimeRangeAsync` method expects `from <= to`; implementations may throw `ArgumentException` if this constraint is violated.
- `ArchiveOldEntriesAsync` is a mutating operation. In multi-threaded environments, concurrent calls with overlapping cutoffs may result in the same entries being counted multiple times if the implementation does not use atomic status transitions.
- The dictionary-returning methods (`GetCountsByEventTypeAsync`, `GetCountsByHandlerAsync`) aggregate across all statuses unless the implementation explicitly scopes them (check the concrete documentation).
- Thread safety is implementation-defined. The interface itself does not guarantee thread-safe access; consumers performing parallel queries or mixed read/write operations should synchronize externally if the backing store does not support concurrent access natively.
- Parameter validation (e.g., null checks on `handlerName` and `eventType`) is typically performed by the implementation. Callers should not rely on every implementation throwing immediately on invalid input—defensive null-guarding is recommended.
