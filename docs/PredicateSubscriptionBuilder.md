# PredicateSubscriptionBuilder

The `PredicateSubscriptionBuilder<TEvent>` class provides a fluent interface for configuring and registering event subscriptions based on dynamic predicates. It enables developers to define complex filtering logic, such as property comparisons and negation, before attaching an event handler. This builder pattern ensures that subscriptions are fully configured with metadata like priority, naming, and logging capabilities prior to being activated within the event bus infrastructure.

## API

### `Where`
```csharp
public PredicateSubscriptionBuilder<TEvent> Where(Func<TEvent, bool> predicate)
```
Adds a condition that must evaluate to `true` for the event handler to be invoked. Multiple calls to this method combine conditions using logical AND behavior.
*   **Parameters**: `predicate` - A function accepting the event instance and returning a boolean result.
*   **Returns**: The current builder instance to allow method chaining.
*   **Throws**: `ArgumentNullException` if `predicate` is null.

### `WhereNot`
```csharp
public PredicateSubscriptionBuilder<TEvent> WhereNot(Func<TEvent, bool> predicate)
```
Adds a negated condition; the handler will only be invoked if the provided predicate evaluates to `false`.
*   **Parameters**: `predicate` - A function accepting the event instance and returning a boolean result.
*   **Returns**: The current builder instance to allow method chaining.
*   **Throws**: `ArgumentNullException` if `predicate` is null.

### `WhereProperty<TProperty>`
```csharp
public PredicateSubscriptionBuilder<TEvent> WhereProperty<TProperty>(Expression<Func<TEvent, TProperty>> propertySelector, Func<TProperty, bool> comparison)
```
Filters events based on a specific property value. This method uses an expression tree to identify the property and a comparison function to validate its value.
*   **Parameters**: 
    *   `propertySelector` - An expression identifying the property on `TEvent` to inspect.
    *   `comparison` - A function defining the validation logic for the extracted property value.
*   **Returns**: The current builder instance to allow method chaining.
*   **Throws**: `ArgumentNullException` if either argument is null; `ArgumentException` if `propertySelector` does not represent a valid property access.

### `WherePropertyContains`
```csharp
public PredicateSubscriptionBuilder<TEvent> WherePropertyContains<TProperty>(Expression<Func<TEvent, TProperty>> propertySelector, TProperty value)
```
Filters events where a specific collection or string property contains a given value. This is a specialized shorthand for common containment checks.
*   **Parameters**: 
    *   `propertySelector` - An expression identifying the property on `TEvent`.
    *   `value` - The value to check for containment within the property.
*   **Returns**: The current builder instance to allow method chaining.
*   **Throws**: `ArgumentNullException` if `propertySelector` is null; runtime exceptions may occur if the property type does not support containment checks (e.g., lacks a `Contains` method).

### `WithHandler`
```csharp
public PredicateSubscriptionBuilder<TEvent> WithHandler(Action<TEvent> handler)
```
Assigns the primary action to be executed when an event matches all defined predicates.
*   **Parameters**: `handler` - The action to execute upon a successful match.
*   **Returns**: The current builder instance to allow method chaining.
*   **Throws**: `ArgumentNullException` if `handler` is null.

### `WithHandlerName`
```csharp
public PredicateSubscriptionBuilder<TEvent> WithHandlerName(string name)
```
Assigns a logical name to the subscription, useful for diagnostics, logging, or administrative identification within the event bus.
*   **Parameters**: `name` - The unique identifier or label for this subscription.
*   **Returns**: The current builder instance to allow method chaining.
*   **Throws**: `ArgumentNullException` if `name` is null or empty.

### `WithPriority`
```csharp
public PredicateSubscriptionBuilder<TEvent> WithPriority(int priority)
```
Sets the execution priority for this subscription. Higher values typically indicate higher precedence when multiple handlers match the same event.
*   **Parameters**: `priority` - An integer representing the subscription priority.
*   **Returns**: The current builder instance to allow method chaining.

### `WithLogger`
```csharp
public PredicateSubscriptionBuilder<TEvent> WithLogger(ILogger logger)
```
Associates a specific logger instance with this subscription to capture diagnostic information regarding predicate evaluation and handler execution.
*   **Parameters**: `logger` - The logger instance to use.
*   **Returns**: The current builder instance to allow method chaining.
*   **Throws**: `ArgumentNullException` if `logger` is null.

### `Register`
```csharp
public IDisposable Register()
```
Finalizes the configuration and subscribes the built predicate logic to the event bus.
*   **Parameters**: None.
*   **Returns**: An `IDisposable` token. Disposing of this token unsubscribes the handler from the event bus.
*   **Throws**: `InvalidOperationException` if `WithHandler` has not been called prior to registration.

## Usage

### Basic Filtering and Registration
The following example demonstrates creating a subscription for `OrderCreatedEvent` that only triggers for orders exceeding a specific monetary threshold, assigning it a high priority and a descriptive name.

```csharp
using var subscription = eventBus
    .ForEvent<OrderCreatedEvent>()
    .Where(e => e.TotalAmount > 1000.00m)
    .WithPriority(10)
    .WithHandlerName("HighValueOrderProcessor")
    .WithHandler(e => ProcessHighValueOrder(e))
    .Register();

// The subscription remains active until the 'using' block exits or Dispose() is called.
```

### Complex Property Predicates
This example illustrates the use of property-specific filtering and negation. It subscribes to `UserLoginEvent` instances where the `Region` property contains "EMEA" but explicitly excludes test accounts identified by a flag.

```csharp
var logger = loggerFactory.CreateLogger("UserLoginSubscription");

var subscription = eventBus
    .ForEvent<UserLoginEvent>()
    .WhereProperty(e => e.Region, r => r.Contains("EMEA"))
    .WhereNot(e => e.IsTestAccount)
    .WithLogger(logger)
    .WithHandler(e => SendRegionalNotification(e))
    .Register();

// The subscription persists until explicitly disposed.
```

## Notes

*   **Execution Order**: Predicates added via `Where`, `WhereNot`, and `WhereProperty` are evaluated in the order they are chained. Evaluation short-circuits; if any predicate returns `false` (or `true` for `WhereNot`), subsequent predicates and the handler are not executed.
*   **Handler Requirement**: Calling `Register()` without first invoking `WithHandler` will result in an `InvalidOperationException`. The builder requires a defined action to be useful.
*   **Thread Safety**: The `PredicateSubscriptionBuilder` itself is not thread-safe and should not be shared across threads during the configuration phase. However, the resulting `IDisposable` token returned by `Register()` is safe to dispose from any thread to unsubscribe.
*   **Expression Limitations**: When using `WhereProperty` or `WherePropertyContains`, the `propertySelector` expression must strictly be a member access expression. Complex expressions (e.g., method calls or arithmetic within the selector) will cause an `ArgumentException` at configuration time.
*   **Resource Management**: The object returned by `Register()` holds a reference to the subscription within the event bus. Failure to dispose of this object when the subscription is no longer needed may result in memory leaks or continued execution of handlers for events that are no longer relevant.
