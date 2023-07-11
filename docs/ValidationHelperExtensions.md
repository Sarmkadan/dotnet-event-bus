# ValidationHelperExtensions

Provides a set of static factory methods that create pre-configured `ValidationHelper` instances for common validation scenarios. These extensions simplify guard clauses and input validation throughout the event bus infrastructure by offering ready-to-use validators with consistent error messages and exception behavior.

## API

All members are static methods that return a `ValidationHelper` instance. Each returned helper encapsulates a specific validation rule and throws an `ArgumentException` (or a derived exception type) when validation fails.

### RequireNotEmpty

```csharp
public static ValidationHelper RequireNotEmpty { get; }
```

Returns a `ValidationHelper` that validates that a `string` value is not null, empty, or whitespace.

**Parameters:** None (static property).

**Returns:** `ValidationHelper` configured for non-empty string validation.

**Throws:** `ArgumentException` when the validated string is null, empty, or consists only of whitespace characters.

---

### RequireNotNull\<T\>

```csharp
public static ValidationHelper RequireNotNull<T>() where T : class
```

Returns a `ValidationHelper` that validates that a reference-type value is not null.

**Type Parameters:**
- `T` — The reference type to validate.

**Returns:** `ValidationHelper` configured for null-check validation.

**Throws:** `ArgumentNullException` when the validated value is null.

---

### RequireNotEmpty\<T\>

```csharp
public static ValidationHelper RequireNotEmpty<T>()
```

Returns a `ValidationHelper` that validates that a collection or enumerable of type `T` is not null and contains at least one element.

**Type Parameters:**
- `T` — The element type of the collection.

**Returns:** `ValidationHelper` configured for non-empty collection validation.

**Throws:** `ArgumentException` when the collection is null or empty.

---

### RequirePattern

```csharp
public static ValidationHelper RequirePattern(string pattern)
```

Returns a `ValidationHelper` that validates that a string matches the specified regular expression pattern.

**Parameters:**
- `pattern` — The regular expression pattern to match against.

**Returns:** `ValidationHelper` configured for regex pattern validation.

**Throws:** `ArgumentException` when the string does not match the pattern. May throw `ArgumentNullException` if the pattern argument itself is null.

---

### RequireAlphanumeric

```csharp
public static ValidationHelper RequireAlphanumeric { get; }
```

Returns a `ValidationHelper` that validates that a string contains only alphanumeric characters (letters and digits).

**Parameters:** None (static property).

**Returns:** `ValidationHelper` configured for alphanumeric validation.

**Throws:** `ArgumentException` when the string contains non-alphanumeric characters or is null/empty.

---

### RequireAlphabetic

```csharp
public static ValidationHelper RequireAlphabetic { get; }
```

Returns a `ValidationHelper` that validates that a string contains only alphabetic characters (letters only, no digits or symbols).

**Parameters:** None (static property).

**Returns:** `ValidationHelper` configured for alphabetic validation.

**Throws:** `ArgumentException` when the string contains non-alphabetic characters or is null/empty.

---

### RequireNumeric

```csharp
public static ValidationHelper RequireNumeric { get; }
```

Returns a `ValidationHelper` that validates that a string represents a numeric value (digits only).

**Parameters:** None (static property).

**Returns:** `ValidationHelper` configured for numeric string validation.

**Throws:** `ArgumentException` when the string contains non-digit characters or is null/empty.

---

### RequireMinLength

```csharp
public static ValidationHelper RequireMinLength(int minLength)
```

Returns a `ValidationHelper` that validates that a string or collection meets a minimum length requirement.

**Parameters:**
- `minLength` — The minimum allowed length (inclusive).

**Returns:** `ValidationHelper` configured for minimum length validation.

**Throws:** `ArgumentException` when the length of the validated item is less than `minLength`.

---

### RequireMaxLength

```csharp
public static ValidationHelper RequireMaxLength(int maxLength)
```

Returns a `ValidationHelper` that validates that a string or collection does not exceed a maximum length.

**Parameters:**
- `maxLength` — The maximum allowed length (inclusive).

**Returns:** `ValidationHelper` configured for maximum length validation.

**Throws:** `ArgumentException` when the length of the validated item exceeds `maxLength`.

---

### RequireExactItems\<T\>

```csharp
public static ValidationHelper RequireExactItems<T>(int expectedCount)
```

Returns a `ValidationHelper` that validates that a collection of type `T` contains exactly the specified number of items.

**Type Parameters:**
- `T` — The element type of the collection.

**Parameters:**
- `expectedCount` — The exact number of items required.

**Returns:** `ValidationHelper` configured for exact item count validation.

**Throws:** `ArgumentException` when the collection is null or its count does not equal `expectedCount`.

---

### RequireGreaterThan\<T\>

```csharp
public static ValidationHelper RequireGreaterThan<T>(T threshold) where T : IComparable<T>
```

Returns a `ValidationHelper` that validates that a comparable value is strictly greater than the specified threshold.

