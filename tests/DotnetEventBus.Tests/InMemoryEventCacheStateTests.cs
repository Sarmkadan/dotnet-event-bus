using System;
using DotnetEventBus.Caching;
using Xunit;

namespace DotnetEventBus.Tests;

public class InMemoryEventCacheStateTests
{
    [Fact]
    public void ToJson_NullCache_ThrowsArgumentNullException()
    {
        InMemoryEventCache? cache = null;
        Assert.Throws<ArgumentNullException>(() => cache!.ToJson());
    }

    [Fact]
    public void ToJson_ReturnsJsonContainingAllExpectedProperties()
    {
        // Arrange: create a cache instance (default constructor assumed to exist)
        var cache = new InMemoryEventCache();

        // Act
        var json = cache.ToJson();

        // Assert: JSON contains the camel‑cased property names defined in InMemoryEventCacheState
        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.Contains("\"totalItems\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"hits\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"misses\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"maxCapacity\"", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FromJson_NullJson_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => InMemoryEventCacheJsonExtensions.FromJson(null!));
    }

    [Fact]
    public void FromJson_EmptyJson_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => InMemoryEventCacheJsonExtensions.FromJson(string.Empty));
    }

    [Fact]
    public void FromJson_ValidJson_ReturnsCacheInstance()
    {
        // Minimal valid JSON that matches the serialized shape of InMemoryEventCacheState
        var json = "{\"stats\":{\"totalItems\":0,\"hits\":0,\"misses\":0},\"maxCapacity\":10000}";
        var cache = InMemoryEventCacheJsonExtensions.FromJson(json);
        Assert.NotNull(cache);
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalseAndNull()
    {
        var invalidJson = "{ this is not valid json }";
        var result = InMemoryEventCacheJsonExtensions.TryFromJson(invalidJson, out var cache);
        Assert.False(result);
        Assert.Null(cache);
    }

    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndCache()
    {
        var json = "{\"stats\":{\"totalItems\":5,\"hits\":2,\"misses\":3},\"maxCapacity\":10000}";
        var result = InMemoryEventCacheJsonExtensions.TryFromJson(json, out var cache);
        Assert.True(result);
        Assert.NotNull(cache);
    }
}
