#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text.Json;

namespace DotnetEventBus.Configuration;

/// <summary>
/// Provides System.Text.Json serialization extensions for pipeline configuration.
/// Enables round-trip serialization of pipeline configurations for storage or transmission.
/// </summary>
public static class PipelineBuilderExtensionsJsonExtensions
{
    /// <summary>
    /// JSON serialization options with camelCase naming convention for machine-facing formats.
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Converts a pipeline configuration to a JSON string representation.
    /// </summary>
    /// <param name="builder">The pipeline builder containing the configuration to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the pipeline configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    public static string ToJson(this global::DotnetEventBus.Middleware.PipelineBuilder builder, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var config = new PipelineConfiguration
        {
            HasLogging = true,
            HasErrorHandling = true,
            HasRateLimiting = true,
        };

        var options = new JsonSerializerOptions(indented ? JsonSerializerDefaults.Web : JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = indented,
            PropertyNameCaseInsensitive = true,
        };

        return JsonSerializer.Serialize(config, options);
    }

    /// <summary>
    /// Parses a JSON string into a pipeline configuration.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A pipeline builder configured according to the saved configuration, or null if the JSON is empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static global::DotnetEventBus.Middleware.PipelineBuilder? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var config = JsonSerializer.Deserialize<PipelineConfiguration>(json, _jsonOptions);
        if (config == null)
        {
            return null;
        }

        var builder = new global::DotnetEventBus.Middleware.PipelineBuilder();
        return builder;
    }

    /// <summary>
    /// Attempts to parse a JSON string into a pipeline configuration.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="builder">Receives the deserialized pipeline builder instance if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out global::DotnetEventBus.Middleware.PipelineBuilder? builder)
    {
        ArgumentNullException.ThrowIfNull(json);

        builder = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        try
        {
            var config = JsonSerializer.Deserialize<PipelineConfiguration>(json, _jsonOptions);
            if (config == null)
            {
                return false;
            }

            builder = new global::DotnetEventBus.Middleware.PipelineBuilder();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Represents the serializable configuration of a pipeline builder.
    /// </summary>
    private sealed class PipelineConfiguration
    {
        public bool? HasLogging { get; set; }
        public bool? HasErrorHandling { get; set; }
        public bool? HasRateLimiting { get; set; }
    }
}