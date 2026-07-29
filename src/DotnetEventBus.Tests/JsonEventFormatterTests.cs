#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using DotnetEventBus.Formatters;
using Xunit;

namespace DotnetEventBus.Tests;

public sealed class JsonEventFormatterTests
{
    private sealed class Sample
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    [Fact]
    public void Serialize_Compact_ReturnsCompactJson()
    {
        var formatter = new JsonEventFormatter();
        var obj = new Sample { Id = 1, Name = "Test" };

        var json = formatter.Serialize(obj, prettyPrint: false);

        // Compact JSON should not contain line breaks or indentation
        Assert.DoesNotContain("\n", json);
        Assert.DoesNotContain("  ", json);
        // Verify content
        var deserialized = JsonSerializer.Deserialize<Sample>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(obj.Id, deserialized!.Id);
        Assert.Equal(obj.Name, deserialized.Name);
    }

    [Fact]
    public void Serialize_Pretty_ReturnsIndentedJson()
    {
        var formatter = new JsonEventFormatter();
        var obj = new Sample { Id = 2, Name = "Pretty" };

        var json = formatter.Serialize(obj, prettyPrint: true);

        // Pretty JSON should contain line breaks and indentation
        Assert.Contains("\n", json);
        Assert.Contains("  ", json);
        var deserialized = JsonSerializer.Deserialize<Sample>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(obj.Id, deserialized!.Id);
        Assert.Equal(obj.Name, deserialized.Name);
    }

    [Fact]
    public void Serialize_NullArgument_ThrowsArgumentNullException()
    {
        var formatter = new JsonEventFormatter();
        Assert.Throws<ArgumentNullException>(() => formatter.Serialize(null!));
    }

    [Fact]
    public void Deserialize_Generic_NullOrEmpty_ReturnsNull()
    {
        var formatter = new JsonEventFormatter();

        Assert.Null(formatter.Deserialize<Sample>(null));
        Assert.Null(formatter.Deserialize<Sample>(string.Empty));
    }

    [Fact]
    public void Deserialize_Generic_ValidJson_ReturnsObject()
    {
        var formatter = new JsonEventFormatter();
        var json = "{\"Id\":3,\"Name\":\"Generic\"}";

        var result = formatter.Deserialize<Sample>(json);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Id);
        Assert.Equal("Generic", result.Name);
    }

    [Fact]
    public void Deserialize_NonGeneric_ValidJson_ReturnsObject()
    {
        var formatter = new JsonEventFormatter();
        var json = "{\"Id\":4,\"Name\":\"NonGeneric\"}";

        var result = formatter.Deserialize(json, typeof(Sample));

        Assert.NotNull(result);
        var sample = Assert.IsType<Sample>(result);
        Assert.Equal(4, sample.Id);
        Assert.Equal("NonGeneric", sample.Name);
    }

    [Fact]
    public void FormatEvent_Null_ReturnsLiteralNullString()
    {
        var formatter = new JsonEventFormatter();

        var result = formatter.FormatEvent(null);

        Assert.Equal("null", result);
    }

    [Fact]
    public void FormatEventWithMetadata_IncludesEventMetadataAndTimestamp()
    {
        var formatter = new JsonEventFormatter();
        var ev = new Sample { Id = 5, Name = "Meta" };
        var metadata = new Dictionary<string, object>
        {
            { "source", "unit-test" },
            { "attempt", 1 }
        };

        var json = formatter.FormatEventWithMetadata(ev, metadata, includePrettyPrint: false);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Verify event object
        var eventElem = root.GetProperty("event");
        Assert.Equal(ev.Id, eventElem.GetProperty("Id").GetInt32());
        Assert.Equal(ev.Name, eventElem.GetProperty("Name").GetString());

        // Verify metadata
        var metaElem = root.GetProperty("metadata");
        Assert.Equal("unit-test", metaElem.GetProperty("source").GetString());
        Assert.Equal(1, metaElem.GetProperty("attempt").GetInt32());

        // Verify timestamp exists and is a valid ISO 8601 string
        var timestamp = root.GetProperty("timestamp").GetString();
        Assert.NotNull(timestamp);
        Assert.True(DateTime.TryParse(timestamp, out _));
    }
}
