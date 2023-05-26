#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetEventBus.Handlers;

namespace DotnetEventBus.Services;

/// <summary>
/// Provides System.Text.Json serialization extensions for predicate subscription types.
/// Enables serialization and deserialization of predicate-based subscription configurations
/// for persistence, transmission, or debugging purposes.
/// </summary>
public static class PredicateSubscriptionExtensionsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    /// <summary>
    /// Serializes a predicate subscription builder configuration to JSON.
    /// </summary>
    /// <typeparam name="TEvent">The event type being subscribed to.</typeparam>
    /// <param name="builder">The predicate subscription builder to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representing the builder configuration.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder"/> is <see langword="null"/>.
    /// </exception>
    public static string ToJson<TEvent>(this PredicateSubscriptionBuilder<TEvent> builder, bool indented = false)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = indented
            ? new JsonSerializerOptions(_jsonSerializerOptions)
            {
                WriteIndented = true
            }
            : _jsonSerializerOptions;

        return JsonSerializer.Serialize(builder, options);
    }

    /// <summary>
    /// Deserializes a predicate subscription builder configuration from JSON.
    /// </summary>
    /// <typeparam name="TEvent">The event type being deserialized.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized predicate subscription builder configuration, or <see langword="null"/> if the JSON is empty or whitespace.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="json"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="JsonException">
    /// Thrown when the JSON is invalid or cannot be deserialized.
    /// </exception>
    public static PredicateSubscriptionBuilder<TEvent>? FromJson<TEvent>(string json)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<PredicateSubscriptionBuilder<TEvent>>(json, _jsonSerializerOptions);
    }

    /// <summary>
    /// Attempts to deserialize a predicate subscription builder configuration from JSON.
    /// </summary>
    /// <typeparam name="TEvent">The event type being deserialized.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">The deserialized predicate subscription builder configuration, or <see langword="null"/> if deserialization fails.</param>
    /// <returns><see langword="true"/> if deserialization succeeds; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="json"/> is <see langword="null"/>.
    /// </exception>
    public static bool TryFromJson<TEvent>(string json, out PredicateSubscriptionBuilder<TEvent>? value)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(json);

        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        try
        {
            value = JsonSerializer.Deserialize<PredicateSubscriptionBuilder<TEvent>>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}