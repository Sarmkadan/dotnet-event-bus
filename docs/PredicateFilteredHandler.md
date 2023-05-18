# PredicateFilteredHandler

A handler wrapper that applies a predicate filter before delegating to an inner `IEventHandler<T>` implementation. It enables conditional execution of handlers based on runtime criteria while preserving the original handler's behavior for matching events.

## API

### `PredicateFilteredHandler`

Initializes a new instance of the `PredicateFilteredHandler` class.
