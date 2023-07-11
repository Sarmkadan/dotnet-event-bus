using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace DotnetEventBus.Benchmarks;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="EventBusBenchmarks"/> instances.
/// </summary>
public static class EventBusBenchmarksJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Serializes an <see cref="EventBusBenchmarks"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The <see cref="EventBusBenchmarks"/> instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the <see cref="EventBusBenchmarks"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this EventBusBenchmarks value, bool indented = false)
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
    /// Deserializes an <see cref="EventBusBenchmarks"/> instance from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>
    /// The deserialized <see cref="EventBusBenchmarks"/> instance if successful;
    /// <see langword="null"/> if the input JSON is <see langword="null"/>, empty, or whitespace.
    /// </returns>
    /// <remarks>
    /// This method catches and discards <see cref="JsonException"/> to provide a forgiving API.
    /// For more control over error handling, use <see cref="TryFromJson(string, out EventBusBenchmarks)"/> instead.
    /// </remarks>
    public static EventBusBenchmarks? FromJson(string? json)
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
    /// Attempts to deserialize an <see cref="EventBusBenchmarks"/> instance from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">
    /// When this method returns, contains the deserialized <see cref="EventBusBenchmarks"/> instance if successful,
    /// or <see langword="null"/> if the deserialization failed.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the deserialization succeeded;
    /// <see langword="false"/> if the input JSON is <see langword="null"/>, empty, whitespace, or invalid.
    /// </returns>
    public static bool TryFromJson(string? json, [NotNullWhen(true)] out EventBusBenchmarks? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<EventBusBenchmarks>(json, _jsonOptions);
            return value is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
