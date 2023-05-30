# DeadLetterEntry

Represents a record of an event that failed to be processed after exhausting all retry attempts. It captures the original event, details about the failure, and the current handling state, enabling downstream tools to review, replay, or permanently discard the message.

## API

### Constructor

**DeadLetterEntry()**  
- **Purpose:** Creates a new instance of the `DeadLetterEntry` type.  
- **Parameters:** None.  
- **Return value:** A newly initialized `DeadLetterEntry` object with default values for all members.  
- **Exceptions:** None.

### Properties

**Id** (`string`)  
- **Purpose:** Unique identifier for the dead‑letter entry, typically a GUID or similar key.  
- **Get/Set:** Read/write.  
- **Exceptions:** None.

**Message** (`EventMessage`)  
- **Purpose:** The original event message that caused the failure.  
- **Get/Set:** Read/write.  
- **Exceptions:** None.

**FailedHandlerName** (`string`)  
- **Purpose:** Name of the handler that threw the exception leading to the dead‑letter state.  
- **Get/Set:** Read/write.  
- **Exceptions:** None.

**ExceptionMessage** (`string`)  
- **Purpose:** The message associated with the exception that caused the processing failure.  
- **Get/Set:** Read/write.  
- **Exceptions:** None.

**ExceptionStackTrace** (`string?`)  
- **Purpose:** Optional stack trace of the exception; may be `null` if not available.  
- **Get/Set:** Read/write.  
- **Exceptions:** None.

**CreatedAtUtc** (`DateTime`)  
- **Purpose:** Timestamp (in UTC) when the dead‑letter entry was created.  
- **Get/Set:** Read/write.  
- **Exceptions:** None.

**MaxRetryAttempts** (`int`)  
- **Purpose:** Maximum number of retry attempts that were configured for the associated event before it was deemed a failure.  
- **Get/Set:** Read/write.  
- **Exceptions:** None.

**Status** (`DeadLetterStatus`)  
- **Purpose:** Current processing state of the entry (e.g., `Pending`, `Reviewed`, `Reprocessed`, `ReprocessFailed`).  
- **Get/Set:** Read/write.  
- **Exceptions:** None.

**StatusReason** (`string?`)  
- **Purpose:** Optional explanatory text describing why the entry is in its current `Status`. May be `null`.  
- **Get/Set:** Read/write.  
- **Exceptions:** None.

### Methods

**MarkAsReviewed()**  
- **Purpose:** Transitions the entry to the `Reviewed` state, indicating that a human or automated process has examined it.  
- **Parameters:** None.  
- **Return value:** `void`.  
- **Exceptions:** May throw `InvalidOperationException` if the entry is already in a terminal state such as `Reprocessed` or `ReprocessFailed`.

**MarkAsReprocessed()**  
- **Purpose:** Transitions the entry to the `Reprocessed` state, indicating that the event has been successfully retried.  
- **Parameters:** None.  
- **Return value:** `void`.  
- **Exceptions:** May throw `InvalidOperationException` if the entry is not in a state that allows reprocessing (e.g., already `Reprocessed`).

**MarkAsReprocessFailed()**  
- **Purpose:** Transitions the entry to the `ReprocessFailed` state, indicating that a retry attempt was made but failed again.  
- **Parameters:** None.  
- **Return value:** `void`.  
- **Exceptions:** May throw `InvalidOperationException` if called while the entry is already in a terminal state.

**GetSummary()** (`string`)  
- **Purpose:** Returns a concise, human‑readable summary of the dead‑letter entry, useful for logging or UI display.  
- **Parameters:** None.  
- **Return value:** A string containing the `Id`, `FailedHandlerName`, `ExceptionMessage`, and current `Status`.  
- **Exceptions:** None.

## Usage

```csharp
using DotNetEventBus;

// Assume we have an EventMessage that failed processing.
EventMessage failedMsg = GetFailedEventFromQueue();

// Create a dead‑letter entry capturing the failure details.
var entry = new DeadLetterEntry
{
    Id = Guid.NewGuid().ToString(),
    Message = failedMsg,
    FailedHandlerName = "OrderCreatedHandler",
    ExceptionMessage = "Database connection timeout",
    ExceptionStackTrace = stackTrace, // obtained from caught exception
    CreatedAtUtc = DateTime.UtcNow,
    MaxRetryAttempts = 3,
    Status = DeadLetterStatus.Pending,
    StatusReason = "Exceeded retry limit"
};

// Later, after an operator reviews the entry:
entry.MarkAsReviewed();
Console.WriteLine(entry.GetSummary());
// Output example: "ID: a1b2c3... | Handler: OrderCreatedHandler | Error: Database connection timeout | Status: Reviewed"
```

```csharp
using DotNetEventBus;

// Retry logic that moves a dead‑letter entry back into processing.
if (entry.Status == DeadLetterStatus.Pending && CanRetry(entry))
{
    try
    {
        ProcessEvent(entry.Message);
        entry.MarkAsReprocessed();
    }
    catch (Exception ex)
    {
        entry.ExceptionMessage = ex.Message;
        entry.ExceptionStackTrace = ex.StackTrace;
        entry.MarkAsReprocessFailed();
    }
}
```

## Notes

- The class does not provide any synchronization mechanisms; concurrent access from multiple threads should be guarded by the caller (e.g., using locks or concurrent collections) to avoid race conditions on mutable properties such as `Status` and `StatusReason`.
- `ExceptionStackTrace` and `StatusReason` can be `null`; consumers should handle these cases when displaying or persisting the entry.
- Setting `MaxRetryAttempts` after the entry has been created does not retroactively affect the original retry count; it is intended for informational or configuration purposes only.
- Transitioning to a terminal state (`Reprocessed` or `ReprocessFailed`) prevents further state changes via the `MarkAs*` methods; attempting to do so will result in an `InvalidOperationException`.
- The `GetSummary` method is designed for quick diagnostics and may evolve; it should not be relied upon for parsing structured data.
