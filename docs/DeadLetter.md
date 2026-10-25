# Dead Letter Flow

This document describes how failed events move through the dead letter queue (DLQ) of DotnetEventBus. It covers three components:

- `DeadLetterService` (`src/DotnetEventBus/Services/DeadLetterService.cs`): the public operations for inspecting, replaying, reviewing, archiving, and purging entries.
- `DeadLetterProcessor` (`src/DotnetEventBus/Workers/DeadLetterProcessor.cs`): a standalone background worker with its own in-memory queue. It is **not** connected to the flow below. See [DeadLetterProcessor](#deadletterprocessor-standalone-worker).
- `InMemoryDeadLetterRepository` (`src/DotnetEventBus/Repositories/DeadLetterRepository.cs`): the default storage for `DeadLetterEntry` records.

The entry model is `DeadLetterEntry` (`src/DotnetEventBus/Models/DeadLetterEntry.cs`). Its status values are `DeadLetterStatus`: `Pending`, `ReviewedNotProcessed`, `Reprocessed`, `ReprocessFailed`, and `Archived`.

## Components and wiring

`AddEventBus` registers the DLQ pieces as singletons:

| Registration | Implementation | Notes |
|---|---|---|
| `IDeadLetterRepository` | `InMemoryDeadLetterRepository(options.MaxDeadLetterEntries)` | Capacity comes from `EventBusOptions.MaxDeadLetterEntries` (default `1000`). |
| `IDeadLetterService` | `DeadLetterService(repository, IServiceProvider, logger)` | Resolves `IEventBus` lazily on first use. |
| `IEventBus` | `EventBus(...)` | Receives the same repository and service singletons. |

The lazy `IEventBus` lookup is required. `EventBus` and `DeadLetterService` depend on each other, and resolving `IEventBus` eagerly inside the `DeadLetterService` constructor would re-enter the singleton that is still being built. The `DeadLetterService(repository, eventBus, logger)` overload is for direct construction; it does not perform lazy resolution.

When you use `EventBus` directly, pass the same `IDeadLetterRepository` instance to both the bus and the service. Otherwise the bus writes entries that the service cannot see.

## Entry lifecycle

```
            ┌────────────── Pending ──────────────┐
            │                                      │
   (created on failure)              (ReprocessEntryAsync succeeds)
            │                                      ▼
            │                              Reprocessed
            │
            ├── MarkAsReviewedAsync ──▶ ReviewedNotProcessed
            │
            ├── ReprocessEntryAsync fails ──▶ ReprocessFailed
            │
            └── ArchiveOldEntriesAsync (older than retention) ──▶ Archived
```

Status changes are made by `DeadLetterEntry` methods and written back with `IDeadLetterRepository.UpdateAsync`:

| Method | Resulting status | `StatusReason` |
|---|---|---|
| `MarkAsReviewed(reason)` | `ReviewedNotProcessed` | The supplied reason (may be `null`). |
| `MarkAsReprocessed()` | `Reprocessed` | `"Successfully reprocessed"` |
| `MarkAsReprocessFailed(reason)` | `ReprocessFailed` | The supplied reason. |
| `ArchiveOldEntriesAsync` (repository) | `Archived` | `"Auto-archived due to retention period"` |

`DeadLetterEntry.Validate()` (in `DeadLetterEntryValidation.cs`) requires a non-empty `StatusReason` for `ReviewedNotProcessed`, `ReprocessFailed`, and `Archived`.

## How entries are created

There are three write paths.

### 1. Handler failure during publish

When a subscription's handler fails after `MaxRetryAttempts`, `EventBus` creates a `DeadLetterEntry` and calls `IDeadLetterRepository.AddAsync` directly. This happens only when both conditions are true:

- `EventBusOptions.EnableDeadLetterQueue` is `true`, and
- the subscription has `SendToDeadLetterOnFailure` set.

The entry's `FailedHandlerName` is the subscription's handler name, and `MaxRetryAttempts` is recorded from the options.

### 2. No handlers registered

If `PublishAsync` finds no handlers, `ThrowOnNoHandlers` controls whether an exception is thrown. Otherwise, the entry is recorded through `DeadLetterService.AddDeadLetterEntryAsync` only when `EnableDeadLetterQueue` and `DeadLetterOnNoHandlers` are both `true`. The handler name is `"NoHandlersRegistered"`.

### 3. Raw distributed payload failures

`EventBus.ProcessRawDistributedEventAsync` writes to the DLQ through `AddDeadLetterEntryAsync` in three cases. Each one returns a failed `PublishResult`:

- the event type cannot be resolved with `Type.GetType`;
- the formatter returns `null`;
- deserialization or the subsequent publish throws.

The handler name for these entries is the internal constant `ProcessRawDistributedEventOperation`. Unlike the first two paths, these writes do not check `EnableDeadLetterQueue`.

You can also call `IDeadLetterService.AddDeadLetterEntryAsync` yourself for payloads received from external sources. The entry is created as `Pending` with `Scope = Distributed`, zero retry attempts, and the handler name `"DeserializationFailed"` unless you pass another name.

## Operations on `IDeadLetterService`

| Method | Behavior |
|---|---|
| `GetPendingEntriesAsync` | Returns all entries with status `Pending`. |
| `GetEntriesByEventTypeAsync(eventType)` | Returns entries whose `Message.EventType` matches. Throws `ArgumentException` for empty input. |
| `GetEntriesByHandlerAsync(handlerName)` | Returns entries whose `FailedHandlerName` matches. Throws `ArgumentException` for empty input. |
| `ReprocessEntryAsync(entryId)` | Republishes one entry (see [Reprocessing](#reprocessing)). Returns `false` if the entry does not exist. |
| `ReprocessByEventTypeAsync(eventType, maxEntries)` | Reprocesses `Pending` entries of one event type, in repository order, up to `maxEntries`. |
| `ReplayAsync(predicate, maxEntries)` | Reprocesses `Pending` entries matching a predicate. Throws `ArgumentNullException` for a null predicate. |
| `MarkAsReviewedAsync(entryId, reason)` | Sets `ReviewedNotProcessed`. Throws `InvalidOperationException` if the entry does not exist. |
| `ArchiveOldEntriesAsync(retentionPeriod)` | Archives entries older than the retention period. Throws `ArgumentException` if the period is not positive. Returns the count archived. |
| `GetStatisticsAsync` | Returns counts per status, plus `EntriesByEventType` and `EntriesByHandler`. |
| `PurgeAsync` | Removes all entries from the repository. |

### Reprocessing

`ReprocessEntryAsync` follows these steps:

1. Look up the entry by ID. If missing, log a warning and return `false`.
2. If no event bus is available, mark the entry `ReprocessFailed` with the reason `"Event bus not available"`, save it, and return `false`.
3. Call `IEventBus.PublishAsync(entry.Message.Payload, type, entry.Message.CorrelationId)`, where `type` is `Type.GetType(entry.Message.EventType)` and falls back to `typeof(object)` if the name cannot be resolved.
4. On success, mark the entry `Reprocessed`. On a failed result, mark it `ReprocessFailed` with the failed-handler count. If the publish throws, mark it `ReprocessFailed` with the exception message.
5. Save the entry and return whether the publish succeeded.

Batch operations (`ReprocessByEventTypeAsync`, `ReplayAsync`) call `ReprocessEntryAsync` for each selected entry. Failures do not stop the batch. They are collected in `BatchReprocessResult.FailedEntryIds`, and `AllSucceeded` is `true` only when `FailedCount` is zero.

### Archiving and purging

`ArchiveOldEntriesAsync` changes the status of old entries to `Archived`. It does not delete them, so archived entries still count toward `TotalEntries` in statistics. An entry is archived if its `CreatedAtUtc` is earlier than `UtcNow - retentionPeriod` and its status is not already `Archived`. This includes entries that were already reprocessed or reviewed.

`PurgeAsync` clears the repository. It logs a warning but has no other safeguards.

### Statistics

`GetStatisticsAsync` counts entries for each status. `TotalEntries` is the sum of the five status counts. `EntriesByEventType` and `EntriesByHandler` are grouped by `Message.EventType` and `FailedHandlerName`.

## Storage and capacity (`InMemoryDeadLetterRepository`)

The repository is an in-memory ring buffer with a maximum of `maxEntries` entries (at least 1).

- When `AddAsync` is called at capacity, the oldest entry by insertion order is deleted to make room. Eviction ignores status, so a `Pending` entry can be evicted while older `Archived` entries are kept if they were inserted later.
- Inserts and evictions are serialized by a `SemaphoreSlim`. Reads use the base `InMemoryRepository` lock.
- Entries are not persisted. A restart empties the DLQ.

Query methods (`GetByStatusAsync`, `GetByHandlerAsync`, `GetByEventTypeAsync`, `GetByTimeRangeAsync`, count and grouping methods) load all entries and filter in memory. Keep `MaxDeadLetterEntries` modest.

## DeadLetterProcessor (standalone worker)

`DeadLetterProcessor` is a `BackgroundService` with its own `List<DeadLetterItem>`. It is not registered by `AddEventBus`, nothing in the library calls its `Enqueue` method, and it does not read `IDeadLetterRepository`. Entries created by the flow above never reach it.

Its behavior, as implemented:

- Every `processingInterval` (default 5 minutes), it takes up to `batchSize` (default 100) `Pending` items, increments `RetryCount`, and sets the status to `Retry`. Items that reach the retry branch also get `LastRetryAt`.
- The loop selects only `Pending` items, so an item in `Retry` is never picked up again. Each item therefore gets one attempt, and the five-attempt threshold that marks an item `Failed` is not reached by the current loop. Nothing sets `Successful`.
- The reprocessing step is a placeholder (`// Attempt reprocessing logic would go here`). The processor does not republish events.
- It waits 100 ms between items.

Its `DeadLetterStatus` enum (`Pending`, `Retry`, `Successful`, `Failed`) is a different type from `Models.DeadLetterStatus` with the same name. Use fully qualified names when both namespaces are in scope.

The list is not synchronized. `Enqueue`, `RemoveItem`, and the processing loop can run concurrently, so calls from multiple threads need external locking.

## Known behaviors and caveats

These describe current code. They are documented here and not changed.

- **Purge leaves stale eviction IDs.** `PurgeAsync` (`ClearAsync`) empties the store but not the repository's insertion-order queue. After a purge, eviction can dequeue IDs that no longer exist, so the store may briefly hold more than `MaxDeadLetterEntries` entries. The excess is bounded by the number of stale IDs. Deleting an entry directly through the base `DeleteAsync` leaves the same kind of stale ID.
- **Failed reprocessing is not retried by batches.** `ReprocessByEventTypeAsync` and `ReplayAsync` select only `Pending` entries. An entry that failed with `ReprocessFailed` can be retried only by calling `ReprocessEntryAsync` with its ID. `ReprocessEntryAsync` itself does not check status, so it will also reprocess `Reprocessed` and `Archived` entries when called by ID.
- **Type resolution.** `Type.GetType` needs assembly-qualified names. During replay, `ReprocessEntryAsync` falls back to `typeof(object)` when the name cannot be resolved, which is unlikely to match the original handlers. `ProcessRawDistributedEventAsync` instead records an unresolved type as a failed DLQ entry.
- **Inconsistent "not found" handling.** `ReprocessEntryAsync` returns `false` for an unknown ID, but `MarkAsReviewedAsync` throws `InvalidOperationException`.
- **Two write paths.** `EventBus` writes handler-failure entries directly to the repository, bypassing `DeadLetterService`. Those entries do not get the logging that `AddDeadLetterEntryAsync` performs.
- **`Services/DeadLetterQueue.cs`** is a separate type and is not part of this flow.
