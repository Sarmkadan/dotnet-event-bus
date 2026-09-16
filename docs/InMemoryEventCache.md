# InMemoryEventCache

`src/DotnetEventBus/Caching/InMemoryEventCache.cs`

In-memory implementation of [`IEventCache`](src/DotnetEventBus/Caching/IEventCache.cs).
It provides fast, local, thread-safe caching for frequently retrieved events and
subscriptions without any external dependency, making it suitable for
single-instance deployments.

## Overview

- **Thread-safe** — backed by a `ConcurrentDictionary<string, CacheEntry>`.
- **Automatic expiration** — entries can carry a `TimeSpan` TTL; expired entries
  are removed lazily on access and by a background cleanup loop.
- **LRU eviction** — when the cache reaches its configured capacity, the least
  recently accessed entry is evicted before a new one is inserted.
- **Observability** — hit/miss/eviction counters and a memory-usage estimate are
  exposed through `GetStatsAsync()`.

The class is `sealed` and implements `IDisposable` to stop the background cleanup
task.

## Constructor

```csharp
public InMemoryEventCache(int maxCapacity = 10000)
```

| Parameter     | Default | Description                                                |
| ------------- | ------- | ---------------------------------------------------------- |
| `maxCapacity` | `10000` | Maximum number of items kept before LRU eviction kicks in. |

The constructor starts a background task that runs every minute and calls
`CleanupExpiredEntries()` to remove expired entries. The loop is cancelled on
`Dispose()`.

## Capacity & LRU eviction

`Capacity` exposes the configured `maxCapacity`.

On every `SetAsync`, the cache checks whether `_cache.Count >= _maxCapacity`. If
so, it calls `EvictOldest()`, which removes the entry with the smallest
`LastAccessed` timestamp (the least recently used entry) and increments the
`_evictions` counter. This keeps the cache bounded at the configured capacity.

`LastAccessed` is refreshed to `DateTime.UtcNow` on every successful `GetAsync`
hit, so frequently used entries are retained while idle ones are evicted first.

## Statistics

`GetStatsAsync()` returns a `CacheStats` snapshot:

| Field              | Type     | Description                                                       |
| ------------------ | -------- | ----------------------------------------------------------------- |
| `Hits`             | `long`   | Number of successful lookups (non-expired entry found).           |
| `Misses`           | `long`   | Number of lookups that found nothing or an expired entry.         |
| `Evictions`        | `long`   | Number of entries evicted by LRU capacity pressure.               |
| `TotalItems`       | `int`    | Current number of entries in the cache.                           |
| `TotalMemoryBytes` | `long`   | Estimated managed memory footprint of the cache.                  |
| `HitRate` (derived)| `double` | `Hits / (Hits + Misses)`, or `0` when no lookups have occurred.   |

`Hits`, `Misses`, and `Evictions` are monotonic counters guarded by a lock and
are never reset by `ClearAsync()`.

### Memory estimation

`EstimateMemoryUsage()` approximates the managed footprint per entry:

- A fixed per-entry overhead of `88` bytes (dictionary bucket + entry object).
- UTF-16 key characters (`key.Length * sizeof(char)`).
- A payload estimate based on value type:
  - `string` — `length * sizeof(char) + 26`
  - `byte[]` — `LongLength + 24`
  - anything else — a flat `64` bytes.

This is an estimate, not an exact measurement.

## API

| Method | Description |
| ------ | ----------- |
| `GetAsync<T>(string key)` | Returns the cached value, or `null` when absent or expired. Expired entries are removed and counted as a miss. Refreshes `LastAccessed` on a hit. |
| `SetAsync<T>(string key, T value, TimeSpan? expiration = null)` | Stores a value with an optional TTL. Evicts the LRU entry first if at capacity. |
| `RemoveAsync(string key)` | Removes a single entry. |
| `ExistsAsync(string key)` | Returns `true` for a present, non-expired entry; otherwise `false` (removing an expired entry if found). |
| `GetManyAsync<T>(IEnumerable<string> keys)` | Returns a dictionary of present, non-expired values. |
| `RemoveManyAsync(IEnumerable<string> keys)` | Removes multiple entries. |
| `ClearAsync()` | Removes all entries. |
| `GetStatsAsync()` | Returns a `CacheStats` snapshot. |
| `ToString()` | Returns a summary of the most recently created entry (value, `CreatedAt`, `ExpiresAt`). |

All methods are `async` and begin with `await Task.Yield()` to preserve the async
contract while performing in-memory work synchronously.

## Expiration

Each `CacheEntry` records `CreatedAt`, `LastAccessed`, and an optional
`ExpiresAt`. An entry is considered expired when `ExpiresAt` is set and the
current UTC time is past it (`IsExpired`). Expired entries are removed:

- lazily, when touched by `GetAsync` / `ExistsAsync` / `GetManyAsync`, and
- periodically, by the background cleanup loop every minute.

## Thread safety

The backing dictionary is a `ConcurrentDictionary`, so concurrent reads and
writes are safe. The statistics counters are updated under `_statsLock` to keep
`Hits`/`Misses`/`Evictions` consistent. `Dispose()` is idempotent and waits up to
one second for the cleanup task to stop.

## Disposal

`Dispose()` cancels the background cleanup loop, waits up to one second for it to
finish, and disposes the `CancellationTokenSource`. It is safe to call multiple
times.