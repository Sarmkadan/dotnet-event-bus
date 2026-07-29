#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using DotnetEventBus.Formatters;
using Xunit;

namespace DotnetEventBus.Tests;

public sealed class XmlEventFormatterTests
{
    private sealed class SampleEvent
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    [Fact]
    public void Serialize_IncludesXmlDeclaration_ByDefault()
    {
        var formatter = new XmlEventFormatter(); // omitXmlDeclaration = false (default)
        var data = new SampleEvent { Id = 42, Name = "Alice" };

        string xml = formatter.Serialize(data, prettyPrint: false);

        Assert.StartsWith("<?xml", xml.TrimStart());
        Assert.Contains("<SampleEvent>", xml);
        Assert.Contains("<Id>42</Id>", xml);
        Assert.Contains("<Name>Alice</Name>", xml);
    }

    [Fact]
    public void Serialize_OmitsXmlDeclaration_WhenRequested()
    {
        var formatter = new XmlEventFormatter(omitXmlDeclaration: true);
        var data = new SampleEvent { Id = 1, Name = "Bob" };

        string xml = formatter.Serialize(data);

        Assert.DoesNotContain("<?xml", xml);
        Assert.Contains("<SampleEvent>", xml);
    }

    [Fact]
    public void Deserialize_ValidXml_ReturnsObject()
    {
        var formatter = new XmlEventFormatter();
        var original = new SampleEvent { Id = 7, Name = "Charlie" };
        string xml = formatter.Serialize(original, prettyPrint: true);

        SampleEvent? deserialized = formatter.Deserialize<SampleEvent>(xml);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Id, deserialized!.Id);
        Assert.Equal(original.Name, deserialized.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Deserialize_NullOrEmpty_ReturnsNull(string? input)
    {
        var formatter = new XmlEventFormatter();

        SampleEvent? result = formatter.Deserialize<SampleEvent>(input!);

        Assert.Null(result);
    }

    [Fact]
    public void FormatEvent_DelegatesToSerialize()
    {
        var formatter = new XmlEventFormatter();
        var data = new SampleEvent { Id = 99, Name = "Delta" };

        string viaFormat = formatter.FormatEvent(data, includePrettyPrint: true);
        string viaSerialize = formatter.Serialize(data, prettyPrint: true);

        Assert.Equal(viaSerialize, viaFormat);
    }

    [Fact]
    public void FormatEventWithMetadata_IncludesMetadataAndSanitizedKeys()
    {
        var formatter = new XmlEventFormatter();
        var data = new SampleEvent { Id = 5, Name = "Echo" };
        var metadata = new Dictionary<string, object>
        {
            ["my key"] = "value",
            ["123number"] = 42,
            ["normal-key"] = true
        };

        string xml = formatter.FormatEventWithMetadata(data, metadata, includePrettyPrint: false);

        // Root element and timestamp attribute
        Assert.Contains("<Event ", xml);
        Assert.Contains("timestamp=\"", xml);

        // Serialized data inside <Data>
        Assert.Contains("<Data>", xml);
        Assert.Contains("<SampleEvent>", xml);
        Assert.Contains("<Id>5</Id>", xml);
        Assert.Contains("<Name>Echo</Name>", xml);
        Assert.Contains("</Data>", xml);

        // Metadata element and sanitized child element names
        Assert.Contains("<Metadata>", xml);
        Assert.Contains("<my_key>value</my_key>", xml);               // space replaced with _
        Assert.Contains("<_123number>42</_123number>", xml);          // leading digit prefixed with _
        Assert.Contains("<normal-key>True</normal-key>", xml);       // dash is allowed
        Assert.Contains("</Metadata>", xml);
    }

    [Fact]
    public void Serialize_NullData_ThrowsArgumentNullException()
    {
        var formatter = new XmlEventFormatter();

        Assert.Throws<ArgumentNullException>(() => formatter.Serialize(null!));
    }

    [Fact]
    public void FormatEventWithMetadata_EmptyMetadata_ProducesEmptyMetadataElement()
    {
        var formatter = new XmlEventFormatter();
        var data = new SampleEvent { Id = 10, Name = "Foxtrot" };
        var emptyMetadata = new Dictionary<string, object>();

        string xml = formatter.FormatEventWithMetadata(data, emptyMetadata, includePrettyPrint: false);

        Assert.Contains("<Metadata></Metadata>", xml);
    }
}
