// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotnetEventBus.Caching;

/// <summary>
/// Interface for event caching implementations.
/// Provides fast access to frequently retrieved events and subscriptions.
/// </summary>
public interface IEventCache
{
    /// <summary>
    /// Gets a value from the cache.
    /// </summary>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// Sets a value in the cache with optional expiration.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Checks if a key exists in the cache.
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Gets multiple values from the cache.
    /// </summary>
    Task<Dictionary<string, T>> GetManyAsync<T>(IEnumerable<string> keys) where T : class;

    /// <summary>
    /// Removes multiple keys from the cache.
    /// </summary>
    Task RemoveManyAsync(IEnumerable<string> keys);

    /// <summary>
    /// Clears all cache entries.
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// Gets cache statistics (hit/miss counts, etc.).
    /// </summary>
    Task<CacheStats> GetStatsAsync();
}

/// <summary>
/// Statistics about cache performance.
/// </summary>
public class CacheStats
{
    public long Hits { get; set; }
    public long Misses { get; set; }
    public int TotalItems { get; set; }
    public long TotalMemoryBytes { get; set; }

    public double HitRate => Hits + Misses > 0 ? (double)Hits / (Hits + Misses) : 0;
}
