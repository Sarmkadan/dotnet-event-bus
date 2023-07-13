#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetEventBus.Advanced;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="EventFilter{T}"/>.
/// Enables converting event filters to/from JSON for persistence, caching, or network transfer.
/// </summary>
public static class EventFilterJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes an <see cref="EventFilter{T}"/> to a JSON string.
    /// </summary>
    /// <typeparam name="T">The event type filtered by the filter.</typeparam>
    /// <param name="value">The filter to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the filter.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson<T>(this EventFilter<T> value, bool indented = false) where T : class
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions)
            {
                WriteIndented = true
            }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes an <see cref="EventFilter{T}"/> from a JSON string.
    /// </summary>
    /// <typeparam name="T">The event type filtered by the filter.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized filter, or null if the JSON is null or empty.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is malformed or cannot be deserialized.</exception>
    public static EventFilter<T>? FromJson<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<EventFilter<T>>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize an <see cref="EventFilter{T}"/> from a JSON string.
    /// </summary>
    /// <typeparam name="T">The event type filtered by the filter.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized filter if successful, otherwise null.</param>
    /// <returns>True if deserialization succeeded; false if the JSON is malformed or null/empty.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is null.</exception>
    public static bool TryFromJson<T>(string? json, out EventFilter<T>? value) where T : class
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<EventFilter<T>>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
