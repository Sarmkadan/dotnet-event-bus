// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetEventBus.Formatters;

/// <summary>
/// Formats events as JSON for serialization and API responses.
/// Provides both compact and pretty-printed output options.
/// Why: JSON is the standard format for distributed event systems and REST APIs.
/// </summary>
public class JsonEventFormatter : IEventFormatter
{
    private readonly JsonSerializerOptions _compactOptions;
    private readonly JsonSerializerOptions _prettyOptions;

    public string Format => "json";
    public string ContentType => "application/json";

    public JsonEventFormatter()
    {
        _compactOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        _prettyOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public string Serialize(object data, bool prettyPrint = false)
    {
        ArgumentNullException.ThrowIfNull(data);

        var options = prettyPrint ? _prettyOptions : _compactOptions;
        return JsonSerializer.Serialize(data, options);
    }

    public T? Deserialize<T>(string json) where T : class
    {
        if (string.IsNullOrEmpty(json))
            return null;

        return JsonSerializer.Deserialize<T>(json, _compactOptions);
    }

    public object? Deserialize(string json, Type targetType)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        return JsonSerializer.Deserialize(json, targetType, _compactOptions);
    }

    public string FormatEvent(object eventData, bool includePrettyPrint = false)
    {
        if (eventData == null)
            return "null";

        return Serialize(eventData, includePrettyPrint);
    }

    public string FormatEventWithMetadata(object eventData, Dictionary<string, object> metadata, bool includePrettyPrint = false)
    {
        var envelope = new
        {
            @event = eventData,
            metadata,
            timestamp = DateTime.UtcNow.ToString("o")
        };

        return Serialize(envelope, includePrettyPrint);
    }
}
