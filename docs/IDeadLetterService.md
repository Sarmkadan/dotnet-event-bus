# IDeadLetterService
The `IDeadLetterService` interface provides a set of methods and properties for managing and processing dead letter entries in an event bus system. It allows for retrieving pending entries, reprocessing failed entries, and tracking statistics on entry processing outcomes. This interface is designed to be used in conjunction with an event bus to handle messages that cannot be processed by their intended handlers.

## API
* `TotalEntries`: Gets the total number of dead letter entries.
* `PendingEntries`: Gets the number of pending dead letter entries.
* `ReviewedNotProcessedEntries`: Gets the number of reviewed but not processed dead letter entries.
* `ReprocessedEntries`: Gets the number of reprocessed dead letter entries.
* `ReprocessFailedEntries`: Gets the number of failed reprocessing attempts for dead letter entries.
* `ArchivedEntries`: Gets the number of archived dead letter entries.
* `EntriesByEventType`: Gets a dictionary of dead letter entries grouped by event type.
* `EntriesByHandler`: Gets a dictionary of dead letter entries grouped by handler.
* `SucceededCount`: Gets the number of successfully processed dead letter entries.
* `FailedCount`: Gets the number of failed dead letter entries.
* `FailedEntryIds`: Gets a list of IDs for failed dead letter entries.
* `DeadLetterService`: Gets the underlying dead letter service instance.
* `GetPendingEntriesAsync`: Retrieves a list of pending dead letter entries asynchronously.
	+ Parameters: None
	+ Return value: An `IEnumerable<DeadLetterEntry>` containing the pending entries.
	+ Throws: Exceptions may be thrown if there is an issue retrieving the entries.
* `GetEntriesByEventTypeAsync`: Retrieves a list of dead letter entries grouped by event type asynchronously.
	+ Parameters: None
	+ Return value: An `IEnumerable<DeadLetterEntry>` containing the entries grouped by event type.
	+ Throws: Exceptions may be thrown if there is an issue retrieving the entries.
* `GetEntriesByHandlerAsync`: Retrieves a list of dead letter entries grouped by handler asynchronously.
	+ Parameters: None
	+ Return value: An `IEnumerable<DeadLetterEntry>` containing the entries grouped by handler.
	+ Throws: Exceptions may be thrown if there is an issue retrieving the entries.
* `ReprocessEntryAsync`: Attempts to reprocess a dead letter entry asynchronously.
	+ Parameters: The ID of the entry to reprocess.
	+ Return value: A `bool` indicating whether the reprocessing was successful.
	+ Throws: Exceptions may be thrown if there is an issue reprocessing the entry.
* `ReprocessByEventTypeAsync`: Attempts to reprocess dead letter entries of a specific event type asynchronously.
	+ Parameters: The event type to reprocess.
	+ Return value: A `BatchReprocessResult` containing the outcome of the reprocessing attempt.
	+ Throws: Exceptions may be thrown if there is an issue reprocessing the entries.
* `MarkAsReviewedAsync`: Marks a dead letter entry as reviewed asynchronously.
	+ Parameters: The ID of the entry to mark as reviewed.
	+ Return value: None
	+ Throws: Exceptions may be thrown if there is an issue marking the entry as reviewed.
* `ArchiveOldEntriesAsync`: Archives old dead letter entries asynchronously.
	+ Parameters: None
	+ Return value: The number of archived entries.
	+ Throws: Exceptions may be thrown if there is an issue archiving the entries.
* `GetStatisticsAsync`: Retrieves statistics on dead letter entry processing outcomes asynchronously.
	+ Parameters: None
	+ Return value: A `DeadLetterStatistics` object containing the statistics.
	+ Throws: Exceptions may be thrown if there is an issue retrieving the statistics.

## Usage
```csharp
// Example 1: Retrieving pending dead letter entries
var deadLetterService = new DeadLetterService();
var pendingEntries = await deadLetterService.GetPendingEntriesAsync();
foreach (var entry in pendingEntries)
{
    Console.WriteLine($"Entry ID: {entry.Id}, Event Type: {entry.EventType}");
}

// Example 2: Reprocessing a failed dead letter entry
var failedEntryId = "failed-entry-id";
var reprocessResult = await deadLetterService.ReprocessEntryAsync(failedEntryId);
if (reprocessResult)
{
    Console.WriteLine($"Reprocessing of entry {failedEntryId} was successful.");
}
else
{
    Console.WriteLine($"Reprocessing of entry {failedEntryId} failed.");
}
```

## Notes
* The `IDeadLetterService` interface is designed to be thread-safe, allowing for concurrent access and modification of dead letter entries.
* When using the `ReprocessEntryAsync` and `ReprocessByEventTypeAsync` methods, be aware that reprocessing may fail if the underlying issue that caused the entry to fail initially has not been resolved.
* The `ArchiveOldEntriesAsync` method may throw exceptions if there are issues with the underlying storage or if the archiving process encounters errors.
* The `GetStatisticsAsync` method may return incomplete or inaccurate statistics if there are issues with the underlying data storage or retrieval mechanisms.
