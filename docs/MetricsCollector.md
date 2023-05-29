# MetricsCollector

A utility class for collecting and reporting metrics related to event publishing, handler execution, system performance, and latency statistics in a .NET event bus system. It tracks counts, durations, success rates, and provides aggregated views of system behavior over time.

## API

### `MetricsCollector()`
Initializes a new instance of the `MetricsCollector` class with all counters and metrics set to zero.

### `void RecordEventPublished()`
Records the occurrence of an event being published. Increments the `PublishCount` and updates relevant metrics.

### `void RecordEventFailed()`
Records the occurrence of an event publishing failure. Increments the `FailureCount` and updates relevant metrics.

### `void RecordHandlerExecution(long durationMs)`
Records the execution of an event handler with the specified duration in milliseconds.
- **Parameters**:
  - `durationMs` (long): The duration of the handler execution in milliseconds.
- **Throws**: `ArgumentOutOfRangeException` if `durationMs` is negative.

### `EventMetrics? GetEventMetrics()`
Retrieves the aggregated metrics for all events.
- **Returns**: An `EventMetrics` object containing total published count, failure count, success rate, and average duration, or `null` if no events have been recorded.

### `IEnumerable<EventMetrics> GetAllEventMetrics()`
Retrieves metrics for each distinct event type that has been recorded.
- **Returns**: An enumerable of `EventMetrics` objects, one per event type, ordered by event type name. Returns an empty enumerable if no events have been recorded.

### `IEnumerable<HandlerMetrics> GetHandlerMetrics()`
Retrieves metrics for each distinct handler that has been recorded.
- **Returns**: An enumerable of `HandlerMetrics` objects, one per handler, ordered by handler type name. Returns an empty enumerable if no handlers have been recorded.

### `HandlerMetrics? GetHandlerMetrics(string handlerTypeName)`
Retrieves metrics for a specific handler by its type name.
- **Parameters**:
  - `handlerTypeName` (string): The fully qualified type name of the handler.
- **Returns**: A `HandlerMetrics` object for the specified handler, or `null` if no such handler has been recorded.

### `IEnumerable<HandlerMetrics> GetAllHandlerMetrics()`
Retrieves metrics for all handlers that have been recorded.
- **Returns**: An enumerable of `HandlerMetrics` objects, one per handler, ordered by handler type name. Returns an empty enumerable if no handlers have been recorded.

### `double GetSuccessRate()`
Calculates the overall success rate of event publishing as a value between 0.0 and 1.0.
- **Returns**: The success rate, or 0.0 if no events have been published.

### `double GetAverageDuration()`
Calculates the average duration of handler executions in milliseconds.
- **Returns**: The average duration in milliseconds, or 0.0 if no handler executions have been recorded.

### `SystemMetrics GetSystemMetrics()`
Retrieves the latest recorded system metrics snapshot.
- **Returns**: A `SystemMetrics` object containing CPU usage, memory usage, and other system-level statistics.

### `LatencyStats? GetLatencyStats()`
Retrieves aggregated latency statistics for event publishing and handling.
- **Returns**: A `LatencyStats` object containing percentiles and averages for latency measurements, or `null` if no latency data has been recorded.

### `IEnumerable<LatencyStats> GetAllLatencyStats()`
Retrieves latency statistics grouped by event type.
- **Returns**: An enumerable of `LatencyStats` objects, one per event type, ordered by event type name. Returns an empty enumerable if no latency data has been recorded.

### `void Reset()`
Resets all recorded metrics and counters to their initial state (zero values).

### `string? EventType`
Gets the type name of the current event being tracked, if applicable. May be `null` if not tracking a specific event.

### `long PublishCount`
Gets the total number of events published.

### `long FailureCount`
Gets the total number of event publishing failures.

### `long TotalDurationMs`
Gets the cumulative duration in milliseconds of all handler executions.

### `double AverageDurationMs`
Gets the average duration in milliseconds of handler executions.

## Usage

### Basic Usage Example
