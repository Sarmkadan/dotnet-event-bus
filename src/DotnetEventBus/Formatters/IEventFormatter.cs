#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace DotnetEventBus.Formatters;

/// <summary>
/// Interface for event formatters that convert events to different output formats.
/// Supports serialization, deserialization, and metadata handling.
/// </summary>
public interface IEventFormatter
{
    /// <summary>
    /// Gets the format name (e.g., "json", "csv", "xml").
    /// </summary>
    string Format { get; }

    /// <summary>
    /// Gets the MIME type for this format.
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// Serializes an object to the target format.
    /// </summary>
    string Serialize(object data, bool prettyPrint = false);

    /// <summary>
    /// Deserializes a formatted string to the specified type.
    /// </summary>
    T? Deserialize<T>(string data) where T : class;

    /// <summary>
    /// Deserializes a formatted string to the specified type.
    /// </summary>
    object? Deserialize(string data, Type targetType);

    /// <summary>
    /// Formats an event for output.
    /// </summary>
    string FormatEvent(object eventData, bool includePrettyPrint = false);

    /// <summary>
    /// Formats an event with metadata for output.
    /// </summary>
    string FormatEventWithMetadata(object eventData, Dictionary<string, object> metadata, bool includePrettyPrint = false);
}
