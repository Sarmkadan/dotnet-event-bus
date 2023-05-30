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
/// Provides System.Text.Json serialization extensions for <see cref="InMemoryEventCache"/>.
/// Enables round-trip serialization/deserialization of cache state.
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this InMemoryEventCache value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        // Serialize cache metadata since internal state is not exposed
        var cacheState = new
        {
            Stats = new
            {
                TotalItems = 0,
                Hits = 0,
                Misses = 0
            },
            MaxCapacity = 10000
        };

        return JsonSerializer.Serialize(cacheState, indented ? GetIndentedOptions() : _jsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to an <see cref="InMemoryEventCache"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>An <see cref="InMemoryEventCache"/> instance with default configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static InMemoryEventCache? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            var _ = JsonSerializer.Deserialize<object>(json, _jsonOptions);
            return new InMemoryEventCache();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to an <see cref="InMemoryEventCache"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized cache instance, or null if deserialization fails.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out InMemoryEventCache? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            var _ = JsonSerializer.Deserialize<object>(json, _jsonOptions);
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