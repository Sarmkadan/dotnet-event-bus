# EventMessage

Represents a single event within the event bus system, encapsulating identity, metadata, payload, and delivery tracking information. `EventMessage` is the core unit of communication, carrying the serialized event data along with routing headers, correlation identifiers, and a processing attempt counter used for retry and dead-letter handling.

## API

### Properties

- **`public string MessageId`**  
  Unique identifier for this message instance. Assigned at creation and remains immutable for the lifetime of the message. Used for deduplication, logging, and tracing across services.

- **`public string EventType`**  
  Fully qualified name of the event type (e.g., `"OrderPlaced"` or `"InventoryReserved"`). Subscribers use this value to route messages to the correct handler.

- **`public DateTime CreatedAtUtc`**  
  UTC timestamp marking when the message was originally created. Set once during construction and preserved through retries.

- **`public string Payload`**  
  Serialized event body, typically a JSON string. This is the actual data that handlers deserialize and process. Must not be null or empty for a valid message.

- **`public string? CorrelationId`**  
  Optional identifier linking this event to a broader workflow or originating request. When set, it enables distributed tracing across multiple events and services.

- **`public string? Source`**  
  Optional identifier of the producing service or component (e.g., `"order-service"`). Used for diagnostics and auditing.

- **`public Dictionary<string, string> Headers`**  
  Key-value collection of arbitrary metadata attached to the message. Headers are propagated through the bus and can influence routing, serialization, or handler behavior. The dictionary is never null; an empty dictionary is present by default.

- **`public MessageScope Scope`**  
  Enumeration indicating the delivery scope of the event. Controls whether the message is broadcast to all subscribers or delivered to a single consumer in a competing-consumers pattern.

- **`public int ProcessingAttempts`**  
  Counter tracking how many times delivery or processing has been attempted. Starts at zero for the original message and increments with each retry. Used by the bus to enforce maximum retry policies and dead-letter decisions.

### Constructors

- **`public EventMessage()`**  
  Initializes a new, empty `EventMessage` with a generated `MessageId`, `CreatedAtUtc` set to the current UTC time, `ProcessingAttempts` set to zero, an empty `Headers` dictionary, and a default `Scope`. The `Payload` and optional fields remain unset until explicitly assigned.

### Methods

- **`public void Validate()`**  
  Performs internal consistency checks on the message. Throws an `ArgumentException` or derived exception if required fields are missing or invalid (e.g., null or whitespace `Payload`, empty `EventType`). Call this before publishing to fail fast on malformed messages.

- **`public EventMessage CreateRetry()`**  
  Produces a new `EventMessage` suitable for a retry attempt. The returned message retains the original `MessageId`, `EventType`, `Payload`, `CorrelationId`, `Source`, `Headers`, and `Scope`, but has its `ProcessingAttempts` incremented by one and `CreatedAtUtc` preserved from the original. This ensures retry tracking without altering identity.

- **`public void AddHeader(string key, string value)`**  
  Adds or overwrites a header entry in the `Headers` dictionary.  
  **Parameters:**  
  - `key`: Non-null header name.  
  - `value`: Header value; null values are typically stored as empty strings or rejected depending on implementation.  
  **Throws:** `ArgumentNullException` if `key` is null.

- **`public string? GetHeader(string key)`**  
  Retrieves the value associated with the specified header key. Returns `null` if the key is not present in `Headers`.  
  **Parameters:**  
  - `key`: Header name to look up.  
  **Returns:** The header value, or `null` if not found.

## Usage

### Example 1: Creating and Publishing a New Event

```csharp
var message = new EventMessage
{
    EventType = "OrderPlaced",
    Payload = JsonSerializer.Serialize(new { OrderId = 123, Amount = 99.95m }),
    CorrelationId = "workflow-abc-456",
    Source = "order-service",
    Scope = MessageScope.Broadcast
};

message.AddHeader("priority", "high");
message.AddHeader("tenant", "region-eu");

message.Validate();

await eventBus.PublishAsync(message);
```

### Example 2: Handling a Failed Delivery with Retry

```csharp
public async Task OnProcessingFailure(EventMessage originalMessage, Exception ex)
{
    if (originalMessage.ProcessingAttempts >= maxRetries)
    {
        await deadLetterQueue.SendAsync(originalMessage);
        return;
    }

    var retryMessage = originalMessage.CreateRetry();

    retryMessage.AddHeader("last-error", ex.GetType().Name);
    retryMessage.AddHeader("retry-reason", ex.Message);

    // Exponential backoff before republishing
    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryMessage.ProcessingAttempts));
    await Task.Delay(delay);

    await eventBus.PublishAsync(retryMessage);
}
```

## Notes

- **Immutability of identity fields:** `MessageId`, `EventType`, and `CreatedAtUtc` are set at construction and must not be modified afterward. `CreateRetry()` preserves these values intentionally to maintain a single logical event lineage across attempts.
- **Header null handling:** `AddHeader` with a null value may either store an empty string or throw, depending on the underlying implementation. Callers should not rely on null header values being round-tripped faithfully.
- **Validation timing:** `Validate()` is not called automatically by the bus. Always invoke it explicitly before publishing to avoid propagating invalid messages that would be rejected downstream.
- **Thread safety:** `EventMessage` is not designed for concurrent mutation. `Headers` is a standard `Dictionary<string, string>` without synchronization. If multiple threads modify headers or other mutable fields simultaneously, external locking is required.
- **`CreateRetry()` and `ProcessingAttempts`:** The retry counter is incremented mechanically; it does not check against any maximum. Enforcement of retry limits is the responsibility of the caller or bus infrastructure, as shown in the usage example.
- **`Scope` default:** The default `MessageScope` value is determined by the enumeration definition. Consult the `MessageScope` documentation for the exact default member (typically a broadcast or single-consumer mode).
