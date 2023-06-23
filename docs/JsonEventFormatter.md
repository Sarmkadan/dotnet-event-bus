# JsonEventFormatter

`JsonEventFormatter` provides utilities for converting event objects to and from JSON representation, as well as for producing formatted string descriptions of events with optional metadata. The class is stateless and relies on the underlying JSON serializer (typically `System.Text.Json`) for all conversion operations.

## API

### JsonEventFormatter
The class itself contains no state; instances can be created freely or used via static‑like patterns if the members are made static in the implementation. Its primary role is to namespace the formatting and serialization helpers.

### Serialize
```csharp
public string Serialize(object @event);
```
**Purpose** – Serializes the supplied event object to a JSON string.  
**Parameters**  
- `@event`: The event instance to serialize. Must not be `null`.  
**Return Value** – A UTF‑8 encoded JSON string representing the event.  
**Exceptions**  
- `ArgumentNullException` if `@event` is `null`.  
- `JsonSerializationException` if the serializer encounters an error (e.g., unsupported type, circular reference).

### Deserialize<T>
```csharp
public T? Deserialize<T>(string json);
```
**Purpose** – Deserializes a JSON string into an instance of the generic type `T`.  
**Parameters**  
- `json`: The JSON input to deserialize. Must not be `null`.  
**Return Value** – An object of type `T` populated from the JSON, or `default(T?)` if deserialization yields `null`.  
**Exceptions**  
- `ArgumentNullException` if `json` is `null`.  
- `JsonSerializationException` if the JSON is malformed or cannot be mapped to `T`.

### Deserialize
```csharp
public object? Deserialize(string json, Type type);
```
**Purpose** – Deserializes a JSON string into an object of the specified runtime `Type`.  
**Parameters**  
- `json`: The JSON input to deserialize. Must not be `null`.  
- `type`: The target `Type` for the deserialized object. Must not be `null` and must be a type supported by the serializer.  
**Return Value** – An object instance of `type` populated from the JSON, or `null` if the JSON represents a null value.  
**Exceptions**  
- `ArgumentNullException` if `json` or `type` is `null`.  
- `ArgumentException` if `type` is not serializable by the underlying serializer.  
- `JsonSerializationException` if the JSON cannot be deserialized to the given type.

### FormatEvent
```csharp
public string FormatEvent(object @event);
```
**Purpose** – Produces a concise, human‑readable string representation of the event, typically including its type name and a summary of its properties.  
**Parameters**  
- `@event`: The event to format. Must not be `null`.  
**Return Value** – A formatted string describing the event.  
**Exceptions**  
- `ArgumentNullException` if `@event` is `null`.  
- `InvalidOperationException` if the event’s properties cannot be accessed for formatting (e.g., due to security restrictions).

### FormatEventWithMetadata
```csharp
public string FormatEventWithMetadata(object @event, IDictionary<string, object> metadata);
```
**Purpose** – Produces a formatted string that includes both the event’s standard representation and additional key‑value metadata pairs.  
**Parameters**  
- `@event`: The event to format. Must not be `null`.  
- `metadata`: A dictionary of extra data to include in the output. Must not be `null`; entries with `null` keys or values are ignored.  
**Return Value** – A formatted string containing the event description followed by the metadata entries.  
**Exceptions**  
- `ArgumentNullException` if `@event` or `metadata` is `null`.  
- `InvalidOperationException` if formatting of the event or any metadata value fails.

## Usage

### Example 1: Serializing and deserializing an event
```csharp
var formatter = new JsonEventFormatter();

var orderCreated = new OrderCreated { OrderId = 123, Amount = 99.95 };
string json = formatter.Serialize(orderCreated);
// json now contains something like: {"OrderId":123,"Amount":99.95}

var restored = formatter.Deserialize<OrderCreated>(json);
// restored.OrderId == 123 && restored.Amount == 99.95
```

### Example 2: Formatting an event with metadata
```csharp
var formatter = new JsonEventFormatter();

var userLoggedIn = new UserLoggedIn { UserId = "alice", Timestamp = DateTime.UtcNow };
var meta = new Dictionary<string, object>
{
    ["Source"] = "Web",
    ["IpAddress"] = "203.0.113.42"
};

string formatted = formatter.FormatEventWithMetadata(userLoggedIn, meta);
// formatted might be: "UserLoggedIn { UserId=alice, Timestamp=2025-11-02T14:30:00Z } | Source=Web, IpAddress=203.0.113.42"
```

## Notes
- All members are stateless; therefore, `JsonEventFormatter` instances are thread‑safe and can be shared across threads without external synchronization.  
- Null arguments are validated explicitly; passing `null` for any required parameter results in an `ArgumentNullException`.  
- The JSON serialization settings (e.g., property naming policy, handling of unknown members) are determined by the serializer used internally; consumers requiring custom settings should wrap or extend this class accordingly.  
- `Deserialize<T>` returns `null` when the JSON represents a JSON `null` value and `T` is a reference type or nullable value type; otherwise, the default value for `T` is returned.  
- Formatting methods rely on reflection to read public properties; non‑public or indexer properties are not included in the output.  
- Culture‑specific formatting (e.g., date/time strings) follows the defaults of the underlying serializer; to enforce a particular culture, adjust the serializer options before invoking these methods.
