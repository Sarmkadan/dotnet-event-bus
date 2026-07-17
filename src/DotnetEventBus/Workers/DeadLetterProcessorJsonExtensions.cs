#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetEventBus.Workers;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="DeadLetterProcessor"/>.
/// </summary>
internal static class DeadLetterProcessorJsonExtensions
{
    static DeadLetterProcessorJsonExtensions()
    {
        // Initialize static members.
    }

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    /// <summary>
    /// Converts a <see cref="DeadLetterProcessor"/> instance to its JSON representation.
    /// </summary>
    /// <param name="value">The dead letter processor instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representing the dead letter processor.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this DeadLetterProcessor value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Parses a JSON string into a <see cref="DeadLetterProcessor"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>The deserialized <see cref="DeadLetterProcessor"/> instance, or <see langword="null"/> if the JSON is empty.</returns>
    /// <exception cref="ArgumentException"><paramref name="json"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="JsonException">The JSON is invalid or cannot be deserialized.</exception>
    public static DeadLetterProcessor? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        return JsonSerializer.Deserialize<DeadLetterProcessor>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to parse a JSON string into a <see cref="DeadLetterProcessor"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <param name="value">Receives the deserialized instance if successful.</param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="json"/> is <see langword="null"/> or empty.</exception>
    public static bool TryFromJson(string json, out DeadLetterProcessor? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<DeadLetterProcessor>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}