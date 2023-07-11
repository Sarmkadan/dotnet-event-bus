# InMemoryRepositoryExtensions

Extension methods that provide asynchronous LINQ-like query capabilities over in-memory collections used as repositories in the `dotnet-event-bus` project. These methods enable fluent querying of in-memory data structures without requiring a full database provider, while maintaining an asynchronous API consistent with Entity Framework Core patterns.

## API

### `FirstOrDefaultAsync<T>`

Retrieves the first element of a sequence or a default value if the sequence contains no elements.

- **Parameters**:
  - `IEnumerable<T> source`: The sequence to search.
  - `Func<T, bool> predicate`: A function to test each element for a condition.
- **Return value**: `Task<T?>`
  A task that represents the asynchronous operation. The task result contains the first element in the sequence that satisfies the condition, or `default(T)` if no such element is found.
- **Exceptions**: Throws `ArgumentNullException` if `source` or `predicate` is `null`.

### `WhereAsync<T>`

Filters a sequence of values based on a predicate.

- **Parameters**:
  - `IEnumerable<T> source`: The sequence to filter.
  - `Func<T, bool> predicate`: A function to test each element for a condition.
- **Return value**: `Task<IEnumerable<T>>`
  A task that represents the asynchronous operation. The task result contains a new enumerable sequence of elements that satisfy the predicate.
- **Exceptions**: Throws `ArgumentNullException` if `source` or `predicate` is `null`.

### `AnyAsync<T>`

Determines whether any element of a sequence satisfies a condition.

- **Parameters**:
  - `IEnumerable<T> source`: The sequence to check.
  - `Func<T, bool> predicate`: A function to test each element for a condition.
- **Return value**: `Task<bool>`
  A task that represents the asynchronous operation. The task result is `true` if any elements in the source sequence pass the test in the specified predicate; otherwise, `false`.
- **Exceptions**: Throws `ArgumentNullException` if `source` or `predicate` is `null`.

### `GetPagedAsync<T>`

Returns a paginated subset of a sequence with total count metadata.

- **Parameters**:
  - `IEnumerable<T> source`: The sequence to paginate.
  - `int pageNumber`: The one-based index of the page to retrieve.
  - `int pageSize`: The number of items per page.
  - `Func<T, object>? orderBy`: An optional function to extract the key used for ordering the results.
  - `bool ascending`: Whether to sort in ascending order; ignored if `orderBy` is `null`.
- **Return value**: `Task<PaginatedResult<T>>`
  A task that represents the asynchronous operation. The task result contains a `PaginatedResult<T>` with the page of items and total count metadata.
- **Exceptions**:
  - Throws `ArgumentNullException` if `source` is `null`.
  - Throws `ArgumentOutOfRangeException` if `pageNumber` is less than 1 or `pageSize` is less than 1.

## Usage

```csharp
// Example 1: Filtering and retrieving a single item
var orders = new List<Order> { /* ... */ };
var firstPending = await InMemoryRepositoryExtensions.FirstOrDefaultAsync(orders, o => o.Status == OrderStatus.Pending);

if (firstPending != null)
{
    Console.WriteLine($"Found pending order: {firstPending.Id}");
}

// Example 2: Paginated query with ordering
var paginated = await InMemoryRepositoryExtensions.GetPagedAsync(
    orders,
    pageNumber: 2,
    pageSize: 10,
    orderBy: o => o.CreatedAt,
    ascending: false);

Console.WriteLine($"Page 2 of {paginated.TotalPages} pages, {paginated.TotalCount} total orders");
```

## Notes

- All methods are thread-safe for concurrent reads. Since the underlying `IEnumerable<T>` is not modified by these methods, no additional synchronization is required when multiple threads read the same collection concurrently.
- `GetPagedAsync` performs in-memory sorting and pagination. For large datasets, consider using a dedicated database provider to avoid performance issues.
- The `orderBy` parameter in `GetPagedAsync` uses `object` as the key type to support both value and reference types; ensure the returned key is consistently comparable.
- These methods do not support cancellation. If cancellation is required, wrap calls in a `Task` with a `CancellationToken` at the call site.
