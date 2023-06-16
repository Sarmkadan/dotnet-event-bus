# IEventCache

`IEventCache` defines a minimal contract for tracking cache statistics and memory usage in an event bus system. It exposes counters for cache hits and misses, the total number of cached items, and an estimate of the memory consumed by those items. This interface is intended for monitoring and diagnostics rather than cache manipulation.

## API

### `Hits`
- **Purpose**: Tracks the total number of successful cache lookups (cache hits).
- **Type**: `long`
- **Return Value**: The current count of hits.
- **Thread Safety**: Safe for concurrent reads; updates are expected to be atomic or externally synchronized.

### `Misses`
- **Purpose**: Tracks the total number of failed cache lookups (cache misses).
- **Type**: `long`
- **Return Value**: The current count of misses.
- **Thread Safety**: Safe for concurrent reads; updates are expected to be atomic or externally synchronized.

### `TotalItems`
- **Purpose**: Provides the total number of items currently stored in the cache.
- **Type**: `int`
- **Return Value**: The current count of items.
- **Thread Safety**: Safe for concurrent reads; updates are expected to be atomic or externally synchronized.

### `TotalMemoryBytes`
- **Purpose**: Estimates the total memory consumed by all cached items, in bytes.
- **Type**: `long`
- **Return Value**: The estimated memory usage.
- **Thread Safety**: Safe for concurrent reads; updates are expected to be atomic or externally synchronized.

## Usage
