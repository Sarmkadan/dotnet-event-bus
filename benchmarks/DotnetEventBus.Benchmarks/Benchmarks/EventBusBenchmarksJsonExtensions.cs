using System.Text.Json;

namespace DotnetEventBus.Benchmarks;

/// <summary>
/// System.Text.Json serialization extensions for EventBusBenchmarks.
/// Provides JSON serialization/deserialization helpers for benchmark data.
/// </summary>
public static class EventBusBenchmarksJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes an EventBusBenchmarks instance to JSON string.
    /// </summary>
    /// <param name="value">The EventBusBenchmarks instance to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    /// <returns>JSON string representation</returns>
    public static string ToJson(this EventBusBenchmarks value, bool indented = false)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions)
            {
                WriteIndented = true
            }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes an EventBusBenchmarks instance from JSON string.
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <returns>Deserialized EventBusBenchmarks instance, or null if JSON is null or empty</returns>
    public static EventBusBenchmarks? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<EventBusBenchmarks>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize an EventBusBenchmarks instance from JSON string.
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <param name="value">Output parameter for the deserialized value</param>
    /// <returns>True if deserialization succeeded, false otherwise</returns>
    public static bool TryFromJson(string json, out EventBusBenchmarks? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<EventBusBenchmarks>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
