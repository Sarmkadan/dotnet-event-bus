# ISubscriptionManager

Central interface for managing event subscriptions in the dotnet-event-bus system. Provides methods to inspect, query, and retrieve subscription metadata, including counts, distributions, and active states. Designed for monitoring and diagnostic purposes rather than runtime subscription modification.

## API

### `public string Id`
Unique identifier for this subscription manager instance. Used for correlation and logging within the event bus system.

### `public string EventType`
The event type this manager instance is configured to handle. May be `null` or empty if the manager oversees multiple event types.

### `public string HandlerName`
The name of the handler associated with this subscription manager. Used to identify the component responsible for processing events.

### `public bool IsActive`
Indicates whether the subscription manager is currently active and processing events. When `false`, subscriptions may still exist but will not be triggered.

### `public int Priority`
The priority level assigned to this subscription manager. Higher values indicate higher priority for event processing order.

### `public bool IsAsync`
Indicates whether the handler associated with this manager executes asynchronously. Affects how subscriptions are processed and monitored.

### `public TimeSpan? Timeout`
Optional timeout duration for handler execution. If exceeded, the subscription may be considered failed or abandoned. `null` indicates no timeout.

### `public DateTime CreatedAtUtc`
The UTC timestamp when this subscription manager was instantiated.

### `public int TotalSubscriptions`
Total number of subscriptions registered with this manager, including active and inactive.

### `public int ActiveSubscriptions`
Number of subscriptions currently active and eligible for event processing.

### `public int InactiveSubscriptions`
Number of subscriptions registered but currently inactive and not processing events.

### `public int UniqueEventTypes`
Number of distinct event types managed by this subscription manager.

### `public int UniqueHandlers`
Number of distinct handlers managed by this subscription manager.

### `public Dictionary<string, int> SubscriptionsByEventType`
Mapping of event type names to the count of subscriptions for each type across all handlers.

### `public Dictionary<string, int> SubscriptionsByHandler`
Mapping of handler names to the count of subscriptions each handler manages.

### `public Dictionary<string, int> ActiveSubscriptionsByEventType`
Mapping of event type names to the count of active subscriptions for each type.

### `public SubscriptionManager`
Reference to the underlying `SubscriptionManager` instance that implements this interface. Provides access to internal state and behavior.

### `public async Task<IEnumerable<SubscriptionInfo>> GetSubscriptionsAsync()`
Retrieves all subscription metadata managed by this instance.

- **Returns**: An asynchronous sequence of `SubscriptionInfo` objects representing each subscription.
- **Throws**: `InvalidOperationException` if the manager is in an invalid state or encounters an unrecoverable error during retrieval.

### `public async Task<IEnumerable<SubscriptionInfo>> GetAllSubscriptionsAsync()`
Retrieves all subscription metadata across all event types and handlers managed by this instance.

- **Returns**: An asynchronous sequence of `SubscriptionInfo` objects representing every subscription.
- **Throws**: `InvalidOperationException` if the manager is in an invalid state or encounters an unrecoverable error during retrieval.

### `public async Task<int> GetSubscriptionCountAsync()`
Retrieves the total number of subscriptions managed by this instance.

- **Returns**: The count of all subscriptions, including active and inactive.
- **Throws**: `InvalidOperationException` if the manager is in an invalid state or cannot compute the count.

## Usage
