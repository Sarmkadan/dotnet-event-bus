# EventEnvelope

A lightweight container for events in a distributed system, carrying metadata about the event's origin, processing state, and context. Used by `dotnet-event-bus` to standardize event serialization, tracking, and routing across services.

## API

### `public string? EventId`
A unique identifier for the event. Optional; if omitted, the system will generate one. Used for deduplication and correlation across services.

### `public required string EventType`
The fully qualified type name of the event payload (e.g., `MyApp.Orders.OrderCreated`). Required to deserialize the `Payload` correctly during consumption.

### `public int Version`
The schema version of the event. Used to handle backward compatibility when evolving event contracts over time. Defaults to `1` if not set.

### `public required object Payload`
The actual domain event or data being transmitted. Serialized and deserialized based on `EventType`. Must be serializable by the configured serializer in `dotnet-event-bus`.

### `public DateTime CreatedAt`
The UTC timestamp when the event was created. Set automatically by `Create` or `CreateLinked` methods. Used for ordering, timeouts, and audit trails.

### `public string? CorrelationId`
An identifier for grouping related events across services (e.g., a user action or workflow instance). Optional but recommended for tracing event chains.

### `public string? CausationId`
The `EventId` of the event that triggered this one, if applicable. Used to reconstruct the causal chain of events in a system.

### `public string? Source`
The logical origin of the event (e.g., `orders-service`, `payment-gateway`). Optional but useful for routing and filtering.

### `public string? Actor`
The identifier of the user or system component that initiated the event (e.g., `user-123`, `system-scheduler`). Used for audit and access control.

### `public Dictionary<string, object> Metadata`
A flexible key-value store for additional context (e.g., `{"tenantId": "acme"}`, `{"region": "us-west"}`). Values must be JSON-serializable.

### `public bool IsTestEvent`
Indicates whether the event is a test or synthetic event. Used by consumers to skip processing in production environments.

### `public int ProcessingAttempts`
The number of times the event has been processed or retried. Incremented automatically by the bus on each attempt.

### `public TimeSpan ProcessingTimeout`
The maximum duration allowed for processing this event. If exceeded, the event may be dead-lettered or retried based on bus configuration.

### `public int Priority`
A numeric priority level for scheduling (higher values = higher priority). Defaults to `0`. Used by prioritized queues in the event bus.

### `public bool IsCritical`
Indicates whether the event is critical to system stability. Critical events may trigger alerts or special handling during failures.

### `public static EventEnvelope Create(object payload, string eventType)`
Creates a new event envelope with the given payload and type.

- **Parameters**:
  - `payload`: The domain event or data to wrap.
  - `eventType`: The fully qualified type name of the payload.
- **Returns**: A new `EventEnvelope` with `CreatedAt` set to `DateTime.UtcNow`, `Version` set to `1`, and other fields initialized to defaults.
- **Throws**: `ArgumentNullException` if `payload` or `eventType` is `null`.

### `public static EventEnvelope CreateLinked(EventEnvelope parent, object payload, string eventType)`
Creates a new event linked to a parent event (e.g., a follow-up action).

- **Parameters**:
  - `parent`: The parent event to link to.
  - `payload`: The domain event or data to wrap.
  - `eventType`: The fully qualified type name of the payload.
- **Returns**: A new `EventEnvelope` with `CausationId` set to `parent.EventId`, `CorrelationId` inherited from `parent`, and other fields initialized as in `Create`.
- **Throws**: `ArgumentNullException` if `parent`, `payload`, or `eventType` is `null`.

### `public Dictionary<string, string> GetHeaders()`
Extracts a flat dictionary of headers from the envelope, suitable for transport or logging.

- **Returns**: A `Dictionary<string, string>` containing:
  - `eventId` → `EventId`
  - `eventType` → `EventType`
  - `version` → `Version.ToString()`
  - `createdAt` → `CreatedAt.ToString("o")`
  - `correlationId` → `CorrelationId`
  - `causationId` → `CausationId`
  - `source` → `Source`
  - `actor` → `Actor`
  - `isTestEvent` → `IsTestEvent.ToString()`
  - `priority` → `Priority.ToString()`
  - `isCritical` → `IsCritical.ToString()`
  - `processingAttempts` → `ProcessingAttempts.ToString()`
  - `processingTimeout` → `ProcessingTimeout.TotalMilliseconds.ToString()`
  - All keys from `Metadata` (with values converted to strings via `ToString()`).
- **Note**: Headers are lossy; `Metadata` values that are not strings are converted to their string representation.

### `public bool IsValid()`
Validates the structural integrity of the envelope.

- **Returns**: `true` if:
  - `EventType` is not `null` or empty.
  - `Payload` is not `null`.
  - `CreatedAt` is not `default(DateTime)`.
  - `Version` is ≥ `1`.
- **Otherwise**: `false`.

### `public bool Success`
Indicates whether the event was processed successfully. Defaults to `true`. Consumers should set this to `false` if processing fails, triggering retries or dead-lettering based on bus configuration.

## Usage

### Example 1: Creating and Publishing an Event
