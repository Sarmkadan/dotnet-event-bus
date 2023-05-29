# EventFilter
The `EventFilter` class is a crucial component in the `dotnet-event-bus` project, designed to filter events based on specific conditions. It provides a flexible and expressive way to define filters, allowing for precise control over which events are processed. By utilizing the `EventFilter` class, developers can efficiently manage event handling and routing in their applications.

## API
### Instance Members
* `Where`: Applies a filter to the event based on a given condition. Returns the `EventFilter` instance for chaining.
* `WhereProperty<TProperty>`: Filters events based on a specific property of type `TProperty`. Returns the `EventFilter` instance for chaining.
* `WherePropertyInRange<TProperty>`: Filters events based on a property of type `TProperty` being within a specified range. Returns the `EventFilter` instance for chaining.
* `WherePropertyContains`: Filters events based on a property containing a specified value. Returns the `EventFilter` instance for chaining.
* `Not`: Negates the current filter, inverting its logic. Returns the `EventFilter` instance for chaining.
* `Matches`: Gets a boolean indicating whether the filter matches the given event.
* `FilterCollection`: Gets an enumerable collection of events that pass the filter.
* `Clear`: Resets the filter to its initial state.

### Static Members
* `CreateFilter<T>`: Creates a new instance of `EventFilter<T>`.
* `CreateWildcardFilter<T>`: Creates a new instance of `EventFilter<T>` that matches all events of type `T`.
* `CreateEmptyFilter<T>`: Creates a new instance of `EventFilter<T>` that does not match any events.

## Usage
The following examples demonstrate how to utilize the `EventFilter` class:
```csharp
// Example 1: Filtering events based on a specific property
var filter = EventFilter.CreateFilter<MyEvent>()
    .WhereProperty<MyEvent>(e => e.Id > 10);
var events = GetEvents();
var filteredEvents = filter.FilterCollection;
foreach (var e in filteredEvents)
{
    Console.WriteLine(e.Id);
}

// Example 2: Creating a wildcard filter and negating it
var wildcardFilter = EventFilter.CreateWildcardFilter<MyEvent>();
var negatedFilter = wildcardFilter.Not;
var eventsToProcess = negatedFilter.FilterCollection;
foreach (var e in eventsToProcess)
{
    Console.WriteLine($"Processing event {e.Id}");
}
```

## Notes
When using the `EventFilter` class, consider the following:
* The `Clear` method resets the filter to its initial state, removing all previously applied conditions.
* The `Matches` property evaluates the filter against a given event, returning `true` if the event passes the filter and `false` otherwise.
* The `FilterCollection` property returns an enumerable collection of events that pass the filter, allowing for efficient processing of filtered events.
* The `EventFilter` class is not thread-safe by default. If used in a multi-threaded environment, consider implementing synchronization mechanisms to ensure thread safety.
* The `WherePropertyInRange<TProperty>` and `WherePropertyContains` methods may throw exceptions if the specified property does not exist or is not of the expected type. Always validate property existence and types before applying these filters.
