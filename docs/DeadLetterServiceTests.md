# DeadLetterServiceTests

This class provides unit tests for the `DeadLetterService` component in the `dotnet-event-bus` project, verifying its core functionality for managing dead-lettered messages, including retrieval, status updates, statistics, and archiving operations.

## API

### `DeadLetterServiceTests`
The test fixture class containing all tests for the `DeadLetterService` functionality. It uses xUnit and Moq to validate behavior under various scenarios.

### `public async Task GetPendingEntriesAsync_ShouldReturnPendingEntries`
Verifies that the service correctly retrieves all dead-lettered entries that are pending review. The test asserts that the returned collection contains only entries with `Status = Pending`.

### `public async Task MarkAsReviewedAsync_ShouldUpdateStatus`
Ensures that marking a dead-lettered entry as reviewed updates its status to `Reviewed`. The test validates both the state change and persistence behavior.

### `public async Task GetStatisticsAsync_ShouldReturnAccurateStats`
Checks that the service returns accurate statistics for dead-lettered entries, including total counts, pending vs. reviewed breakdowns, and age distribution. The test compares actual results against expected values.

### `public async Task ArchiveOldEntriesAsync_ShouldArchiveOldEntries`
Confirms that entries older than a configurable threshold are correctly archived and removed from the active dead-letter store. The test verifies both the removal and archival persistence.

---

### `SubscriptionManagerTests`
A nested test fixture class focused on validating subscription management within the dead-letter context, particularly handler enabling/disabling behavior.

### `public async Task GetSubscriptionsAsync_ShouldReturnSubscriptionInfo`
Validates that the service returns accurate subscription metadata for all registered handlers. The test checks that the returned collection includes expected handler identifiers and event types.

### `public async Task DisableHandlerAsync_ShouldDisableAllHandlerSubscriptions`
Ensures that disabling a handler via the dead-letter service correctly disables all its associated subscriptions. The test verifies both in-memory state and persistence.

### `public async Task GetStatisticsAsync_ShouldReturnAccurateStats`
Reuses the same test logic as the outer fixture to validate statistics consistency across different service contexts.

---

### `DeadLetterExceptionHandlingTests`
A nested test fixture dedicated to validating exception capture and handling in dead-lettered messages.

### `public async Task DeadLetterEntry_ShouldCaptureFullExceptionDetails`
Confirms that when an event handler throws an exception, the dead-letter service captures the full exception details, including message, stack trace, and metadata.

### `public async Task DeadLetterEntry_WithInnerException_ShouldCaptureFullStackTrace`
Ensures that nested exceptions (inner exceptions) are fully captured and preserved in the dead-letter entry, including their stack traces.

### `public async Task DeadLetterEntry_WithNullException_ShouldHandleGracefully`
Validates that the service handles null exceptions gracefully without throwing, creating a dead-letter entry with appropriate fallback metadata.

### `public async Task InvokeAsync_WithValidHandler_ShouldInvoke`
Checks that the dead-letter service correctly invokes a valid handler for a given event type, ensuring the handler receives the expected payload.

### `public void CanHandle_WithValidHandlerAndEventType_ShouldReturnTrue`
Verifies that the handler capability check returns `true` when a handler supports the specified event type.

### `public void CanHandle_WithInvalidEventType_ShouldReturnFalse`
Ensures that the handler capability check returns `false` when the event type is not supported by the handler.

### `public void GetSupportedEventTypes_ShouldReturnHandlerEventTypes`
Validates that the service returns the correct set of event types supported by a given handler.

### `public void EventBusOptions_Validate_ShouldThrowOnInvalidOptions`
Confirms that `EventBusOptions` validation throws an exception when required fields (e.g., retry policy) are missing or invalid.

### `public void EventBusOptions_CalculateRetryDelay_ShouldUseExponentialBackoff`
Ensures that retry delay calculation follows exponential backoff based on retry count and configured parameters.

### `public void EventBusOptions_Clone_ShouldCreateIndependentCopy`
Validates that cloning `EventBusOptions` produces a deep copy that does not share mutable state with the original.

## Usage

### Example 1: Testing Dead Letter Retrieval
