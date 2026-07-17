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
/// Enables serialization of predicate-based subscription configurations for persistence,
/// transmission, or debugging purposes.
/// </summary>
/// <remarks>
/// Note: Deserialization of <see cref="PredicateSubscriptionBuilder{TEvent}"/> is not supported
/// as the type has an internal constructor and requires an <see cref="IEventBus"/> instance.
/// </remarks>
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
}
