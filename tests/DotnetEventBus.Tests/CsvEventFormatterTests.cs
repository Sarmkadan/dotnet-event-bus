#nullable enable

using System;
using System.Collections.Generic;
using DotnetEventBus.Formatters;
using Xunit;

namespace DotnetEventBus.Tests;

public sealed class CsvEventFormatterTests
{
    private class TestEvent
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    [Fact]
    public void Constructor_SetsDefaults()
    {
        var formatter = new CsvEventFormatter();
        Assert.Equal("csv", formatter.Format);
        Assert.Equal("text/csv", formatter.ContentType);
        Assert.Equal(",", formatter.Delimiter);
        Assert.True(formatter.IncludeHeaders);
    }

    [Fact]
    public void Constructor_CustomSettings_SetsProperties()
    {
        var formatter = new CsvEventFormatter(delimiter: ";", includeHeaders: false);
        Assert.Equal(";", formatter.Delimiter);
        Assert.False(formatter.IncludeHeaders);
    }

    [Fact]
    public void Serialize_WithHeaders_ReturnsCorrectFormat()
    {
        var formatter = new CsvEventFormatter();
        var data = new TestEvent { Id = 1, Name = "Test", Description = "Desc" };
        string result = formatter.Serialize(data);

        Assert.Contains("Id,Name,Description", result);
        Assert.Contains("1,Test,Desc", result);
    }

    [Fact]
    public void Serialize_WithDelimiterInValue_EscapesField()
    {
        var formatter = new CsvEventFormatter();
        var data = new TestEvent { Id = 1, Name = "Test,Value" };
        string result = formatter.Serialize(data);

        Assert.Contains("\"Test,Value\"", result);
    }

    [Fact]
    public void Serialize_NullData_ThrowsArgumentNullException()
    {
        var formatter = new CsvEventFormatter();
        Assert.Throws<ArgumentNullException>(() => formatter.Serialize(null!));
    }

    [Fact]
    public void Deserialize_ThrowsNotSupportedException()
    {
        var formatter = new CsvEventFormatter();
        Assert.Throws<NotSupportedException>(() => formatter.Deserialize("dummy", typeof(TestEvent)));
        Assert.Throws<NotSupportedException>(() => formatter.Deserialize<TestEvent>("dummy"));
    }

    [Fact]
    public void FormatEventWithMetadata_CombinesDataAndMetadata()
    {
        var formatter = new CsvEventFormatter();
        var data = new TestEvent { Id = 1, Name = "Event" };
        var metadata = new Dictionary<string, object> { { "Source", "Unit" } };

        string result = formatter.FormatEventWithMetadata(data, metadata);

        Assert.Contains("Id,Name,Description,Source", result);
        Assert.Contains("1,Event,,Unit", result);
    }

    [Fact]
    public void FormatEventWithMetadata_NoHeaders_DoesNotPrintHeaderRow()
    {
        var formatter = new CsvEventFormatter(includeHeaders: false);
        var data = new TestEvent { Id = 1 };
        var metadata = new Dictionary<string, object> { { "Key", "Val" } };

        string result = formatter.FormatEventWithMetadata(data, metadata);

        // Should not contain property names or metadata keys
        Assert.DoesNotContain("Id", result);
        Assert.DoesNotContain("Key", result);
        // Should contain values
        Assert.Contains("1", result);
        Assert.Contains("Val", result);
    }
}
