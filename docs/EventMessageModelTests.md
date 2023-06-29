# EventMessageModelTests

Unit test class for verifying the behavior of `EventMessageModel` in the `dotnet-event-bus` project. This class focuses on validating message retry logic, header management, and activation state transitions for event messages.

## API

### `CreateRetry_ShouldIncrementProcessingAttemptsAndPreserveHeaders()`

Validates that retrying an event message increments the `ProcessingAttempts` counter while preserving all existing headers. The test constructs an initial message with headers, triggers a retry, and asserts that the `ProcessingAttempts` value increases by one and that all headers remain unchanged.

### `AddHeader_ThenGetHeader_ShouldReturnStoredValue()`

Ensures that headers added to an event message can be retrieved exactly as stored. The test adds a header with a known key-value pair and asserts that `GetHeader` returns the identical value for that key.

### `GetHeader_WithUnknownKey_ShouldReturnNull()`

Confirms that attempting to retrieve a non-existent header returns `null` rather than throwing an exception. The test verifies the behavior when querying for a header key that was never added to the message.

### `Disable_ThenEnable_ShouldToggleIsActiveCorrectly()`

Checks that toggling the `IsActive` state of an event message works as expected. The test disables the message, verifies the state change, re-enables it, and asserts that the final state matches expectations.

### `SetTimeout_WithZeroOrNegativeDuration_ShouldThrowArgumentException()`

Validates that setting a zero or negative timeout duration on an event message throws an `ArgumentException`. The test invokes `SetTimeout` with invalid durations and asserts that the expected exception is raised.

## Usage
