using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace DotnetEventBus.Models;

public static partial class SubscriptionJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions _jsonOptionsIndented = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        WriteIndented = true
    };

    /// <summary>
    /// Serializes a <see cref="Subscription"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The subscription to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the subscription.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this Subscription value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        return JsonSerializer.Serialize(value, indented ? _jsonOptionsIndented : _jsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="Subscription"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized subscription, or null if the JSON is empty or whitespace.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized to a Subscription.</exception>
    public static Subscription? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<Subscription>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="Subscription"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized subscription if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out Subscription? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            value = null;
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<Subscription>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}