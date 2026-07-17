# EventMessageModelTestsValidation

Provides static helper members for validating event‑message and subscription models used within the dotnet‑event‑bus library. The members return validation details, boolean validity checks, or enforce validity by throwing exceptions.

## API

### ValidateEventMessage
```csharp
public static IReadOnlyList<string> ValidateEventMessage(EventMessage message)
```
Validates the supplied `EventMessage` instance.  
- **Parameters**  
  - `message`: The event message to validate.  
- **Return value**  
  - An `IReadOnlyList<string>` containing zero or more validation error messages. An empty list indicates the message is valid.  
- **Exceptions**  
  - `ArgumentNullException` if `message` is `null`.

### ValidateSubscription
```csharp
public static IReadOnlyList<string> ValidateSubscription(Subscription subscription)
```
Validates the supplied `Subscription` instance.  
- **Parameters**  
  - `subscription`: The subscription to validate.  
- **Return value**  
  - An `IReadOnlyList<string>` containing zero or more validation error messages. An empty list indicates the subscription is valid.  
- **Exceptions**  
  - `ArgumentNullException` if `subscription` is `null`.

### IsValid (EventMessage overload)
```csharp
public static bool IsValid(EventMessage message)
```
Determines whether the supplied event message passes validation.  
- **Parameters**  
  - `message`: The event message to evaluate.  
- **Return value**  
  - `true` if the message has no validation errors; otherwise `false`.  
- **Exceptions**  
  - `ArgumentNullException` if `message` is `null`.

### IsValid (Subscription overload)
```csharp
public static bool IsValid(Subscription subscription)
```
Determines whether the supplied subscription passes validation.  
- **Parameters**  
  - `subscription`: The subscription to evaluate.  
- **Return value**  
  - `true` if the subscription has no validation errors; otherwise `false`.  
- **Exceptions**  
  - `ArgumentNullException` if `subscription` is `null`.

### EnsureValid (EventMessage overload)
```csharp
public static void EnsureValid(EventMessage message)
```
Throws an exception if the supplied event message is invalid.  
- **Parameters**  
  - `message`: The event message to validate.  
- **Return value**  
  - None.  
- **Exceptions**  
  - `ArgumentNullException` if `message` is `null`.  
  - `InvalidOperationException` (or a derived validation exception) containing the concatenated validation error messages if the message fails validation.

### EnsureValid (Subscription overload)
```csharp
public static void EnsureValid(Subscription subscription)
```
Throws an exception if the supplied subscription is invalid.  
- **Parameters**  
  - `subscription`: The subscription to validate.  
- **Return value**  
  - None.  
- **Exceptions**  
  - `ArgumentNullException` if `subscription` is `null`.  
  - `InvalidOperationException` (or a derived validation exception) containing the concatenated validation error messages if the subscription fails validation.

## Usage

```csharp
using DotNetEventBus.Validation;

// Example 1: Collect validation errors for an event message
var msg = new EventMessage { /* initialize */ };
IReadOnlyList<string> errors = EventMessageModelTestsValidation.ValidateEventMessage(msg);
if (errors.Count > 0)
{
    foreach (var err in errors)
    {
        Console.WriteLine($"Validation error: {err}");
    }
}
else
{
    Console.WriteLine("Event message is valid.");
}
```

```csharp
using DotNetEventBus.Validation;

// Example 2: Enforce subscription validity, throwing on failure
var sub = new Subscription { /* initialize */ };
try
{
    EventMessageModelTestsValidation.EnsureValid(sub);
    // Proceed with subscription usage
}
catch (InvalidOperationException ex)
{
    // Handle validation failure
    Console.Error.WriteLine($"Subscription invalid: {ex.Message}");
}
```

## Notes

- All members are **static** and contain no mutable state; therefore they are thread‑safe and can be invoked concurrently from multiple threads without external synchronization.  
- The `IReadOnlyList<string>` returned by the `Validate*` members is immutable; callers must not attempt to modify the list.  
- Passing `null` to any member results in an `ArgumentNullException` before any validation logic is executed.  
- The `EnsureValid` members throw an exception only when validation fails; they do not return a value. The exception type may be a custom validation exception derived from `InvalidOperationException` depending on the library’s implementation.  
- Validation logic does not alter the state of the supplied objects; it is purely inspective.
