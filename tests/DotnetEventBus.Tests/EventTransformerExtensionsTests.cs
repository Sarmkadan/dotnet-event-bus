using System;
using System.Linq;
using DotnetEventBus.Advanced;
using Xunit;

namespace DotnetEventBus.Tests;

public sealed class EventTransformerExtensionsTests
{
    private sealed class Source { public int Id { get; set; } public string? Name { get; set; } }
    private sealed class Target { public int Id { get; set; } public string? Name { get; set; } }

    private EventTransformer<Source, Target> CreateTransformer()
        => EventTransformerBuilder.CreateTransformer<Source, Target>(s => new Target { Id = s.Id, Name = s.Name });

    [Fact]
    public void When_ConditionTrue_AppliesPostTransform()
    {
        var transformer = CreateTransformer().When(t => t.Id == 1, t => new Target { Id = t.Id, Name = "Modified" });
        var result = transformer.Transform(new Source { Id = 1, Name = "Original" });
        Assert.Equal("Modified", result.Name);
    }

    [Fact]
    public void When_ConditionFalse_DoesNotApplyPostTransform()
    {
        var transformer = CreateTransformer().When(t => t.Id == 2, t => new Target { Id = t.Id, Name = "Modified" });
        var result = transformer.Transform(new Source { Id = 1, Name = "Original" });
        Assert.Equal("Original", result.Name);
    }

    [Fact]
    public void IfNotNull_TargetNotNull_AppliesPostTransform()
    {
        var transformer = CreateTransformer().IfNotNull(t => new Target { Id = t.Id, Name = "NotNull" });
        var result = transformer.Transform(new Source { Id = 1, Name = "Original" });
        Assert.Equal("NotNull", result.Name);
    }

    [Fact]
    public void ThenAll_AppliesMultipleTransforms()
    {
        var transformer = CreateTransformer().ThenAll(
            t => new Target { Id = t.Id, Name = t.Name + "1" },
            t => new Target { Id = t.Id, Name = t.Name + "2" });
        var result = transformer.Transform(new Source { Id = 1, Name = "Base" });
        Assert.Equal("Base12", result.Name);
    }

    [Fact]
    public void ThenAll_EmptyArray_ReturnsOriginal()
    {
        var transformer = CreateTransformer().ThenAll();
        var result = transformer.Transform(new Source { Id = 1, Name = "Base" });
        Assert.Equal("Base", result.Name);
    }

    [Fact]
    public void MapIntermediate_MapsCorrectly()
    {
        var transformer = CreateTransformer().MapIntermediate<Source, Target, string>(
            t => t.Name ?? "",
            s => new Target { Id = 1, Name = s + "Mapped" });
        var result = transformer.Transform(new Source { Id = 1, Name = "Base" });
        Assert.Equal("BaseMapped", result.Name);
    }

    [Fact]
    public void Methods_NullInputs_ThrowArgumentNullException()
    {
        var transformer = CreateTransformer();
        Assert.Throws<ArgumentNullException>(() => transformer.When(null!, t => t));
        Assert.Throws<ArgumentNullException>(() => transformer.When(t => true, null!));
        Assert.Throws<ArgumentNullException>(() => transformer.IfNotNull(null!));
        Assert.Throws<ArgumentNullException>(() => transformer.ThenAll(null!));
        Assert.Throws<ArgumentNullException>(() => transformer.MapIntermediate<Source, Target, string>(null!, s => new Target()));
    }
}
