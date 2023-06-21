# EventFormatterFactory

Central factory for registering, retrieving, and managing `IEventFormatter` instances used to serialize and deserialize events in the `dotnet-event-bus` system. It provides a singleton-like facade over a concurrent collection of formatters, enabling dynamic registration and resolution of content-type-specific formatters at runtime.

## API

### `public static EventFormatterFactory CreateDefault()`

Creates a new `EventFormatterFactory` instance with a default set of built-in formatters (e.g., JSON, Protobuf) registered. This factory is independent of any other instances and maintains its own formatter registry.

- **Returns**: A new `EventFormatterFactory` instance.
- **Throws**: No exceptions.

---

### `public void Register(IEventFormatter formatter)`

Registers a custom `IEventFormatter` with the factory. The formatter is associated with its supported content types and becomes available for retrieval via `GetFormatter`, `GetFormatterByContentType`, or `GetAllFormatters`.

- **Parameters**:
  - `formatter` (`IEventFormatter`): The formatter to register. Must not be `null`.
- **Throws**:
  - `ArgumentNullException`: If `formatter` is `null`.
  - `ArgumentException`: If the formatter’s content types are `null` or contain `null`/`empty` entries.

---

### `public IEventFormatter? GetFormatter(Type eventType)`

Retrieves the first formatter capable of serializing or deserializing events of the specified type. The lookup prioritizes formatters registered with the most specific content types first.

- **Parameters**:
  - `eventType` (`Type`): The type of event to format. Must not be `null`.
- **Returns**:
  - The first matching `IEventFormatter`, or `null` if no formatter supports the type.
- **Throws**:
  - `ArgumentNullException`: If `eventType` is `null`.

---

### `public IEventFormatter? GetFormatterByContentType(string contentType)`

Retrieves the formatter registered for the exact content type specified. Content types are case-insensitive.

- **Parameters**:
  - `contentType` (`string`): The content type to match (e.g., `"application/json"`). Must not be `null` or whitespace.
- **Returns**:
  - The registered `IEventFormatter`, or `null` if no formatter supports the content type.
- **Throws**:
  - `ArgumentNullException`: If `contentType` is `null`.
  - `ArgumentException`: If `contentType` is whitespace.

---
### `public IEnumerable<IEventFormatter> GetAllFormatters()`

Returns an enumerable of all registered formatters in the order they were registered. The collection is a snapshot and remains unchanged even if the factory is modified concurrently.

- **Returns**: An `IEnumerable<IEventFormatter>` of all registered formatters.
- **Throws**: No exceptions.

---
### `public IEnumerable<string> GetSupportedFormats()`

Returns an enumerable of all content types supported by registered formatters. Each content type appears once, regardless of how many formatters support it.

- **Returns**: An `IEnumerable<string>` of supported content types (e.g., `"application/json"`, `"application/protobuf"`).
- **Throws**: No exceptions.

---
### `public bool IsFormatSupported(string contentType)`

Checks whether any registered formatter supports the specified content type. Comparison is case-insensitive.

- **Parameters**:
  - `contentType` (`string`): The content type to check. Must not be `null` or whitespace.
- **Returns**:
  - `true` if the content type is supported; otherwise, `false`.
- **Throws**:
  - `ArgumentNullException`: If `contentType` is `null`.
  - `ArgumentException`: If `contentType` is whitespace.

---
### `public bool Unregister(IEventFormatter formatter)`

Removes a previously registered formatter from the factory. If the formatter was registered multiple times, only the first occurrence is removed.

- **Parameters**:
  - `formatter` (`IEventFormatter`): The formatter to remove. Must not be `null`.
- **Returns**:
  - `true` if the formatter was found and removed; otherwise, `false`.
- **Throws**:
  - `ArgumentNullException`: If `formatter` is `null`.

## Usage

### Example 1: Registering and Retrieving a Custom Formatter
```csharp
using var factory = EventFormatterFactory.CreateDefault();

// Register a custom formatter for Protobuf
var protobufFormatter = new ProtobufEventFormatter();
factory.Register(protobufFormatter);

// Retrieve by content type
var formatter = factory.GetFormatterByContentType("application/protobuf");
Console.WriteLine(formatter != null); // True

// Check support
Console.WriteLine(factory.IsFormatSupported("application/protobuf")); // True
```

### Example 2: Dynamic Formatter Resolution at Runtime
```csharp
using var factory = EventFormatterFactory.CreateDefault();

// Add a JSON formatter dynamically
factory.Register(new JsonEventFormatter());

// Resolve formatter by event type
var formatter = factory.GetFormatter(typeof(MyEvent));
Console.WriteLine(formatter?.GetType().Name); // JsonEventFormatter

// List all supported formats
foreach (var contentType in factory.GetSupportedFormats())
{
    Console.WriteLine(contentType);
}
```

## Notes

- **Thread Safety**: All public members are thread-safe. Concurrent registration, retrieval, and unregistration are supported without external synchronization. Retrievals (`GetFormatter`, `GetFormatterByContentType`, etc.) operate on a consistent snapshot of the registry at the time of invocation.
- **Duplicate Registration**: Registering the same formatter instance multiple times is allowed, but only the first occurrence is used for resolution. Unregister removes only the first occurrence.
- **Content Type Matching**: Content type comparisons are case-insensitive (e.g., `"Application/JSON"` matches `"application/json"`).
- **Null Handling**: Methods accepting `string` parameters treat `null` or whitespace as invalid inputs and throw exceptions. Retrieval methods return `null` when no match is found rather than throwing.
