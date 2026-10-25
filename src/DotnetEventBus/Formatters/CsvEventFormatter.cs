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
    /// <summary>
    /// Gets the format name ("csv").
    /// </summary>
    public string Format => "csv";

    /// <summary>
    /// Gets the MIME type for CSV output ("text/csv").
    /// </summary>
    public string ContentType => "text/csv";

    /// <summary>
    /// Gets the delimiter used to separate fields.
    /// </summary>
    public string Delimiter => _delimiter;

    /// <summary>
    /// Gets a value indicating whether a header row with property names (and metadata keys) is written.
    /// </summary>
    public bool IncludeHeaders => _includeHeaders;

    private readonly string _delimiter;
    private readonly bool _includeHeaders;

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvEventFormatter"/> class.
    /// </summary>
    /// <param name="delimiter">The field delimiter. Defaults to a comma.</param>
    /// <param name="includeHeaders">Whether to write a header row before the values. Defaults to <c>true</c>.</param>
    public CsvEventFormatter(string delimiter = ",", bool includeHeaders = true)
    {
        _delimiter = delimiter;
        _includeHeaders = includeHeaders;
    }

    /// <summary>
    /// Serializes an object to CSV, using its public readable instance properties as columns.
    /// </summary>
    /// <param name="data">The object to serialize. Must not be <c>null</c>.</param>
    /// <param name="prettyPrint">Ignored; CSV output is not pretty-printed.</param>
    /// <returns>The CSV text, including a header row when <see cref="IncludeHeaders"/> is <c>true</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is <c>null</c>.</exception>
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

    /// <summary>
    /// Not supported for CSV. Always throws.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="data">The CSV text to deserialize.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown; CSV deserialization is not supported.</exception>
    public T? Deserialize<T>(string data) where T : class
    {
        // CSV deserialization is not typically supported
        throw new NotSupportedException("CSV deserialization is not supported");
    }

    /// <summary>
    /// Not supported for CSV. Always throws.
    /// </summary>
    /// <param name="data">The CSV text to deserialize.</param>
    /// <param name="targetType">The target type.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown; CSV deserialization is not supported.</exception>
    public object? Deserialize(string data, Type targetType)
    {
        throw new NotSupportedException("CSV deserialization is not supported");
    }

    /// <summary>
    /// Formats an event as CSV. Equivalent to <see cref="Serialize"/> with default options.
    /// </summary>
    /// <param name="eventData">The event to format. Must not be <c>null</c>.</param>
    /// <param name="includePrettyPrint">Ignored; CSV output is not pretty-printed.</param>
    /// <returns>The CSV text for the event.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="eventData"/> is <c>null</c>.</exception>
    public string FormatEvent(object eventData, bool includePrettyPrint = false)
    {
        return Serialize(eventData);
    }

    /// <summary>
    /// Formats an event as CSV and appends the metadata entries as extra columns after the event properties.
    /// </summary>
    /// <param name="eventData">The event to format. Must not be <c>null</c>.</param>
    /// <param name="metadata">Metadata entries appended as additional columns. Keys become headers; <c>null</c> values are written as empty fields.</param>
    /// <param name="includePrettyPrint">Ignored; CSV output is not pretty-printed.</param>
    /// <returns>The CSV text, including a header row when <see cref="IncludeHeaders"/> is <c>true</c>.</returns>
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
