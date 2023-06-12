# EventBusOptions

Configuration options for the EventBus system, controlling behavior such as retry policies, concurrency limits, middleware registration, and distributed transport settings.

## API

### `DefaultHandlerTimeout`
Gets or sets the default timeout for event handler execution. If a handler does not complete within this duration, it will be considered failed and subject to retry logic.

- **Type**: `TimeSpan`
- **Default**: `TimeSpan.FromSeconds(30)`
- **Throws**: `ArgumentOutOfRangeException` if set to a negative value.

---

### `MaxRetryAttempts`
Gets or sets the maximum number of retry attempts for a failed event handler before it is moved to the dead-letter queue (if enabled).

- **Type**: `int`
- **Default**: `3`
- **Throws**: `ArgumentOutOfRangeException` if set to a negative value.

---

### `RetryDelay`
Gets or sets the initial delay between retry attempts for failed event handlers.

- **Type**: `TimeSpan`
- **Default**: `TimeSpan.FromSeconds(1)`
- **Throws**: `ArgumentOutOfRangeException` if set to a negative value.

---
### `RetryDelayMultiplier`
Gets or sets the multiplier applied to the `RetryDelay` after each failed attempt, increasing the delay exponentially.

- **Type**: `double`
- **Default**: `2.0`
- **Throws**: `ArgumentOutOfRangeException` if set to a value less than `1.0`.

---
### `MaxRetryDelay`
Gets or sets the maximum allowed delay between retry attempts, regardless of exponential backoff.

- **Type**: `TimeSpan`
- **Default**: `TimeSpan.FromMinutes(5)`
- **Throws**: `ArgumentOutOfRangeException` if set to a negative value.

---
### `AllowParallelHandling`
Gets or sets a value indicating whether event handlers can be executed concurrently for the same event.

- **Type**: `bool`
- **Default**: `false`

---
### `MaxConcurrentHandlers`
Gets or sets the maximum number of concurrent handler executions allowed when `AllowParallelHandling` is `true`.

- **Type**: `int`
- **Default**: `10`
- **Throws**: `ArgumentOutOfRangeException` if set to a value less than `1`.

---
### `EnableDeadLetterQueue`
Gets or sets a value indicating whether failed events should be routed to a dead-letter queue instead of being discarded.

- **Type**: `bool`
- **Default**: `true`

---
### `ThrowOnHandlerFailure`
Gets or sets a value indicating whether the EventBus should throw an exception when a handler fails, regardless of retry settings.

- **Type**: `bool`
- **Default**: `false`

---
### `IsDistributed`
Gets or sets a value indicating whether the EventBus operates in a distributed environment using a transport layer.

- **Type**: `bool`
- **Default**: `false`

---
### `DistributedTransportType`
Gets or sets the fully qualified type name of the distributed transport implementation to use when `IsDistributed` is `true`.

- **Type**: `string?`
- **Default**: `null`
- **Throws**: `ArgumentException` if set to a non-null, non-parseable type name.

---
### `DistributedTransportConnectionString`
Gets or sets the connection string for the distributed transport layer when `IsDistributed` is `true`.

- **Type**: `string?`
- **Default**: `null`

---
### `MiddlewareTypes`
Gets the list of middleware types to be registered with the EventBus pipeline.

- **Type**: `List<Type>`
- **Default**: Empty list
- **Throws**: `ArgumentException` if any type in the list does not implement the expected middleware interface.

---
### `RequestTimeout`
Gets or sets the timeout for request/response operations within the EventBus.

- **Type**: `TimeSpan`
- **Default**: `TimeSpan.FromSeconds(10)`
- **Throws**: `ArgumentOutOfRangeException` if set to a negative value.

---
### `Validate()`
Validates the current configuration and throws an exception if any required settings are invalid or inconsistent.

- **Returns**: `void`
- **Throws**:
  - `InvalidOperationException` if `IsDistributed` is `true` but `DistributedTransportType` or `DistributedTransportConnectionString` is `null`.
  - `InvalidOperationException` if `MaxRetryDelay` is less than `RetryDelay`.
  - `InvalidOperationException` if `RetryDelayMultiplier` is less than `1.0`.

---
### `CalculateRetryDelay(int attempt)`
Calculates the delay for the next retry attempt based on the current attempt number, applying exponential backoff with the configured `RetryDelayMultiplier` and capped by `MaxRetryDelay`.

- **Parameters**:
  - `attempt`: The current retry attempt number (1-based).
- **Returns**: `TimeSpan` representing the calculated delay.
- **Throws**: `ArgumentOutOfRangeException` if `attempt` is less than `1`.

---
### `Clone()`
Creates a deep copy of the current `EventBusOptions` instance.

- **Returns**: A new `EventBusOptions` instance with the same property values.
- **Note**: The `MiddlewareTypes` list is cloned, but the types themselves are not deep-copied.

## Usage

### Basic Configuration
