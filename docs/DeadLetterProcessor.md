# DeadLetterProcessor

A utility for managing and processing dead-lettered events in a message bus system. It tracks failed event deliveries, supports retry logic, and provides statistics on dead-letter queue states.

## API

### `public DeadLetterProcessor`

Initializes a new instance of the `DeadLetterProcessor` with a unique identifier. The processor maintains an internal collection of dead-letter items and tracks statistics on their states.

### `public void Enqueue(string eventType, object eventData, string errorMessage, string stackTrace)`

Enqueues a new dead-letter item into the processor.

- **Parameters**
  - `eventType`: The type of the event that failed to process.
  - `eventData`: The serialized or raw event data that failed.
  - `errorMessage`: The error message associated with the failure.
  - `stackTrace`: The stack trace of the exception that caused the failure.
- **Throws**
  - `ArgumentNullException`: If `eventType`, `eventData`, `errorMessage`, or `stackTrace` is `null`.

### `public DeadLetterStats GetStats()`

Retrieves aggregated statistics about the dead-letter items currently managed by the processor.

- **Returns**
  A `DeadLetterStats` object containing counts of items by status (`TotalItems`, `PendingItems`, `RetryingItems`, `FailedItems`, `SuccessfulItems`).

### `public IEnumerable<DeadLetterItem> GetAllItems()`

Retrieves all dead-letter items currently managed by the processor.

- **Returns**
  An enumerable sequence of `DeadLetterItem` objects representing all tracked items.

### `public bool RemoveItem(string id)`

Removes a dead-letter item by its unique identifier.

- **Parameters**
  - `id`: The unique identifier of the item to remove.
- **Returns**
  `true` if the item was found and removed; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `id` is `null`.

### `public string? Id`

Gets the unique identifier of the `DeadLetterProcessor` instance.

### `public string? EventType`

Gets the event type associated with the dead-letter item (applies to item-level properties).

### `public object? EventData`

Gets the event data associated with the dead-letter item (applies to item-level properties).

### `public string? ErrorMessage`

Gets the error message associated with the dead-letter item (applies to item-level properties).

### `public string? StackTrace`

Gets the stack trace associated with the dead-letter item (applies to item-level properties).

### `public DateTime CreatedAt`

Gets the timestamp when the dead-letter item was created (applies to item-level properties).

### `public DateTime? LastRetryAt`

Gets the timestamp of the last retry attempt for the dead-letter item (applies to item-level properties).

### `public int RetryCount`

Gets the number of retry attempts made for the dead-letter item (applies to item-level properties).

### `public DeadLetterStatus Status`

Gets or sets the processing status of the dead-letter item (applies to item-level properties).

### `public int TotalItems`

Gets the total number of dead-letter items currently managed by the processor.

### `public int PendingItems`

Gets the number of dead-letter items currently in a pending state.

### `public int RetryingItems`

Gets the number of dead-letter items currently being retried.

### `public int FailedItems`

Gets the number of dead-letter items that have failed and will not be retried.

### `public int SuccessfulItems`

Gets the number of dead-letter items that were successfully reprocessed.

## Usage

### Example 1: Enqueue and Process a Dead-Letter Item
