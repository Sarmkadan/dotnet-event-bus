# EventTransformer

A utility class for transforming event data between different types in an event-driven system. It provides fluent APIs to define transformation pipelines, enabling type-safe conversion of event payloads while maintaining immutability and composition flexibility.

## API

### `public EventTransformer`
Base non-generic type providing the entry point for transformation pipelines.

### `public EventTransformer<TSource, TTarget>`
Generic type representing a transformation pipeline from `TSource` to `TTarget`. Instances are created via factory methods and support method chaining.

### `Then`
Appends a transformation step to the pipeline.

**Parameters:**
- `transformer`: An `EventTransformer<TSource, TTarget>` representing the next transformation step.

**Return Value:**
- Returns a new `EventTransformer<TSource, TTarget>` that combines the current and next steps.

**Exceptions:**
- Throws `ArgumentNullException` if `transformer` is `null`.

### `Transform`
Executes the transformation pipeline on a single source event.

**Parameters:**
- `source`: The source event to transform.

**Return Value:**
- Returns the transformed target event of type `TTarget`.

**Exceptions:**
- Throws `ArgumentNullException` if `source` is `null`.
- May throw exceptions from user-defined transformation logic.

### `TransformMany`
Executes the transformation pipeline on a sequence of source events.

**Parameters:**
- `sources`: The sequence of source events to transform.

**Return Value:**
- Returns an `IEnumerable<TTarget>` containing the transformed target events.

**Exceptions:**
- Throws `ArgumentNullException` if `sources` is `null`.
- May throw exceptions from user-defined transformation logic.

### `Chain<TIntermediate>`
Adds an intermediate transformation step to the pipeline.

**Type Parameters:**
- `TIntermediate`: The intermediate type in the transformation chain.

**Return Value:**
- Returns an `EventTransformer<TSource, TIntermediate>` representing the new pipeline up to the intermediate type.

**Exceptions:**
- None.

### `public static EventTransformer<TSource, TTarget> CreateTransformer<TSource, TTarget>()`
Factory method to create a custom transformation pipeline.

**Type Parameters:**
- `TSource`: The source event type.
- `TTarget`: The target event type.

**Return Value:**
- Returns a new `EventTransformer<TSource, TTarget>` with no transformation steps.

**Exceptions:**
- None.

### `public static EventTransformer<TSource, TTarget> CreatePropertyCopyTransformer<TSource, TTarget>()`
Factory method to create a transformer that copies properties with matching names and compatible types from `TSource` to `TTarget`.

**Type Parameters:**
- `TSource`: The source event type.
- `TTarget`: The target event type.

**Return Value:**
- Returns a new `EventTransformer<TSource, TTarget>` configured to perform property-based copying.

**Exceptions:**
- None.

### `public static EventTransformer<T, Dictionary<string, object?>> CreateDictionaryTransformer<T>()`
Factory method to create a transformer that converts an event of type `T` into a dictionary of property names and values.

**Type Parameters:**
- `T`: The event type to transform.

**Return Value:**
- Returns a new `EventTransformer<T, Dictionary<string, object?>>` that maps properties to dictionary entries.

**Exceptions:**
- None.

## Usage

### Example 1: Simple Property Copy
