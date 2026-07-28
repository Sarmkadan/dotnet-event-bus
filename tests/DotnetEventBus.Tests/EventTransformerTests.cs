using System;
using System.Collections.Generic;
using System.Linq;
using DotnetEventBus.Advanced;
using Xunit;

namespace DotnetEventBus.Tests;

public sealed class EventTransformerTests
{
    private sealed class Source
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Extra { get; set; }
    }

    private sealed class Target
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    [Fact]
    public void CreateTransformer_TransformsCorrectly()
    {
        var transformer = EventTransformerBuilder.CreateTransformer<Source, Target>(s => new Target
        {
            Id = s.Id,
            Name = s.Name
        });

        var source = new Source { Id = 42, Name = "Test" };
        var result = transformer.Transform(source);

        Assert.Equal(42, result.Id);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public void Then_AddsPostTransform()
    {
        var transformer = EventTransformerBuilder.CreateTransformer<Source, Target>(s => new Target
        {
            Id = s.Id,
            Name = s.Name
        });

        transformer = transformer.Then(t => new Target
        {
            Id = t.Id,
            Name = t.Name?.ToUpperInvariant()
        });

        var source = new Source { Id = 1, Name = "lower" };
        var result = transformer.Transform(source);

        Assert.Equal(1, result.Id);
        Assert.Equal("LOWER", result.Name);
    }

    [Fact]
    public void Transform_NullSource_ThrowsArgumentNullException()
    {
        var transformer = EventTransformerBuilder.CreateTransformer<Source, Target>(s => new Target());
        Assert.Throws<ArgumentNullException>(() => transformer.Transform(null!));
    }

    [Fact]
    public void TransformMany_EmptyCollection_ReturnsEmpty()
    {
        var transformer = EventTransformerBuilder.CreateTransformer<Source, Target>(s => new Target());
        var empty = Enumerable.Empty<Source>();
        var result = transformer.TransformMany(empty);

        Assert.Empty(result);
    }

    [Fact]
    public void Chain_TransformsCorrectly()
    {
        var transformer = EventTransformerBuilder.CreateTransformer<Source, Target>(s => new Target
        {
            Id = s.Id,
            Name = s.Name
        });

        var chained = transformer.Chain<string>(t => t.Name ?? string.Empty);

        var source = new Source { Id = 5, Name = "Chain" };
        var result = chained.Transform(source);

        Assert.Equal("Chain", result);
    }

    [Fact]
    public void CreatePropertyCopyTransformer_CopiesMatchingProperties()
    {
        var transformer = EventTransformerBuilder.CreatePropertyCopyTransformer<Source, Target>();
        var source = new Source { Id = 99, Name = "Copy", Extra = "Ignore" };

        var result = transformer.Transform(source);

        Assert.Equal(99, result.Id);
        Assert.Equal("Copy", result.Name);
    }

    [Fact]
    public void CreateDictionaryTransformer_CreatesDictionaryWithAllProperties()
    {
        var transformer = EventTransformerBuilder.CreateDictionaryTransformer<Source>();
        var source = new Source { Id = 7, Name = "Dict", Extra = "Value" };

        var dict = transformer.Transform(source);

        Assert.Equal(3, dict.Count);
        Assert.Equal(7, dict["Id"]);
        Assert.Equal("Dict", dict["Name"]);
        Assert.Equal("Value", dict["Extra"]);
    }

    [Fact]
    public void Constructor_NullTransformFunc_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new EventTransformer<Source, Target>(null!));
    }

    [Fact]
    public void Then_NullPostTransform_ThrowsArgumentNullException()
    {
        var transformer = EventTransformerBuilder.CreateTransformer<Source, Target>(s => new Target());
        Assert.Throws<ArgumentNullException>(() => transformer.Then(null!));
    }

    [Fact]
    public void CreateTransformer_NullMapFunc_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => EventTransformerBuilder.CreateTransformer<Source, Target>(null!));
    }
}
