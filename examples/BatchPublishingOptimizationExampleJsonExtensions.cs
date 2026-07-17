#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetEventBus.Examples;

/// <summary>
/// Provides System.Text.Json serialization extensions for event types in the
/// <see cref="BatchPublishingOptimizationExample"/> class.
/// </summary>
public static class BatchPublishingOptimizationExampleJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes a <see cref="BatchPublishingOptimizationExample.LogEntryEvent"/> to JSON.
    /// </summary>
    /// <param name="value">The log entry event to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representation of the log entry event.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this BatchPublishingOptimizationExample.LogEntryEvent value, bool indented = false)
        => JsonSerializer.Serialize(value, GetJsonSerializerOptions(indented));

    /// <summary>
    /// Serializes an <see cref="BatchPublishingOptimizationExample.AnalyticsEvent"/> to JSON.
    /// </summary>
    /// <param name="value">The analytics event to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representation of the analytics event.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this BatchPublishingOptimizationExample.AnalyticsEvent value, bool indented = false)
        => JsonSerializer.Serialize(value, GetJsonSerializerOptions(indented));

    /// <summary>
    /// Deserializes a <see cref="BatchPublishingOptimizationExample.LogEntryEvent"/> from JSON.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized log entry event, or null if <paramref name="json"/> is empty or whitespace.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static BatchPublishingOptimizationExample.LogEntryEvent? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        return JsonSerializer.Deserialize<BatchPublishingOptimizationExample.LogEntryEvent>(json, _jsonSerializerOptions);
    }

    /// <summary>
    /// Attempts to deserialize a <see cref="BatchPublishingOptimizationExample.LogEntryEvent"/> from JSON.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized log entry event if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string json, out BatchPublishingOptimizationExample.LogEntryEvent? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<BatchPublishingOptimizationExample.LogEntryEvent>(json, _jsonSerializerOptions);
            return value is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Deserializes an <see cref="BatchPublishingOptimizationExample.AnalyticsEvent"/> from JSON.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized analytics event, or null if <paramref name="json"/> is empty or whitespace.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static BatchPublishingOptimizationExample.AnalyticsEvent? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        return JsonSerializer.Deserialize<BatchPublishingOptimizationExample.AnalyticsEvent>(json, _jsonSerializerOptions);
    }

    /// <summary>
    /// Attempts to deserialize an <see cref="BatchPublishingOptimizationExample.AnalyticsEvent"/> from JSON.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized analytics event if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string json, out BatchPublishingOptimizationExample.AnalyticsEvent? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<BatchPublishingOptimizationExample.AnalyticsEvent>(json, _jsonSerializerOptions);
            return value is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the appropriate <see cref="JsonSerializerOptions"/> based on the indented parameter.
    /// </summary>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>The configured <see cref="JsonSerializerOptions"/>.</returns>
    private static JsonSerializerOptions GetJsonSerializerOptions(bool indented)
        => indented
            ? new JsonSerializerOptions(_jsonSerializerOptions)
            {
                WriteIndented = true
            }
            : _jsonSerializerOptions;