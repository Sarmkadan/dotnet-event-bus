// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace DotnetEventBus.Formatters;

/// <summary>
/// Formats events as XML for legacy system integration.
/// Supports both serialization and deserialization with proper formatting.
/// Why: XML is required for integration with legacy enterprise systems.
/// </summary>
public class XmlEventFormatter : IEventFormatter
{
    public string Format => "xml";
    public string ContentType => "application/xml";

    private readonly bool _omitXmlDeclaration;
    private readonly Encoding _encoding;

    public XmlEventFormatter(bool omitXmlDeclaration = false, Encoding? encoding = null)
    {
        _omitXmlDeclaration = omitXmlDeclaration;
        _encoding = encoding ?? Encoding.UTF8;
    }

    public string Serialize(object data, bool prettyPrint = false)
    {
        ArgumentNullException.ThrowIfNull(data);

        var serializer = new XmlSerializer(data.GetType());
        var settings = new XmlWriterSettings
        {
            Encoding = _encoding,
            Indent = prettyPrint,
            OmitXmlDeclaration = _omitXmlDeclaration,
            ConformanceLevel = ConformanceLevel.Document
        };

        using (var stream = new MemoryStream())
        using (var writer = XmlWriter.Create(stream, settings))
        {
            serializer.Serialize(writer, data);
            return _encoding.GetString(stream.ToArray());
        }
    }

    public T? Deserialize<T>(string data) where T : class
    {
        if (string.IsNullOrEmpty(data))
            return null;

        try
        {
            var serializer = new XmlSerializer(typeof(T));
            using (var reader = new StringReader(data))
            {
                return serializer.Deserialize(reader) as T;
            }
        }
        catch
        {
            return null;
        }
    }

    public object? Deserialize(string data, Type targetType)
    {
        if (string.IsNullOrEmpty(data))
            return null;

        try
        {
            var serializer = new XmlSerializer(targetType);
            using (var reader = new StringReader(data))
            {
                return serializer.Deserialize(reader);
            }
        }
        catch
        {
            return null;
        }
    }

    public string FormatEvent(object eventData, bool includePrettyPrint = false)
    {
        return Serialize(eventData, includePrettyPrint);
    }

    public string FormatEventWithMetadata(object eventData, Dictionary<string, object> metadata, bool includePrettyPrint = false)
    {
        var settings = new XmlWriterSettings
        {
            Encoding = _encoding,
            Indent = includePrettyPrint,
            OmitXmlDeclaration = _omitXmlDeclaration,
            ConformanceLevel = ConformanceLevel.Document
        };

        using (var stream = new MemoryStream())
        using (var writer = XmlWriter.Create(stream, settings))
        {
            writer.WriteStartElement("Event");
            writer.WriteAttributeString("timestamp", DateTime.UtcNow.ToString("o"));

            // Write event data
            writer.WriteStartElement("Data");
            var eventSerializer = new XmlSerializer(eventData.GetType());
            eventSerializer.Serialize(writer, eventData);
            writer.WriteEndElement(); // Data

            // Write metadata
            writer.WriteStartElement("Metadata");
            foreach (var kvp in metadata)
            {
                writer.WriteStartElement(SanitizeXmlElementName(kvp.Key));
                writer.WriteString(kvp.Value?.ToString() ?? string.Empty);
                writer.WriteEndElement();
            }
            writer.WriteEndElement(); // Metadata

            writer.WriteEndElement(); // Event
            return _encoding.GetString(stream.ToArray());
        }
    }

    private static string SanitizeXmlElementName(string name)
    {
        // XML element names cannot contain spaces or special characters
        var sanitized = System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9_-]", "_");
        // XML element names cannot start with a number
        if (char.IsDigit(sanitized[0]))
            sanitized = "_" + sanitized;

        return sanitized;
    }
}
