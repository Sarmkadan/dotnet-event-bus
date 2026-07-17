#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetEventBus.Examples;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="SubscriptionManagementExample.UserActionEvent"/>.
/// </summary>
public static class SubscriptionManagementExampleJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes a <see cref="SubscriptionManagementExample.UserActionEvent"/> to a JSON string.
    /// </summary>
    /// <param name="value">The user action event to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for better readability.</param>
    /// <returns>A JSON string representation of the user action event.</returns>
    /// <remarks>
    /// Uses camelCase property naming policy and ignores null values when serializing.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this SubscriptionManagementExample.UserActionEvent value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonSerializerOptions)
            {
                WriteIndented = true
            }
            : _jsonSerializerOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a <see cref="SubscriptionManagementExample.UserActionEvent"/> from JSON.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized user action event, or <see langword="null"/> if the JSON is empty or deserialization produces <see langword="null"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized into a <see cref="SubscriptionManagementExample.UserActionEvent"/>.</exception>
    public static SubscriptionManagementExample.UserActionEvent? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        return JsonSerializer.Deserialize<SubscriptionManagementExample.UserActionEvent>(json, _jsonSerializerOptions);
    }

    /// <summary>
    /// Attempts to deserialize a <see cref="SubscriptionManagementExample.UserActionEvent"/> from JSON.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized user action event if successful; otherwise, <see langword="null"/>.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
    public static bool TryFromJson(string json, out SubscriptionManagementExample.UserActionEvent? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<SubscriptionManagementExample.UserActionEvent>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}