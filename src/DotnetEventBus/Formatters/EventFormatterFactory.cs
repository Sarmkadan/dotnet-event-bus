#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetEventBus.Formatters;

/// <summary>
/// Factory for creating and managing event formatters.
/// Provides a registry of formatters and format negotiation by MIME type.
/// Why: Allows flexible switching between output formats without changing client code.
/// </summary>
public sealed class EventFormatterFactory
{
    private readonly Dictionary<string, IEventFormatter> _formatters = [];

    /// <summary>
    /// Creates a new factory with standard formatters (JSON, CSV, XML) pre-registered.
    /// </summary>
    public static EventFormatterFactory CreateDefault()
    {
        var factory = new EventFormatterFactory();
        factory.Register(new JsonEventFormatter());
        factory.Register(new CsvEventFormatter());
        factory.Register(new XmlEventFormatter());
        return factory;
    }

    /// <summary>
    /// Registers a formatter in the factory.
    /// Overrides any existing formatter with the same format name.
    /// </summary>
    public void Register(IEventFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        _formatters[formatter.Format.ToLower()] = formatter;
    }

    /// <summary>
    /// Gets a formatter by its format name (case-insensitive).
    /// </summary>
    public IEventFormatter? GetFormatter(string format)
    {
        if (string.IsNullOrEmpty(format))
            return null;

        _formatters.TryGetValue(format.ToLower(), out var formatter);
        return formatter;
    }

    /// <summary>
    /// Gets a formatter by MIME type content-type header.
    /// Supports exact matches and partial matches.
    /// </summary>
    public IEventFormatter? GetFormatterByContentType(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return null;

        // Exact match first
        var formatter = _formatters.Values.FirstOrDefault(f =>
            f.ContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase));

        if (formatter is not null)
            return formatter;

        // Partial match (e.g., "application/json" matches "json")
        var formatName = ExtractFormatFromContentType(contentType);
        return GetFormatter(formatName);
    }

    /// <summary>
    /// Gets all registered formatters.
    /// </summary>
    public IEnumerable<IEventFormatter> GetAllFormatters()
    {
        return _formatters.Values.ToList();
    }

    /// <summary>
    /// Gets all supported format names.
    /// </summary>
    public IEnumerable<string> GetSupportedFormats()
    {
        return _formatters.Keys.ToList();
    }

    /// <summary>
    /// Checks if a specific format is supported.
    /// </summary>
    public bool IsFormatSupported(string format)
    {
        return !string.IsNullOrEmpty(format) && _formatters.ContainsKey(format.ToLower());
    }

    /// <summary>
    /// Unregisters a formatter by format name.
    /// </summary>
    public bool Unregister(string format)
    {
        return !string.IsNullOrEmpty(format) && _formatters.Remove(format.ToLower());
    }

    private static string ExtractFormatFromContentType(string contentType)
    {
        // Extract format from "application/json" -> "json"
        var parts = contentType.Split('/');
        if (parts.Length > 1)
        {
            return parts[1].Split(';')[0].Trim(); // Remove charset parameters
        }

        return contentType;
    }
}
