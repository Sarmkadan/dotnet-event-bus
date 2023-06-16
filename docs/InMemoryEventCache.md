# InMemoryEventCache

`InMemoryEventCache` provides an in-process, dictionary-backed cache for storing serialized event payloads and associated metadata. It is designed for short-lived caching scenarios where distributed persistence is not required, offering type-safe get/set operations with optional expiration and basic statistical tracking.

## API

### `public InMemoryEventCache()`
Initializes a new instance of the cache with empty internal storage and zeroed statistics. No external dependencies or configuration are required.

### `public async Task<T?> GetAsync<T>(string key)`
Retrieves a cached value by its unique key and deserializes it to the requested type.

- **Parameters:** `key` — the string identifier under which the value was stored.
- **Returns:** the deserialized value of type `T`, or `null` if the key is not found or the entry has expired.
- **Exceptions:** throws `ArgumentNullException` when `key` is null; throws `InvalidCastException` or `JsonException` if the stored payload cannot be deserialized to `T`.

### `public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)`
Stores a value in the cache under the specified key, optionally with a time-to-live duration.

- **Parameters:**
  - `key` — the string identifier for the cached entry.
  - `value` — the object to serialize and store.
  - `ttl` — optional `TimeSpan` after which the entry is considered expired and will be ignored by `GetAsync` and `ExistsAsync`.
- **Returns:** a completed task.
- **Exceptions:** throws `ArgumentNullException` when `key` or `value` is null.

### `public async Task RemoveAsync(string key)`
Removes a single entry from the cache by its key. If the key does not exist, the operation completes without error.

- **Parameters:** `key` — the string identifier of the entry to remove.
- **Returns:** a completed task.
- **Exceptions:** throws `ArgumentNullException` when `key` is null.

### `public async Task<bool> ExistsAsync(string key)`
Checks whether a non-expired entry exists for the given key.

- **Parameters:** `key` — the string identifier to check.
- **Returns:** `true` if the key exists and its expiration time (if set) has not passed; `false` otherwise.
- **Exceptions:** throws `ArgumentNullException` when `key` is null.

### `public async Task<Dictionary<string, T>> GetManyAsync<T>(IEnumerable<string> keys)`
Retrieves multiple entries at once, returning only those that exist and have not expired.

- **Parameters:** `keys` — a collection of string identifiers to fetch.
- **Returns:** a dictionary mapping each found key to its deserialized value of type `T`. Keys that are missing or expired are omitted from the result.
- **Exceptions:** throws `ArgumentNullException` when `keys` is null; individual deserialization failures for a key cause that key to be excluded rather than throwing.

### `public async Task RemoveManyAsync(IEnumerable<string> keys)`
Removes all specified entries from the cache. Missing keys are silently ignored.

- **Parameters:** `keys` — a collection of string identifiers to remove.
- **Returns:** a completed task.
- **Exceptions:** throws `ArgumentNullException` when `keys` is null.

### `public async Task ClearAsync()`
Removes every entry from the cache and resets all statistics counters to zero.

- **Returns:** a completed task.

### `public async Task<CacheStats> GetStatsAsync()`
Returns a snapshot of current cache statistics.

- **Returns:** a `CacheStats` object containing:
  - `public required object Value` — the total number of hits (successful retrievals).
  - `public DateTime CreatedAt` — the timestamp when the cache instance was created.
  - `public DateTime? ExpiresAt` — always `null` for the cache itself (instance-level expiration is not supported; this field is reserved for future use or per-entry reflection).

## Usage

### Example 1: Cache an event payload and retrieve it
```csharp
var cache = new InMemoryEventCache();

var orderEvent = new OrderPlaced { OrderId = Guid.NewGuid(), Amount = 99.95m };
await cache.SetAsync("event:order:123", orderEvent, TimeSpan.FromMinutes(5));

// Later in the pipeline
var cached = await cache.GetAsync<OrderPlaced>("event:order:123");
if (cached is not null)
{
    Console.WriteLine($"Replaying order {cached.OrderId} with amount {cached.Amount}");
}
```

### Example 2: Batch check and eviction of stale correlation IDs
```csharp
var cache = new InMemoryEventCache();
var correlationIds = new[] { "corr:a", "corr:b", "corr:c" };

// Store several entries
foreach (var id in correlationIds)
{
    await cache.SetAsync(id, new CorrelationRecord { ProcessedAt = DateTime.UtcNow }, TimeSpan.FromSeconds(30));
}

// Later, fetch only those still valid
var stillValid = await cache.GetManyAsync<CorrelationRecord>(correlationIds);
Console.WriteLine($"{stillValid.Count} of {correlationIds.Length} correlations still active");

// Clean up processed ones
var toRemove = correlationIds.Except(stillValid.Keys);
await cache.RemoveManyAsync(toRemove);
```

## Notes

- **Expiration is lazy:** entries with a TTL are not proactively evicted. They are checked at retrieval time (`GetAsync`, `GetManyAsync`, `ExistsAsync`) and treated as missing if expired. Expired entries remain in internal storage until explicitly removed or cleared.
- **Thread safety:** all public methods are asynchronous and return tasks, but the underlying dictionary operations are not guarded by a synchronization primitive unless the implementation internally uses locks or concurrent collections. Callers should avoid concurrent mutations from multiple threads unless the implementation guarantees safety.
- **Statistics:** the `CacheStats.Value` field tracks cumulative hits. A hit is counted each time `GetAsync` or `GetManyAsync` successfully returns a non-expired entry. Misses, removals, and clears do not decrement the counter.
- **Type safety:** `GetAsync<T>` and `GetManyAsync<T>` perform deserialization at call time. Storing incompatible types under the same key and retrieving with a different type parameter will cause deserialization errors.
- **Null keys:** all methods accepting a `key` or `keys` parameter throw `ArgumentNullException` for null arguments. Empty strings are allowed as keys but may lead to collisions if used unintentionally.
