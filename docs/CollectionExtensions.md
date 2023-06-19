# CollectionExtensions

`CollectionExtensions` provides a set of static extension methods and utility types for working with collections, enumerables, and dictionaries. It includes batching, null/empty checks, safe first-element retrieval, iteration with side effects, distinct dictionary creation, grouping, set comparison, distinct-by-key filtering, random element selection, and pagination support through the nested `Page<T>` type.

## API

### `Batch<T>(this IEnumerable<T> source, int size)`

Splits the source sequence into batches of the specified size. Returns an `IEnumerable<IEnumerable<T>>` where each inner enumerable represents one batch. The last batch may contain fewer elements than `size`. Throws `ArgumentOutOfRangeException` if `size` is less than or equal to zero. Throws `ArgumentNullException` if `source` is null.

### `IsNullOrEmpty<T>(this IEnumerable<T>? source)`

Returns `true` if the source is `null` or contains no elements; otherwise `false`. Evaluates emptiness without fully enumerating if the source is a collection with a `Count` property, otherwise enumerates at least one element.

### `FirstOrDefaultValue<T>(this IEnumerable<T> source) where T : struct`

Returns the first element of the sequence as `T?`, or `null` if the sequence is empty. Unlike `FirstOrDefault`, the return type is explicitly nullable even when `T` is a non-nullable value type. Throws `ArgumentNullException` if `source` is null.

### `ForEach<T>(this IEnumerable<T> source, Action<T> action)`

Executes the given `action` on each element of the sequence. Returns the original sequence after full enumeration, enabling further chaining. Throws `ArgumentNullException` if `source` or `action` is null.

### `ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> asyncAction)`

Asynchronously executes `asyncAction` on each element sequentially (one at a time). Returns a `Task` that completes when all actions have finished. Throws `ArgumentNullException` if `source` or `asyncAction` is null. Exceptions thrown by individual actions propagate after the current action fails; subsequent elements are not processed.

### `ToDictionaryDistinct<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector)`

Creates a `Dictionary<TKey, TValue>` from the source sequence. If duplicate keys are encountered, only the first occurrence is kept; subsequent duplicates are silently ignored. Throws `ArgumentNullException` if `source`, `keySelector`, or `valueSelector` is null.

### `GroupByToDictionary<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)`

Groups elements by key and returns a `Dictionary<TKey, List<T>>` where each key maps to a list of all elements sharing that key. Unlike `ToDictionaryDistinct`, all elements are preserved. Throws `ArgumentNullException` if `source` or `keySelector` is null.

### `SetEquals<T>(this IEnumerable<T> first, IEnumerable<T> second)`

Determines whether two sequences contain the same set of elements, ignoring order and duplicate counts. Returns `true` if every element of `first` appears in `second` and vice versa. Uses default equality comparison. Throws `ArgumentNullException` if either sequence is null.

### `DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)`

Returns distinct elements from the source based on a key extracted by `keySelector`. The first element for each distinct key is retained; subsequent elements with the same key are omitted. Throws `ArgumentNullException` if `source` or `keySelector` is null.

### `GetRandom<T>(this IEnumerable<T> source)`

Returns a random element from the sequence as `T?`, or `null` if the sequence is empty. Uses a thread-local `Random` instance internally. Throws `ArgumentNullException` if `source` is null.

### `AsPages<T>(this IEnumerable<T> source, int pageSize)`

Partitions the source sequence into pages of the given size, returning an `IEnumerable<Page<T>>`. Each `Page<T>` contains the page number (1-based), the page size, and the items in that page. The last page may contain fewer items. Throws `ArgumentOutOfRangeException` if `pageSize` is less than or equal to zero. Throws `ArgumentNullException` if `source` is null.

### `Page<T>` (nested type)

Represents a single page of results.

- **`PageNumber`** (`int`): The 1-based index of this page within the overall sequence.
- **`PageSize`** (`int`): The maximum number of items this page can hold (equal to the requested page size for all but possibly the last page).
- **`Items`** (`List<T>`): The elements belonging to this page.

## Usage

### Example 1: Batching and Asynchronous Processing

```csharp
var orderIds = Enumerable.Range(1, 250);
var batches = orderIds.Batch(50);

foreach (var batch in batches)
{
    await batch.ForEachAsync(async id =>
    {
        await ProcessOrderAsync(id);
    });
}
```

### Example 2: Pagination with Distinct Keys

```csharp
var products = GetProducts(); // IEnumerable<Product>
var distinctByCategory = products.DistinctBy(p => p.CategoryId);

var pages = distinctByCategory.AsPages(10);
foreach (var page in pages)
{
    Console.WriteLine($"Page {page.PageNumber}: {page.Items.Count} items");
    foreach (var product in page.Items)
    {
        Console.WriteLine($"  {product.Name}");
    }
}
```

## Notes

- **Deferred execution**: Methods returning `IEnumerable<T>` (such as `Batch`, `DistinctBy`, `AsPages`) use deferred execution. The source sequence is not enumerated until the result is iterated. Multiple iterations will re-evaluate the source.
- **`ForEach` and `ForEachAsync`**: These methods eagerly enumerate the source. `ForEach` returns the source after enumeration, allowing chaining, but the enumeration has already occurred. `ForEachAsync` processes elements sequentially; for concurrent execution, consider `Task.WhenAll` with a projection instead.
- **`GetRandom` thread safety**: The internal `Random` instance is thread-local, so concurrent calls from different threads do not interfere. However, the method itself is not a cryptographically strong random source.
- **`SetEquals`**: This method builds hash sets from both sequences. For very large sequences, memory consumption may be significant. Duplicate counts are ignored — `[1,1,2]` and `[1,2]` are considered equal.
- **`ToDictionaryDistinct`**: Silently drops duplicates. If detection of duplicate keys is required, use standard LINQ `ToDictionary` which throws on duplicates.
- **`FirstOrDefaultValue`**: Designed for value types where `FirstOrDefault` returns a default value indistinguishable from a valid element (e.g., `0` for `int`). The nullable return type disambiguates "no element" from a legitimate default value.
- **`Page<T>`**: The `Items` list is materialized when each page is enumerated. Page numbering starts at 1, not 0.
