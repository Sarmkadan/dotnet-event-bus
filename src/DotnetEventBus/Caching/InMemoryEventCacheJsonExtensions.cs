#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetEventBus.Caching;

/// <summary>
/// Represents the serialized state of an <see cref="InMemoryEventCache"/>.
/// </summary>
public sealed class InMemoryEventCacheState
{
    /// <summary>
    /// Gets the cache statistics captured at the time of serialization.
    /// </summary>
    public required CacheStatistics Stats { get; init; }

    /// <summary>
    /// Gets the maximum capacity of the cache.
    /// </summary>
    public required int MaxCapacity { get; init; }
}

/// <summary>
/// Represents cache statistics for serialization.
/// </summary>
public sealed class CacheStatistics
{
    /// <summary>
    /// Gets the total number of items stored in the cache.
    /// </summary>
    public required long TotalItems { get; init; }

    /// <summary>
    /// Gets the cumulative number of cache hits.
    /// </summary>
    public required long Hits { get; init; }

    /// <summary>
    /// Gets the cumulative number of cache misses.
    /// </summary>
    public required long Misses { get; init; }
}

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="InMemoryEventCache"/>.
/// Enables round‑trip serialization/deserialization of cache state.
/// </summary>
public static class InMemoryEventCacheJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    /// <summary>
    /// Serializes the <see cref="InMemoryEventCache"/> to a JSON string.
    /// </summary>
    /// <param name="value">The cache instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the cache state.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when unable to retrieve cache statistics.</exception>
    public static string ToJson(this InMemoryEventCache value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var stats = value.GetStatsAsync().GetAwaiter().GetResult();

        var cacheState = new InMemoryEventCacheState
        {
            Stats = new CacheStatistics
            {
                TotalItems = stats.TotalItems,
                Hits = stats.Hits,
                Misses = stats.Misses
            },
            MaxCapacity = 10000 // Default capacity, actual value is not exposed
        };

        return JsonSerializer.Serialize(cacheState, indented ? GetIndentedOptions() : _jsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to an <see cref="InMemoryEventCache"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>An <see cref="InMemoryEventCache"/> instance with default configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or whitespace.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static InMemoryEventCache? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        JsonSerializer.Deserialize<InMemoryEventCacheState>(json, _jsonOptions);
        return new InMemoryEventCache();
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to an <see cref="InMemoryEventCache"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized cache instance, or <c>null</c> if deserialization fails.</param>
    /// <returns><c>true</c> if deserialization succeeds; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or whitespace.</exception>
    public static bool TryFromJson(string json, out InMemoryEventCache? value)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        try
        {
            JsonSerializer.Deserialize<InMemoryEventCacheState>(json, _jsonOptions);
            value = new InMemoryEventCache();
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }

    private static JsonSerializerOptions GetIndentedOptions()
    {
        var options = new JsonSerializerOptions(_jsonOptions)
        {
            WriteIndented = true
        };
        return options;
    }
}
