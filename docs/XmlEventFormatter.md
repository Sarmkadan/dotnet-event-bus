# XmlEventFormatter

`XmlEventFormatter` is an implementation of the event serialization contract that converts event objects and their optional metadata into XML strings and back. It provides both generic and non-generic deserialization paths, along with helper methods to format events into human-readable or log-friendly representations.

## API

### `public XmlEventFormatter`

Default parameterless constructor. Initializes a new instance of the formatter with standard XML serialization settings. No configuration arguments are required.

### `public string Serialize`

Serializes an event object to its XML string representation.

- **Parameters**  
  Implicitly accepts the event object to serialize (the exact signature depends on the interface being implemented; typically `Serialize<T>(T event)` or `Serialize(object event)`).
- **Return value**  
  `string` – the XML representation of the event.
- **Exceptions**  
  Throws `InvalidOperationException` or `XmlException` if the object cannot be serialized (e.g., it contains non-serializable members or circular references). May throw `ArgumentNullException` if a null event is passed when not permitted.

### `public T? Deserialize<T>`

Deserializes an XML string back into a strongly-typed event object.

- **Parameters**  
  `string` – the XML payload to deserialize.
- **Return value**  
  `T?` – the deserialized event instance, or `null` if the input is null/empty or deserialization yields no object.
- **Type parameter**  
  `T` – the expected event type.
- **Exceptions**  
  Throws `InvalidOperationException` or `XmlException` if the XML is malformed or does not match the structure of `T`. May throw `ArgumentNullException` for a null input when not supported.

### `public object? Deserialize`

Non-generic deserialization method. Converts an XML string into an object instance without compile-time type knowledge.

- **Parameters**  
  `string` – the XML payload, and typically a `Type` argument indicating the target event type.
- **Return value**  
  `object?` – the deserialized event instance, or `null` if deserialization fails or input is empty.
- **Exceptions**  
  Throws `ArgumentNullException` if the target type is null. Throws `InvalidOperationException` or `XmlException` for malformed XML or type mismatches.

### `public string FormatEvent`

Produces a formatted string representation of an event object, intended for logging or diagnostics. The output is not necessarily valid XML for deserialization but is human-readable.

- **Parameters**  
  The event object to format.
- **Return value**  
  `string` – a formatted representation of the event.
- **Exceptions**  
  Throws `ArgumentNullException` if the event is null. May throw `InvalidOperationException` if formatting encounters an unexpected object state.

### `public string FormatEventWithMetadata`

Produces a formatted string that includes both the event object and its associated metadata (e.g., headers, timestamp, correlation IDs). Like `FormatEvent`, the output is intended for human consumption rather than machine deserialization.

- **Parameters**  
  The event object and a metadata object/dictionary.
- **Return value**  
  `string` – a combined formatted representation of the event and its metadata.
- **Exceptions**  
  Throws `ArgumentNullException` if either the event or metadata is null. May throw `InvalidOperationException` if the metadata structure is incompatible with the formatter’s expectations.

## Usage

### Example 1: Serialize and deserialize a typed event

```csharp
var formatter = new XmlEventFormatter();

var orderPlaced = new OrderPlacedEvent
{
    OrderId = Guid.NewGuid(),
    CustomerName = "Jane Doe",
    Amount = 129.99m
};

string xml = formatter.Serialize(orderPlaced);

// Transmit or persist the XML, then later:
OrderPlacedEvent? deserialized = formatter.Deserialize<OrderPlacedEvent>(xml);

Console.WriteLine($"Order {deserialized?.OrderId} for {deserialized?.CustomerName}");
```

### Example 2: Log an event with metadata using non-generic deserialization

```csharp
var formatter = new XmlEventFormatter();

var paymentReceived = new PaymentReceivedEvent
{
    PaymentId = "PAY-001",
    Status = "Completed"
};

var metadata = new Dictionary<string, string>
{
    ["CorrelationId"] = Guid.NewGuid().ToString(),
    ["OccurredAt"] = DateTime.UtcNow.ToString("O")
};

string formatted = formatter.FormatEventWithMetadata(paymentReceived, metadata);
Console.WriteLine(formatted);

// Later, reconstruct the event without knowing its concrete type at compile time:
object? reconstructed = formatter.Deserialize(formatter.Serialize(paymentReceived), typeof(PaymentReceivedEvent));
var typed = reconstructed as PaymentReceivedEvent;
Console.WriteLine(typed?.PaymentId);
```

## Notes

- **Thread safety**: `XmlEventFormatter` relies on the underlying `XmlSerializer`, which is documented as thread-safe for serialization and deserialization operations once constructed. Instances of this class can be safely shared across multiple threads after initialization.
- **Null handling**: `Deserialize<T>` and `Deserialize` may return `null` for empty or whitespace input strings. Callers should guard against null results when the event payload is mandatory.
- **Type fidelity**: The non-generic `Deserialize` requires the caller to supply the correct `Type`. Passing an incompatible type will result in an `InvalidOperationException` or a `null` return, depending on the mismatch.
- **Format methods**: `FormatEvent` and `FormatEventWithMetadata` are not guaranteed to produce round-trippable XML. Their output is optimized for readability and may include additional formatting, indentation, or metadata annotations that are not valid for deserialization.
- **Edge cases**: Objects with circular references, non-public setters, or types not decorated with XML serialization attributes will cause serialization failures. The formatter does not perform schema validation; malformed XML passed to `Deserialize` will result in exceptions rather than graceful degradation.
