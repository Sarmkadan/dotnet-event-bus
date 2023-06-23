# TestFilterEvent

Represents a test event used to validate the filtering capabilities of the `dotnet-event-bus`. It contains sample event data fields and a suite of test methods that verify the behavior of filter predicates, property matching, range queries, string containment, negation, and combined filters. This class is intended for use in unit tests to ensure the event bus filtering system works correctly under various conditions.

## API

### Properties

| Member | Type | Description |
|--------|------|-------------|
| `OrderId` | `int` | The unique identifier of the order. |
| `Amount` | `decimal` | The monetary value associated with the event. |
| `Status` | `string` | The current status of the order (e.g., "Pending", "Shipped"). |
| `Region` | `string?` | An optional geographic region for the order. May be `null`. |

### Methods

All methods return `void` and are designed to be invoked by a test runner. They assert expected filtering behavior and may throw an exception (typically an assertion failure) if the test condition is not met.

| Method | Description |
|--------|-------------|
| `Where_WithMatchingPredicate_ShouldMatch` | Verifies that a filter predicate matching the event data returns `true`. |
| `Where_WithMultiplePredicates_AllMustMatch` | Verifies that multiple predicates combined with logical AND all match the event. |
| `Where_WithNullPredicate_ShouldThrowArgumentNullException` | Asserts that passing a `null` predicate to a `Where` operation throws `ArgumentNullException`. |
| `WhereProperty_WithMatchingValue_ShouldMatch` | Verifies that a property filter with an exact matching value returns `true`. |
| `WhereProperty_WithNumericValue_ShouldMatch` | Verifies that a numeric property filter (e.g., `Amount`) matches when the value equals the expected number. |
| `WhereProperty_WithNullProperty_ShouldNotMatch` | Verifies that a property filter on a `null` property (e.g., `Region`) does not match. |
| `WherePropertyInRange_WithValueInRange_ShouldMatch` | Verifies that a range filter on a numeric property matches when the value falls within the specified range. |
| `WherePropertyInRange_WithBoundaryValues_ShouldInclude` | Verifies that range filters include boundary values (inclusive). |
| `WherePropertyContains_WithCaseInsensitiveMatch_ShouldMatch` | Verifies that a string containment filter matches regardless of case. |
| `WherePropertyContains_WithPartialMatch_ShouldMatch` | Verifies that a string containment filter matches on a substring. |
| `WherePropertyContains_WithNullProperty_ShouldNotThrow` | Verifies that applying a containment filter on a `null` property does not throw an exception. |
| `Not_ShouldInvertPredicate` | Verifies that the `Not` operator correctly inverts a predicate’s result. |
| `Complex_FilterCombination_AllPredicatesMustMatch` | Verifies that a combination of multiple filter types (e.g., `Where`, `WhereProperty`, `WherePropertyInRange`) all match simultaneously. |
| `Where_ShouldAllowChaining` | Verifies that filter methods can be chained fluently without side effects. |
| `Matches_WithEmptyFilterSet_ShouldMatchAll` | Verifies that an empty filter set matches any event (i.e., no filtering applied). |

## Usage

The following examples demonstrate how to instantiate `TestFilterEvent` and use its members in a test context.

### Example 1: Creating an event and verifying property values

```csharp
using dotnet_event_bus;

var testEvent = new TestFilterEvent
{
    OrderId = 42,
    Amount = 99.99m,
    Status = "Shipped",
    Region = "North America"
};

// Properties are accessible for inspection or further filtering
Console.WriteLine($"Order {testEvent.OrderId} has status {testEvent.Status}");
```

### Example 2: Running a test method to validate filtering behavior

```csharp
using dotnet_event_bus;
using Xunit;

public class FilterTests
{
    [Fact]
    public void Test_Where_WithMatchingPredicate()
    {
        var testEvent = new TestFilterEvent();
        // This method will throw if the predicate does not match
        testEvent.Where_WithMatchingPredicate_ShouldMatch();
    }

    [Fact]
    public void Test_WhereProperty_WithNumericValue()
    {
        var testEvent = new TestFilterEvent();
        testEvent.WhereProperty_WithNumericValue_ShouldMatch();
    }
}
```

## Notes

- **Edge Cases**:  
  - The `Region` property is nullable; methods such as `WhereProperty_WithNullProperty_ShouldNotMatch` and `WherePropertyContains_WithNullProperty_ShouldNotThrow` explicitly test behavior when this property is `null`.  
  - Range filters (`WherePropertyInRange_WithBoundaryValues_ShouldInclude`) treat boundaries as inclusive.  
  - String containment (`WherePropertyContains_WithCaseInsensitiveMatch_ShouldMatch`) is case-insensitive.

- **Thread Safety**:  
  `TestFilterEvent` is not thread-safe. Its properties are mutable and intended for single-threaded test execution. Concurrent reads or writes to the same instance may produce inconsistent results. Each test method should operate on its own instance or ensure exclusive access.

- **Exception Handling**:  
  Methods that test for exceptions (e.g., `Where_WithNullPredicate_ShouldThrowArgumentNullException`) rely on the test framework to catch the expected exception. If the exception is not thrown, the method will fail with an assertion error.