**Type Parameters:**
- `T` — A type implementing `IComparable<T>`.

**Parameters:**
- `threshold` — The value that the validated item must exceed.

**Returns:** `ValidationHelper` configured for greater-than comparison.

**Throws:** `ArgumentException` when the validated value is less than or equal to `threshold`.

---

### RequireLessThan\<T\>

```csharp
public static ValidationHelper RequireLessThan<T>(T threshold) where T : IComparable<T>
```

Returns a `ValidationHelper` that validates that a comparable value is strictly less than the specified threshold.

**Type Parameters:**
- `T` — A type implementing `IComparable<T>`.

**Parameters:**
- `threshold` — The value that the validated item must not reach or exceed.

**Returns:** `ValidationHelper` configured for less-than comparison.

**Throws:** `ArgumentException` when the validated value is greater than or equal to `threshold`.

---

### RequireValidIpAddress

```csharp
public static ValidationHelper RequireValidIpAddress { get; }
```

Returns a `ValidationHelper` that validates that a string represents a valid IPv4 or IPv6 address.

**Parameters:** None (static property).

**Returns:** `ValidationHelper` configured for IP address validation.

**Throws:** `ArgumentException` when the string is not a valid IP address format.

---

### RequireValidGuid

```csharp
public static ValidationHelper RequireValidGuid { get; }
```

Returns a `ValidationHelper` that validates that a string represents a valid GUID (Globally Unique Identifier).

**Parameters:** None (static property).

**Returns:** `ValidationHelper` configured for GUID format validation.

**Throws:** `ArgumentException` when the string cannot be parsed as a GUID.

## Usage

### Example 1: Validating Event Message Properties Before Publishing

```csharp
using dotnet_event_bus;

public class EventPublisher
{
    public void PublishOrderCreatedEvent(string orderId, string customerEmail, List<string> productIds)
    {
        // Validate required fields before constructing the event
        ValidationHelperExtensions.RequireNotEmpty.Validate(orderId, nameof(orderId));
        ValidationHelperExtensions.RequirePattern(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")
            .Validate(customerEmail, nameof(customerEmail));
        ValidationHelperExtensions.RequireNotEmpty<string>().Validate(productIds, nameof(productIds));
        ValidationHelperExtensions.RequireExactItems<string>(3)
            .Validate(productIds, nameof(productIds));

        var orderEvent = new OrderCreatedEvent
        {
            OrderId = orderId,
            CustomerEmail = customerEmail,
            ProductIds = productIds
        };

        // Proceed with publishing...
    }
}
```

### Example 2: Guarding Configuration Values During Service Initialization

```csharp
using dotnet_event_bus;

public class EventBusConfiguration
{
    public string BrokerEndpoint { get; set; }
    public int MaxRetryCount { get; set; }
    public string InstanceId { get; set; }

    public void Validate()
    {
        ValidationHelperExtensions.RequireNotEmpty.Validate(BrokerEndpoint, nameof(BrokerEndpoint));
        ValidationHelperExtensions.RequireValidIpAddress.Validate(BrokerEndpoint, nameof(BrokerEndpoint));
        ValidationHelperExtensions.RequireGreaterThan(0).Validate(MaxRetryCount, nameof(MaxRetryCount));
        ValidationHelperExtensions.RequireLessThan(10).Validate(MaxRetryCount, nameof(MaxRetryCount));
        ValidationHelperExtensions.RequireValidGuid.Validate(InstanceId, nameof(InstanceId));
    }
}
```

## Notes

- **Immutability:** Each static member returns a new `ValidationHelper` instance. The helpers themselves are stateless and safe for concurrent use across multiple threads.
- **Thread Safety:** All methods are static and produce immutable objects. No shared mutable state is involved, making them inherently thread-safe.
- **Chaining:** The returned `ValidationHelper` objects can be invoked independently; they are not designed for fluent chaining. Each call to `Validate` executes a single rule.
- **Edge Cases for String Validators:** `RequireNotEmpty`, `RequireAlphanumeric`, `RequireAlphabetic`, and `RequireNumeric` all treat null and empty strings as invalid. Whitespace-only strings fail `RequireNotEmpty` but may pass character-content validators if whitespace is not explicitly excluded by their pattern.
- **Collection Validators:** `RequireNotEmpty<T>` and `RequireExactItems<T>` operate on any type implementing `IEnumerable<T>`. Null collections always fail validation.
- **Comparable Validators:** `RequireGreaterThan<T>` and `RequireLessThan<T>` use strict comparison (exclusive of the threshold). Passing a value equal to the threshold results in a validation failure.
- **IP Address Validation:** `RequireValidIpAddress` accepts both IPv4 and IPv6 formats. Hostnames or malformed addresses are rejected.
- **GUID Validation:** `RequireValidGuid` expects strings in standard GUID formats (e.g., `"d2719b1e-6a5b-4c3e-8f2a-1e7b3c5d9f0a"`). Empty strings and non-GUID text are rejected.
