#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DotnetEventBus.Formatters;

/// <summary>
/// Formats events as CSV for data export and reporting.
/// Automatically extracts properties from event objects.
/// Why: CSV is a standard format for bulk event export and analysis in external tools.
/// </summary>
public sealed class CsvEventFormatter : IEventFormatter
{
    public string Format => "csv";
    public string ContentType => "text/csv";

    private readonly string _delimiter;
    private readonly bool _includeHeaders;

    public CsvEventFormatter(string delimiter = ",", bool includeHeaders = true)
    {
        _delimiter = delimiter;
        _includeHeaders = includeHeaders;
    }

    public string Serialize(object data, bool prettyPrint = false)
    {
        ArgumentNullException.ThrowIfNull(data);

        var properties = GetProperties(data.GetType());
        var sb = new StringBuilder();

        // Write headers
        if (_includeHeaders)
        {
            var headers = string.Join(_delimiter, properties.Select(p => EscapeCsvField(p.Name)));
            sb.AppendLine(headers);
        }

        // Write values
        var values = string.Join(_delimiter, properties.Select(p => EscapeCsvField(GetPropertyValue(data, p))));
        sb.AppendLine(values);

        return sb.ToString();
    }

    public T? Deserialize<T>(string data) where T : class
    {
        // CSV deserialization is not typically supported
        throw new NotSupportedException("CSV deserialization is not supported");
    }

    public object? Deserialize(string data, Type targetType)
    {
        throw new NotSupportedException("CSV deserialization is not supported");
    }

    public string FormatEvent(object eventData, bool includePrettyPrint = false)
    {
        return Serialize(eventData);
    }

    public string FormatEventWithMetadata(object eventData, Dictionary<string, object> metadata, bool includePrettyPrint = false)
    {
        var properties = GetProperties(eventData.GetType());
        var sb = new StringBuilder();

        // Write headers (include metadata keys)
        if (_includeHeaders)
        {
            var allHeaders = properties.Select(p => p.Name).Concat(metadata.Keys).ToList();
            sb.AppendLine(string.Join(_delimiter, allHeaders.Select(EscapeCsvField)));
        }

        // Write values
        var eventValues = properties.Select(p => EscapeCsvField(GetPropertyValue(eventData, p)));
        var metadataValues = metadata.Values.Select(v => EscapeCsvField(v?.ToString() ?? ""));
        var allValues = eventValues.Concat(metadataValues);
        sb.AppendLine(string.Join(_delimiter, allValues));

        return sb.ToString();
    }

    private static PropertyInfo[] GetProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToArray();
    }

    private static string GetPropertyValue(object obj, PropertyInfo property)
    {
        try
        {
            var value = property.GetValue(obj);
            return value?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private string EscapeCsvField(string? field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;

        // If field contains delimiter, quotes, or newlines, wrap in quotes and escape quotes
        if (field.Contains(_delimiter) || field.Contains("\"") || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}
