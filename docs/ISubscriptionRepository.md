# ISubscriptionRepository

The `ISubscriptionRepository` interface defines a contract for managing event subscriptions within the `dotnet-event-bus` system. It provides methods to query, enable, and disable subscriptions based on event types and handler names, supporting prioritization and filtering of active/inactive subscriptions. Implementations of this interface are responsible for persisting and retrieving subscription data, enabling the event bus to route events to their respective handlers.

## API

### `Task<IEnumerable<Subscription>> GetByEventTypeAsync`
**Purpose**: Retrieves all subscriptions associated with a specific event type, regardless of their active status.
**Parameters**: None (assumes event type is inferred or provided via implementation-specific means).
**Returns**: An enumerable collection of `Subscription` objects matching the event type.
**Throws**: May throw `ArgumentException` if the event type is invalid or not supported.

### `Task<IEnumerable<Subscription>> GetActiveByEventTypeAsync`
**Purpose**: Retrieves all *active* subscriptions for a given event type.
**Parameters**: None (assumes event type is inferred or provided via implementation-specific means).
**Returns**: An enumerable collection of active `Subscription` objects for the specified event type.
**Throws**: May throw `ArgumentException` if the event type is invalid or not supported.

### `Task<IEnumerable<Subscription>> GetByHandlerNameAsync`
**Purpose**: Retrieves all subscriptions associated with a specific handler name, regardless of their active status.
**Parameters**: None (assumes handler name is inferred or provided via implementation-specific means).
**Returns**: An enumerable collection of `Subscription` objects matching the handler name.
**Throws**: May throw `ArgumentException` if the handler name is invalid or not found.

### `Task<IEnumerable<Subscription>> GetAllActiveAsync`
**Purpose**: Retrieves all active subscriptions across all event types.
**Parameters**: None.
**Returns**: An enumerable collection of all active `Subscription` objects.
**Throws**: None.

### `Task<IEnumerable<Subscription>> GetAllInactiveAsync`
**Purpose**: Retrieves all inactive subscriptions across all event types.
**Parameters**: None.
**Returns**: An enumerable collection of all inactive `Subscription` objects.
**Throws**: None.

### `Task<IEnumerable<Subscription>> GetByEventTypeOrderedByPriorityAsync`
**Purpose**: Retrieves all subscriptions for a given event type, ordered by their priority (ascending or descending, as defined by the implementation).
**Parameters**: None (assumes event type is inferred or provided via implementation-specific means).
**Returns**: An enumerable collection of `Subscription` objects for the specified event type, ordered by priority.
**Throws**: May throw `ArgumentException` if the event type is invalid or not supported.

### `Task<int> CountByEventTypeAsync`
**Purpose**: Returns the count of subscriptions for a given event type, regardless of their active status.
**Parameters**: None (assumes event type is inferred or provided via implementation-specific means).
**Returns**: The number of subscriptions for the specified event type.
**Throws**: May throw `ArgumentException` if the event type is invalid or not supported.

### `Task DisableHandlerAsync`
**Purpose**: Disables all subscriptions associated with a specific handler name, marking them as inactive.
**Parameters**: None (assumes handler name is inferred or provided via implementation-specific means).
**Returns**: A `Task` representing the asynchronous operation.
**Throws**: May throw `ArgumentException` if the handler name is invalid or not found.

### `Task EnableHandlerAsync`
**Purpose**: Enables all subscriptions associated with a specific handler name, marking them as active.
**Parameters**: None (assumes handler name is inferred or provided via implementation-specific means).
**Returns**: A `Task` representing the asynchronous operation.
**Throws**: May throw `ArgumentException` if the handler name is invalid or not found.

## Usage

### Example 1: Querying and Disabling Subscriptions by Event Type
