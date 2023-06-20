# ReflectionHelper

Utility class providing common reflection operations to locate types, inspect attributes, manipulate objects, and invoke members at runtime.

## API

### `FindImplementationsOf<TInterface>()`
Locates all concrete, non-abstract types in the current assembly that implement the specified interface or base class.
- **Parameters**: None.
- **Return value**: `IEnumerable<Type>` of matching implementations.
- **Throws**: `ArgumentException` if `TInterface` is not an interface or abstract class.

### `FindImplementationsOfInAllAssemblies<TInterface>()`
Locates all concrete, non-abstract types across all loaded assemblies that implement the specified interface or base class.
- **Parameters**: None.
- **Return value**: `IEnumerable<Type>` of matching implementations.
- **Throws**: `ArgumentException` if `TInterface` is not an interface or abstract class.

### `GetMethodsBySignature(Type type, string name, Type[] parameterTypes)`
Retrieves methods matching a precise signature on the given type.
- **Parameters**:
  - `type`: The type to search.
  - `name`: Method name; pass `null` to match any name.
  - `parameterTypes`: Ordered parameter types; pass `null` or empty array to match parameterless methods.
- **Return value**: `IEnumerable<MethodInfo>` of matching methods.
- **Throws**: `ArgumentNullException` if `type` is `null`.

### `TryCreateInstance<T>()`
Attempts to instantiate a type `T` using its public parameterless constructor.
- **Parameters**: None.
- **Return value**: `T?` – the new instance, or `null` if instantiation fails.
- **Throws**: None.

### `GetCustomAttributes<TAttribute>()`
Extracts all custom attributes of type `TAttribute` from the current assembly.
- **Parameters**: None.
- **Return value**: `IEnumerable<TAttribute>` of found attributes.
- **Throws**: None.

### `HasAttribute<TAttribute>()`
Determines whether the current assembly defines at least one attribute of type `TAttribute`.
- **Parameters**: None.
- **Return value**: `bool` indicating presence.
- **Throws**: None.

### `GetPropertyValue(object? instance, string propertyName)`
Retrieves the value of a named property on the given instance.
- **Parameters**:
  - `instance`: The object whose property is read; `null` for static properties.
  - `propertyName`: Name of the property.
- **Return value**: `object?` – the property value, or `null` if the property does not exist or is `null`.
- **Throws**: `ArgumentNullException` if `propertyName` is `null`.

### `SetPropertyValue(object? instance, string propertyName, object? value)`
Assigns a value to a named property on the given instance.
- **Parameters**:
  - `instance`: The object whose property is set; `null` for static properties.
  - `propertyName`: Name of the property.
  - `value`: New value; may be `null`.
- **Return value**: None.
- **Throws**: `ArgumentNullException` if `propertyName` is `null`; `TargetException` if the property is read-only or inaccessible.

### `GetAllPropertyValues(object instance)`
Collects all readable, non-indexer property values from an instance into a dictionary keyed by property name.
- **Parameters**:
  - `instance`: The object whose properties are read.
- **Return value**: `Dictionary<string, object?>` mapping property names to values.
- **Throws**: `ArgumentNullException` if `instance` is `null`.

### `InvokeMethod(object? instance, string methodName, object?[]? parameters)`
Invokes a named method on the given instance with the provided arguments.
- **Parameters**:
  - `instance`: The object whose method is invoked; `null` for static methods.
  - `methodName`: Name of the method.
  - `parameters`: Argument values; may be `null` or empty for parameterless methods.
- **Return value**: `object?` – the method’s return value, or `null` if the method is `void`.
- **Throws**: `ArgumentNullException` if `methodName` is `null`; `MissingMethodException` if the method does not exist; `TargetInvocationException` if the method throws.

### `GetGenericArguments(Type type)`
Retrieves the generic type arguments of a generic type or generic type definition.
- **Parameters**:
  - `type`: The generic type.
- **Return value**: `Type[]` of generic arguments.
- **Throws**: `ArgumentNullException` if `type` is `null`; `ArgumentException` if `type` is not a generic type.

## Usage
