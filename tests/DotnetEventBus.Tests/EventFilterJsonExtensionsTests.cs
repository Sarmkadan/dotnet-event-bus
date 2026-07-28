// Copyright (c) 2024
// SPDX-License-Identifier: MIT

using System;
using DotnetEventBus.Advanced;
using Xunit;

namespace DotnetEventBus.Tests;

public sealed class EventFilterJsonExtensionsTests
{
    // A simple POCO used as the generic type argument for EventFilter<T>.
    private sealed class SampleEvent
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private static EventFilter<SampleEvent> CreateSampleFilter()
    {
        // The EventFilter<T> type in this repository has a public parameter‑less constructor.
        // No additional configuration is required for the serialization tests.
        return new EventFilter<SampleEvent>();
    }

    [Fact]
    public void ToJson_WithValidFilter_ReturnsNonEmptyJson()
    {
        // Arrange
        var filter = CreateSampleFilter();

        // Act
        string json = filter.ToJson();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(json));
        // The JSON should start with a '{' because the filter is serialized as an object.
        Assert.StartsWith("{", json);
    }

    [Fact]
    public void ToJson_WithNullFilter_ThrowsArgumentNullException()
    {
        // Arrange
        EventFilter<SampleEvent>? filter = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => filter!.ToJson());
    }

    [Fact]
    public void ToJson_WithIndentation_ContainsNewLine()
    {
        // Arrange
        var filter = CreateSampleFilter();

        // Act
        string json = filter.ToJson(indented: true);

        // Assert
        // Indented JSON must contain at least one line‑break character.
        Assert.Contains(Environment.NewLine, json);
    }

    [Fact]
    public void FromJson_WithValidJson_ReturnsFilter()
    {
        // Arrange
        var original = CreateSampleFilter();
        string json = original.ToJson();

        // Act
        var deserialized = EventFilterJsonExtensions.FromJson<SampleEvent>(json);

        // Assert
        Assert.NotNull(deserialized);
        // The deserialized instance should be of the same generic type.
        Assert.IsType<EventFilter<SampleEvent>>(deserialized);
    }

    [Fact]
    public void FromJson_WithNullOrEmpty_ReturnsNull()
    {
        // Null input
        Assert.Null(EventFilterJsonExtensions.FromJson<SampleEvent>(null));

        // Empty string
        Assert.Null(EventFilterJsonExtensions.FromJson<SampleEvent>(string.Empty));

        // Whitespace only
        Assert.Null(EventFilterJsonExtensions.FromJson<SampleEvent>("   "));
    }

    [Fact]
    public void TryFromJson_WithValidJson_ReturnsTrueAndValue()
    {
        // Arrange
        var original = CreateSampleFilter();
        string json = original.ToJson();

        // Act
        bool result = EventFilterJsonExtensions.TryFromJson<SampleEvent>(json, out var value);

        // Assert
        Assert.True(result);
        Assert.NotNull(value);
        Assert.IsType<EventFilter<SampleEvent>>(value);
    }

    [Fact]
    public void TryFromJson_WithMalformedJson_ReturnsFalseAndNull()
    {
        // Arrange
        const string malformedJson = "{ this is not valid json }";

        // Act
        bool result = EventFilterJsonExtensions.TryFromJson<SampleEvent>(malformedJson, out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void TryFromJson_WithNullOrEmpty_ReturnsFalseAndNull()
    {
        // Null
        bool nullResult = EventFilterJsonExtensions.TryFromJson<SampleEvent>(null, out var nullValue);
        Assert.False(nullResult);
        Assert.Null(nullValue);

        // Empty
        bool emptyResult = EventFilterJsonExtensions.TryFromJson<SampleEvent>(string.Empty, out var emptyValue);
        Assert.False(emptyResult);
        Assert.Null(emptyValue);
    }
}
