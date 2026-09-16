#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetEventBus.Caching;

/// <summary>
/// In-memory implementation of the event cache.
/// Uses concurrent dictionary for thread-safe access and automatic expiration.
/// Why: Provides fast, local caching without external dependencies for single-instance deployments.
/// </summary>
public sealed class InMemoryEventCache : IEventCache, IDisposable
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = [];
    private readonly object _statsLock = new();
    private readonly CancellationTokenSource _cleanupCts = new();
    private readonly Task _cleanupTask;
    private int _disposed;
    private long _hits = 0;
    private long _misses = 0;
    private long _evictions = 0;

    /// <summary>
    /// Maximum number of items to keep in cache before eviction.
    /// </summary>
    private readonly int _maxCapacity;

    /// <summary>
    /// Gets the maximum capacity of the cache.
    /// </summary>
    public int Capacity => _maxCapacity;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryEventCache"/> class.
    /// </summary>
    /// <param name="maxCapacity">Maximum number of items to keep in cache before LRU eviction. Defaults to 10000.</param>
    public InMemoryEventCache(int maxCapacity = 10000)
    {
        _maxCapacity = maxCapacity;

        // Start cleanup task that runs every minute until disposed.
        var cleanupToken = _cleanupCts.Token;
        _cleanupTask = Task.Run(async () =>
        {
            while (!cleanupToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), cleanupToken);
                    CleanupExpiredEntries();
                }
                catch (OperationCanceledException) when (cleanupToken.IsCancellationRequested)
                {
                    // Expected on dispose.
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Trace.TraceError(
                        $"Failed to clean up expired cache entries: {exception}");
                }
            }
        });
    }

    /// <summary>
    /// Stops the background cleanup loop.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _cleanupCts.Cancel();
        Task.WaitAny([_cleanupTask], TimeSpan.FromSeconds(1));
        _cleanupCts.Dispose();
    }

    /// <summary>
    /// Returns a concise summary of the cache, surfacing the most recent
    /// entry's value along with its creation and expiration timestamps.
    /// </summary>
    public override string ToString()
    {
        var newest = _cache.Values
            .OrderByDescending(entry => entry.CreatedAt)
            .FirstOrDefault();

        return $"InMemoryEventCache {{ Value = {newest?.Value}, CreatedAt = {newest?.CreatedAt:O}, ExpiresAt = {newest?.ExpiresAt:O} }}";
    }

    /// <summary>
    /// Retrieves the value associated with the specified key, or <c>null</c> if
    /// the key is absent or its entry has expired.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key to look up.</param>
    /// <returns>The cached value, or <c>null</c> when not found or expired.</returns>
    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        ArgumentNullException.ThrowIfNull(key);

        await Task.Yield(); // Keep async contract

        if (_cache.TryGetValue(key, out var entry))
        {
            // Check if expired
            if (entry.IsExpired)
            {
                _cache.TryRemove(key, out _);
                RecordMiss();
                return null;
            }

            // Update last accessed time for LRU
            entry.LastAccessed = DateTime.UtcNow;
            RecordHit();
            return entry.Value as T;
        }

        RecordMiss();
        return null;
    }

    /// <summary>
    /// Stores the specified value in the cache with the given key and optional expiration.
    /// If the cache is at capacity, the least recently used item will be evicted.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">Optional expiration time. If null, the entry does not expire.</param>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        await Task.Yield(); // Keep async contract

        // Check capacity and evict if necessary
        if (_cache.Count >= _maxCapacity)
        {
            EvictOldest();
        }

        var now = DateTime.UtcNow;
        var entry = new CacheEntry
        {
            Value = value,
            CreatedAt = now,
            LastAccessed = now,
            ExpiresAt = expiration.HasValue ? now.Add(expiration.Value) : null
        };

        _cache[key] = entry;
    }

    /// <summary>
    /// Removes the entry associated with the specified key from the cache.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    public async Task RemoveAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        await Task.Yield(); // Keep async contract
        _cache.TryRemove(key, out _);
    }

    /// <summary>
    /// Determines whether the cache contains a non-expired entry for the specified key.
    /// </summary>
    /// <param name="key">The cache key to check.</param>
    /// <returns><c>true</c> if a non-expired entry exists; otherwise, <c>false</c>.</returns>
    public async Task<bool> ExistsAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        await Task.Yield(); // Keep async contract

        if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
        {
            return true;
        }

        if (entry?.IsExpired == true)
        {
            _cache.TryRemove(key, out _);
        }

        return false;
    }

    /// <summary>
    /// Retrieves the values for the specified keys that are present and not expired.
    /// </summary>
    /// <typeparam name="T">The type of the cached values.</typeparam>
    /// <param name="keys">The cache keys to retrieve.</param>
    /// <returns>A dictionary mapping each found key to its cached value.</returns>
    public async Task<Dictionary<string, T>> GetManyAsync<T>(IEnumerable<string> keys) where T : class
    {
        ArgumentNullException.ThrowIfNull(keys);

        await Task.Yield(); // Keep async contract

        var results = new Dictionary<string, T>();

        foreach (var key in keys)
        {
            if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
            {
                if (entry.Value is T value)
                {
                    results[key] = value;
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Removes the entries associated with the specified keys from the cache.
    /// </summary>
    /// <param name="keys">The cache keys to remove.</param>
    public async Task RemoveManyAsync(IEnumerable<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        await Task.Yield(); // Keep async contract

        foreach (var key in keys)
        {
            _cache.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Removes all entries from the cache.
    /// </summary>
    public async Task ClearAsync()
    {
        await Task.Yield(); // Keep async contract
        _cache.Clear();
    }

    /// <summary>
    /// Returns statistics about cache usage, including hit/miss counts, evictions,
    /// current item count, and an estimate of memory usage.
    /// </summary>
    /// <returns>A <see cref="CacheStats"/> snapshot of the cache.</returns>
    public async Task<CacheStats> GetStatsAsync()
    {
        await Task.Yield(); // Keep async contract

        lock (_statsLock)
        {
            return new CacheStats
            {
                Hits = _hits,
                Misses = _misses,
                Evictions = _evictions,
                TotalItems = _cache.Count,
                TotalMemoryBytes = EstimateMemoryUsage()
            };
        }
    }

    private void RecordHit()
    {
        lock (_statsLock)
        {
            _hits++;
        }
    }

    private void RecordMiss()
    {
        lock (_statsLock)
        {
            _misses++;
        }
    }

    private void CleanupExpiredEntries()
    {
        var expiredKeys = _cache
            .Where(kvp => kvp.Value.IsExpired)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }
    }

    private void EvictOldest()
    {
        // LRU: remove least recently accessed entry
        var oldestKey = _cache
            .OrderBy(kvp => kvp.Value.LastAccessed)
            .FirstOrDefault().Key;

        if (!string.IsNullOrEmpty(oldestKey))
        {
            _cache.TryRemove(oldestKey, out _);
            lock (_statsLock)
            {
                _evictions++;
            }
        }
    }

    private long EstimateMemoryUsage()
    {
        // Estimates managed footprint per entry: dictionary bucket plus entry object
        // overhead, UTF-16 key characters, and payload size where it is measurable.
        const long PerEntryOverhead = 88;

        long total = 0;

        foreach (var kvp in _cache)
        {
            total += PerEntryOverhead + (long)kvp.Key.Length * sizeof(char);

            total += kvp.Value.Value switch
            {
                string s => (long)s.Length * sizeof(char) + 26,
                byte[] bytes => bytes.LongLength + 24,
                _ => 64
            };
        }

        return total;
    }

    private class CacheEntry
    {
        public required object Value { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessed { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    }
}
