# PredicateSubscriptionBuilderExtensions

Provides extension methods for building predicate-based event subscriptions in the dotnet-event-bus system. These methods allow filtering event subscriptions using property comparisons, type constraints, and custom predicates to control when event handlers are invoked.

## API

### `Where<TEvent>`

Adds a custom predicate to the subscription that determines whether the event handler should be invoked.

- **Parameters**:
  - `predicate`: A function that takes an event of type `TEvent` and returns `true` if the handler should be invoked.
- **Return value**: A `PredicateSubscriptionBuilder<TEvent>` for method chaining.
- **Throws**: `ArgumentNullException` if `predicate` is `null`.

### `WhereNot<TEvent>`

Adds a negated custom predicate to the subscription.

- **Parameters**:
  - `predicate`: A function that takes an event of type `TEvent` and returns `true` if the handler should **not** be invoked.
- **Return value**: A `PredicateSubscriptionBuilder<TEvent>` for method chaining.
- **Throws**: `ArgumentNullException` if `predicate` is `null`.

### `WhereTypeIs<TEvent, TDerived>`

Filters events to only those of a specific derived type.

- **Type parameters**:
  - `TEvent`: The base event type.
  - `TDerived`: The derived type to match.
- **Return value**: A `PredicateSubscriptionBuilder<TEvent>` for method chaining.
- **Throws**: No exceptions.

### `WhereTypeIsNot<TEvent, TExcluded>`

Excludes events of a specific derived type from the subscription.

- **Type parameters**:
  - `TEvent`: The base event type.
  - `TExcluded`: The derived type to exclude.
- **Return value**: A `PredicateSubscriptionBuilder<TEvent>` for method chaining.
- **Throws**: No exceptions.

### `WherePropertyIn<TEvent, TProperty>`

Filters events where a specified property value matches one of the provided values.

- **Type parameters**:
  - `TEvent`: The event type.
  - `TProperty`: The type of the property to compare.
- **Parameters**:
  - `propertySelector`: A function to select the property from the event.
  - `values`: The set of values to match against.
- **Return value**: A `PredicateSubscriptionBuilder<TEvent>` for method chaining.
- **Throws**: `ArgumentNullException` if `propertySelector` or `values` is `null`.

### `WherePropertyIsNull<TEvent, TProperty>`

Filters events where a specified property is `null`.

- **Type parameters**:
  - `TEvent`: The event type.
  - `TProperty`: The type of the property to check.
- **Parameters**:
  - `propertySelector`: A function to select the property from the event.
- **Return value**: A `PredicateSubscriptionBuilder<TEvent>` for method chaining.
- **Throws**: `ArgumentNullException` if `propertySelector` is `null`.

### `WherePropertyIsNotNull<TEvent, TProperty>`

Filters events where a specified property is not `null`.

- **Type parameters**:
  - `TEvent`: The event type.
  - `TProperty`: The type of the property to check.
- **Parameters**:
  - `propertySelector`: A function to select the property from the event.
- **Return value**: A `PredicateSubscriptionBuilder<TEvent>` for method chaining.
- **Throws**: `ArgumentNullException` if `propertySelector` is `null`.

### `WherePropertyMatches<TEvent>`

Adds a predicate that compares a property value using a custom matcher.

- **Type parameters**:
  - `TEvent`: The event type.
- **Parameters**:
  - `propertySelector`: A function to select the property from the event.
  - `matcher`: A function that takes the property value and returns `true` if it matches.
- **Return value**: A `PredicateSubscriptionBuilder<TEvent>` for method chaining.
- **Throws**: `ArgumentNullException` if `propertySelector` or `matcher` is `null`.

### `WherePropertyInRange<TEvent, TProperty>`

Filters events where a numeric property value falls within a specified range.

- **Type parameters**:
  - `TEvent`: The event type.
  - `TProperty`: The type of the property to compare (must implement `IComparable<TProperty>`).
- **Parameters**:
  - `propertySelector`: A function to select the property from the event.
  - `minValue`: The minimum value (inclusive).
  - `maxValue`: The maximum value (inclusive).
- **Return value**: A `PredicateSubscriptionBuilder<TEvent>` for method chaining.
- **Throws**:
  - `ArgumentNullException` if `propertySelector` is `null`.
  - `ArgumentOutOfRangeException` if `minValue` is greater than `maxValue`.

### `WithHandler<TEvent>`

Associates an event handler with the subscription.

- **Type parameters**:
  - `TEvent`: The event type.
- **Parameters**:
  - `handler`: The delegate to invoke when the predicate matches.
- **Return value**: A `PredicateSubscriptionBuilder<TEvent>` for method chaining.
- **Throws**: `ArgumentNullException` if `handler` is `null`.

### `WithLogger<TEvent>`

Associates a logger with the subscription for diagnostic purposes.

- **Type parameters**:
  - `TEvent`: The event type.
- **Parameters**:
  - `logger`: The logger instance to use.
- **Return value**: A `PredicateSubscriptionBuilder<TEvent>` for method chaining.
- **Throws**: `ArgumentNullException` if `logger` is `null`.

## Usage

### Example 1: Filtering by Property Value

```csharp
public class OrderPlacedEvent
{
    public string Status { get; set; }
    public decimal Amount { get; set; }
}

public class OrderHandler
{
    public void Handle(OrderPlacedEvent @event) => Console.WriteLine($"Order {@event.Status}");
}

// Subscribe to orders with status "Completed"
bus.Subscribe<OrderPlacedEvent>()
     .WherePropertyIn(e => e.Status, new[] { "Completed" })
     .WithHandler(new OrderHandler().Handle);
```

### Example 2: Combining Multiple Predicates

```csharp
public class SensorEvent
{
    public double Temperature { get; set; }
    public string Location { get; set; }
}

public class AlertService
{
    public void CheckTemperature(SensorEvent @event) =>
        Console.WriteLine($"High temperature alert at {@event.Location}");
}

// Subscribe to sensor events where temperature > 30 and location is "ServerRoom"
bus.Subscribe<SensorEvent>()
     .WherePropertyMatches(e => e.Temperature, t => t > 30)
     .WherePropertyIn(e => e.Location, new[] { "ServerRoom" })
     .WithHandler(new AlertService().CheckTemperature);
```

## Notes

- Predicates are evaluated in the order they are added to the subscription.
- Property-based filters (`WherePropertyIn`, `WherePropertyIsNull`, etc.) use reflection and may impact performance if overused. Cache compiled predicates where possible.
- Type-based filters (`WhereTypeIs`, `WhereTypeIsNot`) are resolved at subscription time and do not incur runtime reflection costs.
- The subscription builder is not thread-safe; ensure all modifications occur on the same thread or are synchronized externally.
- Predicates that throw exceptions during evaluation will propagate those exceptions to the event bus dispatcher. Validate predicates to avoid runtime failures.
