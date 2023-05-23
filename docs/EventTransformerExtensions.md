# EventTransformerExtensions

The `EventTransformerExtensions` static class provides a set of fluent extension methods for `EventTransformer<TSource, TTarget>`. These methods enable conditional transformation, null‑safety checks, multi‑target expansion, and intermediate type mapping without modifying the original transformer. Each method returns a new `EventTransformer<TSource, TTarget>` instance, allowing chaining.

## API

### `When<TSource, TTarget>`

Adds a conditional guard to the transformation pipeline. The transformation is applied only when the source event satisfies a user‑supplied predicate; otherwise, the event is passed through unchanged.

- **Parameters**  
  The method accepts a predicate delegate that receives the source event and returns a `bool`.  
- **Returns**  
  A new `EventTransformer<TSource, TTarget>` that conditionally applies the downstream transformation.  
- **Throws**  
  `ArgumentNullException` if the predicate is `null`.

### `IfNotNull<TSource, TTarget>`

Adds a null‑check guard. The transformation is applied only when the source event is not `null`; `null` events are passed through unchanged.

- **Parameters**  
  None.  
- **Returns**  
  A new `EventTransformer<TSource, TTarget>` that skips the transformation for `null` source events.  
- **Throws**  
  Nothing.

### `ThenAll<TSource, TTarget>`

Expands a single source event into multiple target events. The transformation is applied to each element returned by a user‑supplied selector, and the results are flattened into a single output stream.

- **Parameters**  
  The method accepts a selector delegate that receives the source event and returns an `IEnumerable<TTarget>`.  
- **Returns**  
  A new `EventTransformer<TSource, TTarget>` that produces zero or more target events per source event.  
- **Throws**  
  `ArgumentNullException` if the selector is `null`.

### `MapIntermediate<TSource, TTarget, TIntermediate>`

Introduces an intermediate type in the transformation pipeline. The source event is first mapped to an intermediate value, which is then finalized into the target type. This is useful when the transformation logic is split into two composable steps.

- **Parameters**  
  The method accepts two delegates: a map function from `TSource` to `TIntermediate`, and a finalize function from `TIntermediate` to `TTarget`.  
- **Returns**  
  A new `EventTransformer<TSource, TTarget>` that performs the two‑step mapping.  
- **Throws**  
  `ArgumentNullException` if either delegate is `null`.

## Usage

### Example 1: Conditional transformation with null safety

```csharp
using EventBus.Transformers;

// Assume an existing transformer that converts OrderPlaced to OrderConfirmed
EventTransformer<OrderPlaced, OrderConfirmed> baseTransformer = ...;

// Only transform orders with a total > 100, and skip null events
var conditionalTransformer = baseTransformer
    .IfNotNull()
    .When(order => order.Total > 100);

// Use the transformer in an event bus pipeline
eventBus.Subscribe<OrderPlaced, OrderConfirmed>(conditionalTransformer);
```

### Example 2: Multi‑target expansion with intermediate mapping

```csharp
using EventBus.Transformers;

// Transform a single UserRegistered event into multiple WelcomeEmail events
EventTransformer<UserRegistered, WelcomeEmail> emailTransformer = ...;

// Expand each user registration into several email targets (e.g., admin and user)
var multiTargetTransformer = emailTransformer
    .ThenAll(user => new[]
    {
        new WelcomeEmail { Recipient = user.Email, Type = "user" },
        new WelcomeEmail { Recipient = "admin@example.com", Type = "admin" }
    });

// Alternatively, map through an intermediate DTO
EventTransformer<UserRegistered, WelcomeEmail> mappedTransformer = emailTransformer
    .MapIntermediate<UserRegistered, WelcomeEmail, UserDto>(
        user => new UserDto { Name = user.Name, Email = user.Email },
        dto => new WelcomeEmail { Recipient = dto.Email, Body = $"Hello {dto.Name}" }
    );
```

## Notes

- **Edge cases**  
  - `When` with a predicate that always returns `false` will suppress all transformations; the source event is passed through unchanged.  
  - `IfNotNull` does not check for `null` inside the transformation logic; it only guards the entry point. If the source event is `null`, the entire pipeline is skipped.  
  - `ThenAll` expects the selector to return a non‑null `IEnumerable<TTarget>`. Returning `null` from the selector will cause a `NullReferenceException` at runtime.  
  - `MapIntermediate` does not cache the intermediate value; the map function is called once per source event.

- **Thread safety**  
  All extension methods are stateless and thread‑safe. The returned `EventTransformer<TSource, TTarget>` instances are immutable and can be shared across threads. However, the delegates passed to these methods (predicates, selectors, map/finalize functions) are invoked by the transformer at runtime; their thread safety depends on the caller’s implementation.
