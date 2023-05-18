# PredicateFilteredHandlerExtensions

Extension methods for creating and combining predicate-based event handlers that filter whether an inner handler should be invoked based on runtime conditions.

## API

### `WithInnerHandler<TEvent>`
Attaches an inner handler to a predicate-based handler, enabling conditional execution of the inner handler based on the predicate's evaluation.

- **Parameters**
  - `handler`: The inner handler to be invoked when the predicate evaluates to `true`.
- **Return Value**
  Returns a new `PredicateFilteredHandler<TEvent>` instance that combines the predicate logic with the provided inner handler.
- **Throws**
  Throws `ArgumentNullException` if `handler` is `null`.

### `InvertPredicate<TEvent>`
Inverts the logical outcome of the predicate associated with the handler.

- **Return Value**
  Returns a new `PredicateFilteredHandler<TEvent>` with the inverted predicate logic.
- **Throws**
  Throws `InvalidOperationException` if the handler has no predicate defined.

### `AndPredicate<TEvent>`
Combines the current predicate with another predicate using a logical AND operation.

- **Parameters**
  - `predicate`: The predicate to combine with the existing predicate via logical AND.
- **Return Value**
  Returns a new `PredicateFilteredHandler<TEvent>` with the combined predicate logic.
- **Throws**
  Throws `ArgumentNullException` if `predicate` is `null`.
  Throws `InvalidOperationException` if the handler has no predicate defined.

### `OrPredicate<TEvent>`
Combines the current predicate with another predicate using a logical OR operation.

- **Parameters**
  - `predicate`: The predicate to combine with the existing predicate via logical OR.
- **Return Value**
  Returns a new `PredicateFilteredHandler<TEvent>` with the combined predicate logic.
- **Throws**
  Throws `ArgumentNullException` if `predicate` is `null`.
  Throws `InvalidOperationException` if the handler has no predicate defined.

## Usage
