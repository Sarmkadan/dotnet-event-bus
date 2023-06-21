# CsvEventFormatter

The `CsvEventFormatter` class provides methods for serializing and deserializing event objects to and from CSV (comma-separated values) strings. It is designed to work with the event bus infrastructure, enabling events to be formatted as CSV lines for logging, storage, or transmission. The formatter supports both generic and non-generic deserialization, as well as methods that include optional metadata (such as event type or timestamp) in the output.

## API

### `public CsvEventFormatter()`

Initializes a new instance of the `CsvEventFormatter` class. The constructor takes no parameters and sets up the formatter with default CSV formatting rules (comma delimiter, no header, standard escaping).

### `public string Serialize(object value)`

Serializes the specified object into a CSV-formatted string.

- **Parameters**  
  `value` – The object to serialize. Must not be `null`.

- **Returns**  
  A `string` containing the CSV representation of the object. The string includes all public readable properties of the object, serialized as a single CSV line.

- **Throws**  
  `ArgumentNullException` – if `value` is `null`.  
  `InvalidOperationException` – if the object’s properties cannot be read or converted to CSV (e.g., unsupported property types).

### `public T? Deserialize<T>(string csvLine)`

Deserializes a CSV-formatted string into an instance of type `T`.

- **Type Parameters**  
  `T` – The target type to deserialize into. Must have a parameterless constructor.

- **Parameters**  
  `csvLine` – A CSV string representing a single line of data. Must not be `null` or empty.

- **Returns**  
  An instance of `T` with properties populated from the CSV fields. Returns `default(T)` if the CSV line is empty or contains only whitespace (when `T` is a nullable reference type or value type).

- **Throws**  
  `ArgumentNullException` – if `csvLine` is `null`.  
  `ArgumentException` – if `csvLine` is empty or malformed (e.g., incorrect number of fields).  
  `InvalidOperationException` – if the CSV fields cannot be converted to the corresponding property types of `T`.

### `public object? Deserialize(string csvLine, Type type)`

Deserializes a CSV-formatted string into an object of the specified type.

- **Parameters**  
  `csvLine` – A CSV string representing a single line of data. Must not be `null` or empty.  
  `type` – The `Type` of the object to create. Must have a parameterless constructor.

- **Returns**  
  An `object?` instance of the given `type` with properties populated from the CSV fields. Returns `null` if the CSV line is empty or contains only whitespace.

- **Throws**  
  `ArgumentNullException` – if `csvLine` or `type` is `null`.  
  `ArgumentException` – if `csvLine` is empty or malformed, or if `type` does not have a parameterless constructor.  
  `InvalidOperationException` – if the CSV fields cannot be converted to the property types of the specified type.

### `public string FormatEvent(object eventData)`

Formats an event object as a CSV string. This method is semantically equivalent to `Serialize`, but is intended for use specifically with event objects in the event bus pipeline.

- **Parameters**  
  `eventData` – The event object to format. Must not be `null`.

- **Returns**  
  A CSV-formatted string representing the event data.

- **Throws**  
  `ArgumentNullException` – if `eventData` is `null`.  
  `InvalidOperationException` – if the event object cannot be serialized to CSV.

### `public string FormatEventWithMetadata(object eventData, string? eventType = null, DateTime? timestamp = null)`

Formats an event object as a CSV string, prepending optional metadata fields (event type and timestamp) to the CSV line.

- **Parameters**  
  `eventData` – The event object to format. Must not be `null`.  
  `eventType` – An optional string representing the event type name. If provided, it is added as the first CSV field.  
  `timestamp` – An optional `DateTime` value. If provided, it is added as the second CSV field (after `eventType`, or as the first field if `eventType` is `null`).

- **Returns**  
  A CSV-formatted string that includes the metadata fields (if supplied) followed by the serialized event data.

- **Throws**  
  `ArgumentNullException` – if `eventData` is `null`.  
  `InvalidOperationException` – if the event object cannot be serialized to CSV.

## Usage

### Example 1: Basic Serialization and Deserialization

```csharp
using DotNetEventBus.Formatters;

public class OrderPlacedEvent
{
    public string OrderId { get; set; }
    public decimal Total { get; set; }
    public string CustomerName { get; set; }
}

var formatter = new CsvEventFormatter();
var orderEvent = new OrderPlacedEvent
{
    OrderId = "ORD-123",
    Total = 299.99m,
    CustomerName = "Alice"
};

// Serialize the event to CSV
string csv = formatter.Serialize(orderEvent);
Console.WriteLine(csv); // Output: "ORD-123","299.99","Alice"

// Deserialize back to an OrderPlacedEvent
var deserialized = formatter.Deserialize<OrderPlacedEvent>(csv);
Console.WriteLine(deserialized.OrderId); // Output: ORD-123
```

### Example 2: Formatting with Metadata

```csharp
using DotNetEventBus.Formatters;

public class UserLoggedInEvent
{
    public string UserId { get; set; }
    public string IpAddress { get; set; }
}

var formatter = new CsvEventFormatter();
var loginEvent = new UserLoggedInEvent
{
    UserId = "user42",
    IpAddress = "192.168.1.1"
};

// Format with event type and timestamp
string csvWithMeta = formatter.FormatEventWithMetadata(
    loginEvent,
    eventType: "UserLoggedIn",
    timestamp: DateTime.UtcNow);

Console.WriteLine(csvWithMeta);
// Example output: "UserLoggedIn","2025-03-20T10:30:00Z","user42","192.168.1.1"
```

## Notes

- **Null Handling**: All methods throw `ArgumentNullException` when required parameters are `null`. The `Deserialize` methods treat an empty or whitespace-only CSV line as a null/default result, but a non-null, non-empty line that is malformed will throw an `ArgumentException`.
- **Type Requirements**: Types used with `Deserialize<T>` or the non-generic `Deserialize` must have a parameterless constructor. Properties are matched to CSV fields by order (the order in which they are serialized by `Serialize`). The formatter does not support custom field mapping or header rows.
- **Escaping**: CSV fields containing commas, double quotes, or newlines are automatically escaped according to standard CSV rules (RFC 4180). The formatter does not support custom delimiters.
- **Thread Safety**: The `CsvEventFormatter` class is immutable and does not maintain any mutable state. All public methods are reentrant and safe to call concurrently from multiple threads.
