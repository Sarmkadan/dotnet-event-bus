# InMemoryEventCacheState

Represents the mutable state of an in‑memory event cache, exposing capacity counters, hit/miss metrics and JSON (de)serialization helpers.

## API

### `public required CacheStatistics Stats`
Provides access to detailed cache statistics.  
- **Purpose:** Holds metrics such as eviction counts, average latency, etc.  
- **Type:** `CacheStatistics` (get/set).  
- **Parameters:** None.  
- **Return value:** The current `CacheStatistics` instance.  
- **Exceptions:** None thrown by the property itself; assigning `null` is allowed but may cause downstream `NullReferenceException` when accessed.

### `public required int MaxCapacity`
Defines the upper bound of items the cache may store.  
- **Purpose:** Limits memory usage; when the cache exceeds this size, eviction policies are triggered.  
- **Type:** `int` (get/set).  
- **Parameters:** None.  
- **Return value:** The configured maximum capacity.  
- **Exceptions:** Setting a negative value does not throw from the property but will cause the cache to behave unpredictably; callers should validate ≥ 0 before assignment.

### `public required long TotalItems`
Counts the cumulative number of items that have been added to the cache since its creation.  
- **Purpose:** Useful for throughput monitoring.  
- **Type:** `long` (get/set).  
- **Parameters:** None.  
- **Return value:** The total items count.  
- **Exceptions:** None.

### `public required long Hits`
Records the number of successful cache look‑ups.  
- **Purpose:** Indicates how often requests were served from the cache.  
- **Type:** `long` (get/set).  
- **Parameters:** None.  
- **Return value:** The hit count.  
- **Exceptions:** None.

### `public required long Misses`
Records the number of failed cache look‑ups.  
- **Purpose:** Indicates how often the cache had to retrieve data from the underlying source.  
- **Type:** `long` (get/set).  
- **Parameters:** None.  
- **Return value:** The miss count.  
- **Exceptions:** None.

### `public static string ToJson()`
Serializes the current state of an `InMemoryEventCacheState` instance to a JSON string.  
- **Purpose:** Enables persistence or transmission of cache metrics.  
- **Parameters:** None.  
- **Return value:** A JSON‑encoded string representing the instance’s property values.  
- **Exceptions:** Throws `JsonException` if serialization fails (e.g., due to circular references or unsupported types within `CacheStatistics`).

### `public static InMemoryEventCache? FromJson()`
Deserializes a JSON payload into an `InMemoryEventCache` instance.  
- **Purpose:** Reconstructs a cache object from its JSON representation.  
- **Parameters:** None.  
- **Return value:** An `InMemoryEventCache` populated with the deserialized data, or `null` if the input does not represent a valid cache state.  
- **Exceptions:** Throws `JsonException` if the JSON is malformed; returns `null` for valid JSON that does not map to an `InMemoryEventCache`.

### `public static bool TryFromJson()`
Attempts to deserialize a JSON payload into an `InMemoryEventCache` without throwing exceptions.  
- **Purpose:** Provides a safe fallback for invalid or unexpected JSON.  
- **Parameters:** None.  
- **Return value:** `true` if deserialization succeeded and an `InMemoryEventCache` was produced; otherwise `false`.  
- **Exceptions:** None; any internal parsing errors are caught and result in a `false` return.

## Usage

```csharp
// Create and configure cache state
var state = new InMemoryEventCacheState
{
    Stats      = new CacheStatistics { /* initialize as needed */ },
    MaxCapacity= 10_000,
    TotalItems = 0,
    Hits       = 0,
    Misses     = 0
};

// Serialize state to JSON for logging or storage
string json = InMemoryEventCacheState.ToJson();
// json now contains something like:
// {"Stats":{...},"MaxCapacity":10000,"TotalItems":0,"Hits":0,"Misses":0}

// Deserialize back into a cache object (null if JSON is invalid)
InMemoryEventCache? cache = InMemoryEventCacheState.FromJson();
if (cache != null)
{
    // Use the restored cache...
}

// Safe deserialization that never throws
bool ok = InMemoryEventCacheState.TryFromJson();
if (ok)
{
    // Deserialization succeeded; retrieve result via an out parameter if the API provides one
}
else
{
    // Handle invalid JSON gracefully
}
```

```csharp
// Updating metrics during operation
state.Hits++;          // cache hit
state.TotalItems++;    // new item added
if (state.TotalItems > state.MaxCapacity)
{
    // Trigger eviction logic elsewhere
}

// Periodically export metrics
string metricsJson = InMemoryEventCacheState.ToJson();
File.WriteAllText("cache-metrics.json", metricsJson);
```

## Notes

- All properties are `required`; an instance must have each property assigned before it is first used. Failure to do so results in a compile‑time error.
- The type itself does not enforce thread safety. Concurrent reads and writes to `Stats`, `MaxCapacity`, `TotalItems`, `Hits`, or `Misses` can lead to race conditions. External synchronization (e.g., `lock` or `Interlocked` operations) is required when the instance is accessed from multiple threads.
- The static JSON methods do not retain any internal state; they are thread‑safe with respect to each other because they operate solely on the data supplied via the implicit instance context (or input JSON). However, if the instance being serialized is mutated concurrently, the resulting JSON may reflect an inconsistent snapshot.
- Setting `MaxCapacity` to a negative value is not prevented by the property; such a value will cause undefined behavior in the cache implementation. Callers should validate the value prior to assignment.
- `FromJson` returns `null` for JSON that does not correspond to an `InMemoryEventCache` instance; callers should check for null before using the result.
- `TryFromJson` swallows all exceptions and reports success via its Boolean return; detailed error information is not available through this method. Use `FromJson` when diagnostic details are needed.
