# BatchPublishingOptimizationExample

A utility class demonstrating batch publishing optimization patterns for event-driven systems, providing methods to track and process event batches efficiently while exposing metrics for monitoring.

## API

### `LogId`
- **Purpose**: Unique identifier for the log entry.
- **Type**: `string`
- **Remarks**: Read-only property representing the log entry's unique key.

### `Level`
- **Purpose**: Severity level of the log entry (e.g., "Info", "Error").
- **Type**: `string`
- **Remarks**: Read-only property indicating the log level.

### `Message`
- **Purpose**: Descriptive content of the log entry.
- **Type**: `string`
- **Remarks**: Read-only property containing the log message.

### `Timestamp`
- **Purpose**: Point in time when the log entry was created.
- **Type**: `DateTime`
- **Remarks**: Read-only property set to the moment of instantiation.

### `EventType`
- **Purpose**: Classification of the event being processed.
- **Type**: `string`
- **Remarks**: Read-only property describing the event category.

### `UserId`
- **Purpose**: Identifier of the user associated with the event.
- **Type**: `string`
- **Remarks**: Read-only property linking the event to a user.

### `Properties`
- **Purpose**: Additional contextual data associated with the log entry.
- **Type**: `Dictionary<string, object>`
- **Remarks**: Read-only property holding extensible metadata.

### `Handle()`
- **Purpose**: Asynchronous method to process the batched event.
- **Returns**: `Task`
- **Remarks**: Overridable; intended to encapsulate batch processing logic.

### `GetProcessedCount()`
- **Purpose**: Static method to retrieve the total number of events processed.
- **Returns**: `int` – count of processed events.
- **Remarks**: Thread-safe counter for monitoring throughput.

### `Main(string[] args)`
- **Purpose**: Entry point for demonstrating batch publishing optimization.
- **Parameters**:
  - `args` – Command-line arguments (unused).
- **Returns**: `Task`
- **Remarks**: Async entry point showcasing batch handling patterns.

## Usage

### Example 1: Basic Batch Processing
