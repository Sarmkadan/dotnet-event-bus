# IRepository

The `IRepository` type represents a paginated collection of items. It is typically returned by data access methods that support paging, providing the current page of results along with metadata about the total dataset. The type is generic over the item type `T`.

## API

### `List<T> Items`

The list of items belonging to the current page. This property is read/write. When set, the provided list is stored directly; no defensive copy is made. The list may be `null` if no items are present, though callers should treat a `null` value as an empty collection.

### `int PageNumber`

The one‑based index of the current page. A value of `1` indicates the first page. This property is read/write. Negative values or zero are not validated by the type itself, but consumers should ensure the value is at least `1`.

### `int PageSize`

The maximum number of items per page. This property is read/write. A value of `0` or less is not validated, but it is expected to be a positive integer. The actual number of items in `Items` may be less than `PageSize` when the page is the last one.

### `int TotalCount`

The total number of items across all pages. This property is read/write. It should be greater than or equal to zero. No validation is performed by the type.

## Usage

The following example creates an `IRepository<string>` instance and populates it with the first page of a search result.

```csharp
var page = new IRepository<string>
{
    Items = new List<string> { "apple", "banana", "cherry" },
    PageNumber = 1,
    PageSize = 10,
    TotalCount = 3
};
```

The next example demonstrates how a repository method might return an `IRepository<T>` after querying a database.

```csharp
public IRepository<Product> GetProducts(int pageNumber, int pageSize)
{
    var allProducts = _context.Products.ToList();
    var totalCount = allProducts.Count;
    var pagedItems = allProducts
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToList();

    return new IRepository<Product>
    {
        Items = pagedItems,
        PageNumber = pageNumber,
        PageSize = pageSize,
        TotalCount = totalCount
    };
}
```

## Notes

- **Edge cases**: When `TotalCount` is zero, `Items` is typically empty and `PageNumber` should be `1`. If `PageNumber` exceeds the last page (i.e., `(PageNumber - 1) * PageSize >= TotalCount`), `Items` will be empty. The type does not enforce these invariants; callers are responsible for setting consistent values.
- **Thread safety**: Instances of `IRepository` are not thread‑safe. Concurrent reads and writes to `Items`, `PageNumber`, `PageSize`, or `TotalCount` can result in inconsistent state. If shared across threads, external synchronization (e.g., a lock) is required. The `List<T>` stored in `Items` is also not thread‑safe.
