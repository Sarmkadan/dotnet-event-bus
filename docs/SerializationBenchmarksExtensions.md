# SerializationBenchmarksExtensions

The `SerializationBenchmarksExtensions` class provides utility methods designed for benchmarking and validating serialization processes within the `dotnet-event-bus` library. It facilitates the verification of object serializability across different formats—JSON, CSV, and XML—and enables the retrieval of serialization format identifiers for targeted performance analysis.

## API

### ValidateJsonSerialization(object instance)

Validates whether the specified object can be correctly serialized to JSON format.

*   **Parameters:** `instance` (object) - The object to validate for JSON serialization.
*   **Returns:** `bool` - `true` if the object is successfully serializable; otherwise, `false`.
*   **Throws:** Throws `ArgumentNullException` if the `instance` is `null`.

### ValidateCsvSerialization(object instance)

Validates whether the specified object can be correctly serialized to CSV format.

*   **Parameters:** `instance` (object) - The object to validate for CSV serialization.
*   **Returns:** `bool` - `true` if the object is successfully serializable; otherwise, `false`.
*   **Throws:** Throws `ArgumentNullException` if the `instance` is `null`.

### ValidateXmlSerialization(object instance)

Validates whether the specified object can be correctly serialized to XML format.

*   **Parameters:** `instance` (object) - The object to validate for XML serialization.
*   **Returns:** `bool` - `true` if the object is successfully serializable; otherwise, `false`.
*   **Throws:** Throws `ArgumentNullException` if the `instance` is `null`.

### GetSerializationFormat(object instance)

Retrieves the identifier for the serialization format currently associated with the object.

*   **Parameters:** `instance` (object) - The object for which to retrieve the format identifier.
*   **Returns:** `string` - A string representing the serialization format.
*   **Throws:** Throws `ArgumentNullException` if the `instance` is `null`.

## Usage

```csharp
using DotNetEventBus.Benchmarks;

var myEvent = new UserCreatedEvent { UserId = 1, Username = "jdoe" };

// Validate JSON compatibility before benchmark
if (SerializationBenchmarksExtensions.ValidateJsonSerialization(myEvent))
{
    Console.WriteLine("Event is JSON-compatible.");
}

// Retrieve format identifier
string format = SerializationBenchmarksExtensions.GetSerializationFormat(myEvent);
Console.WriteLine($"Detected format: {format}");
```

```csharp
// Example using conditional logic for different serializers
var data = new TransactionData();

var isValid = SerializationBenchmarksExtensions.ValidateXmlSerialization(data) 
    && SerializationBenchmarksExtensions.ValidateCsvSerialization(data);

if (isValid)
{
    // Perform comparative benchmarking
}
```

## Notes

*   **Edge Cases:** Serialization validation methods may return `false` for complex objects that include circular references, depending on the underlying serializer configuration.
*   **Thread Safety:** The methods in this class are stateless and designed to be thread-safe, provided that the underlying objects being passed for validation do not exhibit thread-safety issues during serialization attempts.
*   **Performance:** These methods are intended for use in benchmarking environments. Avoid using them in high-throughput production paths, as the overhead of serialization validation can be significant.
