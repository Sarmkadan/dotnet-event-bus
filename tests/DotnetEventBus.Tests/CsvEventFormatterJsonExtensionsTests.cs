#nullable enable

using System;
using System.Text.Json;
using DotnetEventBus.Formatters;
using Xunit;

namespace DotnetEventBus.Tests;

public sealed class CsvEventFormatterJsonExtensionsTests
{
    [Fact]
    public void ToJson_NullFormatter_ThrowsArgumentNullException()
    {
        CsvEventFormatter? formatter = null;
        Assert.Throws<ArgumentNullException>(() => formatter!.ToJson());
    }

    [Fact]
    public void ToJson_DefaultFormatter_ReturnsExpectedJson()
    {
        var formatter = new CsvEventFormatter(); // delimiter=",", includeHeaders=true

        string json = formatter.ToJson();

        // Expected JSON (camelCase, no indentation)
        const string expected = "{\"format\":\"csv\",\"contentType\":\"text/csv\",\"delimiter\":\",\",\"includeHeaders\":true}";
        Assert.Equal(expected, json);
    }

    [Fact]
    public void ToJson_Indented_ReturnsIndentedJson()
    {
        var formatter = new CsvEventFormatter(delimiter: ";", includeHeaders: false);

        string json = formatter.ToJson(indented: true);

        // The indented JSON should contain line breaks and indentation.
        Assert.Contains("\n", json);
        Assert.Contains("  \"delimiter\": \";\"", json);
        Assert.Contains("\"includeHeaders\": false", json);
    }

    [Fact]
    public void FromJson_ValidJson_ReturnsFormatterWithSameSettings()
    {
        const string json = "{\"format\":\"csv\",\"contentType\":\"text/csv\",\"delimiter\":\"|\",\"includeHeaders\":false}";
        CsvEventFormatter? result = CsvEventFormatterJsonExtensions.FromJson(json);

        Assert.NotNull(result);
        Assert.Equal("|", result!.Delimiter);
        Assert.False(result.IncludeHeaders);
        // The format and content type are fixed by the class, but we verify they match.
        Assert.Equal("csv", result.Format);
        Assert.Equal("text/csv", result.ContentType);
    }

    [Fact]
    public void FromJson_NullOrEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CsvEventFormatterJsonExtensions.FromJson(null!));
        Assert.Throws<ArgumentException>(() => CsvEventFormatterJsonExtensions.FromJson(string.Empty));
    }

    [Fact]
    public void FromJson_InvalidJson_ReturnsNull()
    {
        const string malformed = "{\"delimiter\":\"-\", \"includeHeaders\":true"; // missing closing brace
        CsvEventFormatter? result = CsvEventFormatterJsonExtensions.FromJson(malformed);
        Assert.Null(result);
    }

    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndFormatter()
    {
        const string json = "{\"delimiter\":\",\",\"includeHeaders\":true}";
        bool success = CsvEventFormatterJsonExtensions.TryFromJson(json, out CsvEventFormatter? formatter);

        Assert.True(success);
        Assert.NotNull(formatter);
        Assert.Equal(",", formatter!.Delimiter);
        Assert.True(formatter.IncludeHeaders);
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalseAndNull()
    {
        const string malformed = "{\"delimiter\":\"*\",\"includeHeaders\":true"; // missing closing brace
        bool success = CsvEventFormatterJsonExtensions.TryFromJson(malformed, out CsvEventFormatter? formatter);

        Assert.False(success);
        Assert.Null(formatter);
    }

    [Fact]
    public void TryFromJson_NullOrEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CsvEventFormatterJsonExtensions.TryFromJson(null!, out _));
        Assert.Throws<ArgumentException>(() => CsvEventFormatterJsonExtensions.TryFromJson(string.Empty, out _));
    }
}
