# TypeExtensions

Provides a set of extension methods for the `System.Type` class that simplify common type inspection tasks used throughout the `dotnet-event-bus` infrastructure. These methods offer concise, readable ways to query type metadata, check inheritance and interface implementation, retrieve friendly names, and enumerate public properties without repetitive reflection code.

## API

### `GetFriendlyName`

```csharp
public static string GetFriendlyName(this Type type)
```

Returns a human-readable representation of the type, including generic parameters in a compact form (e.g., `List<int>` instead of `List\`1[System.Int32]`).

- **Parameters**  
  `type` – The type to format. Must not be `null`.

- **Returns**  
  A `string` containing the friendly name.

- **Throws**  
  `ArgumentNullException` if `type` is `null`.

---

### `IsAssignableFromNullable`

```csharp
public static bool IsAssignableFromNullable(this Type type, Type other)
```

Determines whether an instance of type `other` can be assigned to a variable of type `type`, taking nullable value types into account. For example, `typeof(int?)` is considered assignable from `typeof(int)`.

- **Parameters**  
  `type` – The target (assignable-to) type.  
  `other` – The source type to check.

- **Returns**  
  `true` if `other` is assignable to `type` under nullable-aware rules; otherwise `false`.

- **Throws**  
  `ArgumentNullException` if either argument is `null`.

---

### `Implements<TInterface>`

```csharp
public static bool Implements<TInterface>(this Type type)
```

Checks whether the type implements the specified interface `TInterface`.

- **Type Parameters**  
  `TInterface` – The interface type to test. Must be an interface.

- **Parameters**  
  `type` – The type to inspect.

- **Returns**  
  `true` if `type` implements `TInterface` (directly or through inheritance); otherwise `false`.

- **Throws**  
  `ArgumentNullException` if `type` is `null`.  
  `InvalidOperationException` if `TInterface` is not an interface.

---

### `IsNullableType`

```csharp
public static bool IsNullableType(this Type type)
```

Indicates whether the type is a nullable value type (e.g., `int?`, `DateTime?`). Returns `false` for reference types, even though they are inherently nullable.

- **Parameters**  
  `type` – The type to examine.

- **Returns**  
  `true` if `type` is a constructed `Nullable<T>`; otherwise `false`.

- **Throws**  
  `ArgumentNullException` if `type` is `null`.

---

### `GetAllInterfaces`

```csharp
public static IEnumerable<Type> GetAllInterfaces(this Type type)
```

Retrieves all interfaces implemented by the type, including those inherited from base classes and other interfaces.

- **Parameters**  
  `type` – The type to query.

- **Returns**  
  An `IEnumerable<Type>` containing the distinct interfaces.

- **Throws**  
  `ArgumentNullException` if `type` is `null`.

---

### `IsInstantiable`

```csharp
public static bool IsInstantiable(this Type type)
```

Determines whether the type can be instantiated (i.e., is a concrete class, struct, or value type that is not abstract, static, or an interface).

- **Parameters**  
  `type` – The type to evaluate.

- **Returns**  
  `true` if `type` is a non-abstract, non-static class, struct, or value type; `false` for interfaces, abstract classes, static classes, and generic type definitions.

- **Throws**  
  `ArgumentNullException` if `type` is `null`.

---

### `GetFullTypeNameWithGenerics`

```csharp
public static string GetFullTypeNameWithGenerics(this Type type)
```

Returns the fully qualified type name, including the namespace and generic type arguments in their full form (e.g., `System.Collections.Generic.List<System.Int32>`).

- **Parameters**  
  `type` – The type to format.

- **Returns**  
  A `string` with the full type name.

- **Throws**  
  `ArgumentNullException` if `type` is `null`.

---

### `InheritsFrom`

```csharp
public static bool InheritsFrom(this Type type, Type baseType)
```

Checks whether the type inherits from the specified base type (class or interface). For interfaces, this behaves identically to `Implements<T>` but accepts a runtime `Type` argument.

- **Parameters**  
  `type` – The derived type to test.  
  `baseType` – The potential base type.

- **Returns**  
  `true` if `type` derives from or implements `baseType`; otherwise `false`.

- **Throws**  
  `ArgumentNullException` if either argument is `null`.

---

### `GetAllPublicProperties`

```csharp
public static IEnumerable<PropertyInfo> GetAllPublicProperties(this Type type)
```

Enumerates all public properties declared on the type and its base types, including those inherited from interfaces.

- **Parameters**  
  `type` – The type to inspect.

- **Returns**  
  An `IEnumerable<PropertyInfo>` representing the public properties.

- **Throws**  
  `ArgumentNullException` if `type` is `null`.

## Usage

### Example 1: Logging type information for event handlers

```csharp
using dotnet_event_bus;
using System;

public class OrderCreatedHandler
{
    public void Handle(OrderCreatedEvent @event) { }
}

// Inspect handler type
Type handlerType = typeof(OrderCreatedHandler);
Console.WriteLine($"Handler: {handlerType.GetFriendlyName()}");
Console.WriteLine($"Is nullable? {handlerType.IsNullableType()}");
Console.WriteLine($"Instantiable? {handlerType.IsInstantiable()}");
Console.WriteLine($"Full name: {handlerType.GetFullTypeNameWithGenerics()}");
```

### Example 2: Filtering types that implement a specific interface

```csharp
using dotnet_event_bus;
using System;
using System.Collections.Generic;
using System.Linq;

public interface IEventHandler<T> { }
public class UserCreatedHandler : IEventHandler<UserCreatedEvent> { }
public class EmailNotifier : IEventHandler<EmailSentEvent> { }

// Find all types that implement IEventHandler<>
Type[] candidateTypes = { typeof(UserCreatedHandler), typeof(EmailNotifier), typeof(string) };
var eventHandlers = candidateTypes
    .Where(t => t.Implements<IEventHandler<UserCreatedEvent>>())
    .ToList();

Console.WriteLine($"Handlers for UserCreatedEvent: {eventHandlers.Count}");

// List all interfaces implemented by a type
Type handlerType = typeof(UserCreatedHandler);
IEnumerable<Type> interfaces = handlerType.GetAllInterfaces();
foreach (var iface in interfaces)
    Console.WriteLine(iface.GetFriendlyName());
```

## Notes

- All methods throw `ArgumentNullException` when a `null` type argument is passed. Always validate input before calling these extensions.
- `IsNullableType` returns `false` for reference types. To check whether a reference type can be `null`, use `!type.IsValueType` or the `Nullable.GetUnderlyingType` pattern.
- `Implements<TInterface>` and `InheritsFrom` both work with generic type definitions (e.g., `typeof(IEnumerable<>)`) as well as constructed generic types. For open generics, the check is performed on the generic type definition.
- `GetAllInterfaces` and `GetAllPublicProperties` include inherited members. Duplicate interfaces (from multiple inheritance paths) are returned only once.
- `IsInstantiable` returns `false` for generic type definitions (e.g., `typeof(List<>)`) and for types that are abstract, static, or interfaces.
- These extension methods are stateless and thread-safe. They perform only read-only reflection operations and do not modify any shared state. Multiple threads may safely call them on the same `Type` instance concurrently.
