#nullable enable

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetEventBus.Caching;

/// <summary>
/// JSON extension methods for <see cref="InMemoryEventCache"/>.
/// </summary>
public static class InMemoryEventCacheJsonExtensions
{
    /// <summary>
    /// Serializes the cache state to a JSON string.
    /// </summary>
    /// <param name="cache">The cache to serialize.</param>
    /// <returns>A JSON string representing the cache state.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cache"/> is <see langword="null"/>.</exception>
    public static string ToJson(this InMemoryEventCache cache)
    {
        if (cache == null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        // Get stats synchronously (safe because GetStatsAsync yields then does synchronous work)
        var statsTask = cache.GetStatsAsync();
        statsTask.Wait();
        var stats = statsTask.Result;

        var dto = new
        {
            stats = new
            {
                totalItems = stats.TotalItems,
                hits = stats.Hits,
                misses = stats.Misses
            },
            maxCapacity = cache.Capacity
        };

        return JsonSerializer.Serialize(dto);
    }

    /// <summary>
    /// Deserializes a JSON string into an <see cref="InMemoryEventCache"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>An <see cref="InMemoryEventCache"/> instance with the specified capacity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty.</exception>
    /// <exception cref="JsonException">Thrown when <paramref name="json"/> is not valid JSON.</exception>
    public static InMemoryEventCache FromJson(string json)
    {
        if (json == null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        if (json.Length == 0)
        {
            throw new ArgumentException("Input string is empty.", nameof(json));
        }

        using var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;

        int maxCapacity = InMemoryEventCache.DefaultMaxCapacity; // default 10000
        if (root.TryGetProperty("maxCapacity", out var maxCapacityProp) &&
            maxCapacityProp.ValueKind == JsonValueKind.Number)
        {
            maxCapacity = maxCapacityProp.GetInt32();
        }

        // Note: We cannot set hits/misses/etc. from JSON as they are private state.
        // The deserialized cache will start with zero stats.
        return new InMemoryEventCache(maxCapacity);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into an <see cref="InMemoryEventCache"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="cache">When this method returns, contains the deserialized cache instance if the input was valid JSON, otherwise null.</param>
    /// <returns>true if <paramref name="json"/> was valid JSON; otherwise, false.</returns>
    public static bool TryFromJson(string json, out InMemoryEventCache? cache)
    {
        try
        {
            cache = FromJson(json);
            return true;
        }
        catch (JsonException)
        {
            cache = null;
            return false;
        }
        catch (ArgumentNullException)
        {
            cache = null;
            return false;
        }
        catch (ArgumentException)
        {
            cache = null;
            return false;
        }
    }
}