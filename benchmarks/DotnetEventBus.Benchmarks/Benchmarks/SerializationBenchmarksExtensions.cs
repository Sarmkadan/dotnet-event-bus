using System;
using System.IO;
using System.Text;
using BenchmarkDotNet.Running;

namespace DotnetEventBus.Benchmarks;

/// <summary>
/// Extension methods for <see cref="SerializationBenchmarks"/> that provide additional functionality
/// for working with serialization benchmarks and their results.
/// </summary>
public static class SerializationBenchmarksExtensions
{
    /// <summary>
    /// Validates that the JSON serialization produces valid JSON that can be deserialized back.
    /// </summary>
    /// <param name="benchmarks">The serialization benchmarks instance.</param>
    /// <returns>True if the serialized JSON is valid and can be deserialized; otherwise false.</returns>
    public static bool ValidateJsonSerialization(this SerializationBenchmarks benchmarks)
    {
        var serialized = benchmarks.Serialize_Json();
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return false;
        }

        try
        {
            // Simple validation: check if it starts with '{' and ends with '}'
            // and contains basic JSON structure
            var trimmed = serialized.Trim();
            if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
            {
                // Try to parse as JSON
                using var doc = System.Text.Json.JsonDocument.Parse(serialized);
                return doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Undefined;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates that the CSV serialization produces valid CSV that can be parsed.
    /// </summary>
    /// <param name="benchmarks">The serialization benchmarks instance.</param>
    /// <returns>True if the serialized CSV is valid and contains expected headers; otherwise false.</returns>
    public static bool ValidateCsvSerialization(this SerializationBenchmarks benchmarks)
    {
        var serialized = benchmarks.Serialize_Csv();
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return false;
        }

        try
        {
            // Check if it contains CSV headers (should have at least one comma)
            var lines = serialized.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 0 && lines[0].Contains(','))
            {
                // Check if it has data rows
                return lines.Length > 1;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates that the XML serialization produces valid XML that can be loaded.
    /// </summary>
    /// <param name="benchmarks">The serialization benchmarks instance.</param>
    /// <returns>True if the serialized XML is valid and can be loaded; otherwise false.</returns>
    public static bool ValidateXmlSerialization(this SerializationBenchmarks benchmarks)
    {
        var serialized = benchmarks.Serialize_Xml();
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return false;
        }

        try
        {
            // Check if it starts with XML declaration or root element
            var trimmed = serialized.Trim();
            if (trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith('<'))
            {
                // Try to load as XML
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(serialized);
                return doc.DocumentElement != null;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a summary of the serialization format based on the benchmark instance.
    /// </summary>
    /// <param name="benchmarks">The serialization benchmarks instance.</param>
    /// <returns>A string describing the serialization format being tested.</returns>
    public static string GetSerializationFormat(this SerializationBenchmarks benchmarks)
    {
        if (benchmarks == null)
        {
            throw new ArgumentNullException(nameof(benchmarks));
        }

        // This is a heuristic based on the method names
        // Since we can't directly inspect the formatter types, we use the benchmark method names
        if (benchmarks.Serialize_Json() != null)
        {
            return "JSON";
        }

        if (benchmarks.Serialize_Csv() != null)
        {
            return "CSV";
        }

        if (benchmarks.Serialize_Xml() != null)
        {
            return "XML";
        }

        return "Unknown";
    }
}