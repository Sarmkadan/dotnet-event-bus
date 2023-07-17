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
    private long _hits = 0;
    private long _misses = 0;

    /// <summary>
    /// Maximum number of items to keep in cache before eviction.
    /// </summary>
    private readonly int _maxCapacity;

    public InMemoryEventCache(int maxCapacity = 10000)
    {
        _maxCapacity = maxCapacity;

        // Start cleanup task that runs every minute until disposed.
        _ = Task.Run(async () =>
        {
            try
            {
                while (!_cleanupCts.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), _cleanupCts.Token);
                    CleanupExpiredEntries();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on dispose.
            }
        });
    }

    /// <summary>
    /// Stops the background cleanup loop.
    /// </summary>
    public void Dispose()
    {
        _cleanupCts.Cancel();
        _cleanupCts.Dispose();
    }

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

            RecordHit();
            return entry.Value as T;
        }

        RecordMiss();
        return null;
    }

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

        var entry = new CacheEntry
        {
            Value = value,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null
        };

        _cache[key] = entry;
    }

    public async Task RemoveAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        await Task.Yield(); // Keep async contract
        _cache.TryRemove(key, out _);
    }

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

    public async Task RemoveManyAsync(IEnumerable<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        await Task.Yield(); // Keep async contract

        foreach (var key in keys)
        {
            _cache.TryRemove(key, out _);
        }
    }

    public async Task ClearAsync()
    {
        await Task.Yield(); // Keep async contract
        _cache.Clear();
    }

    public async Task<CacheStats> GetStatsAsync()
    {
        await Task.Yield(); // Keep async contract

        lock (_statsLock)
        {
            return new CacheStats
            {
                Hits = _hits,
                Misses = _misses,
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
        // Simple LRU: remove oldest entry by creation time
        var oldestKey = _cache
            .OrderBy(kvp => kvp.Value.CreatedAt)
            .FirstOrDefault().Key;

        if (!string.IsNullOrEmpty(oldestKey))
        {
            _cache.TryRemove(oldestKey, out _);
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
        public DateTime? ExpiresAt { get; set; }

        public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    }
}
