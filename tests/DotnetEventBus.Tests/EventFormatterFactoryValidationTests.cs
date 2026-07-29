#nullable enable

using System;
using DotnetEventBus.Formatters;
using Xunit;

namespace DotnetEventBus.Tests;

public sealed class EventFormatterFactoryValidationTests
{
    [Fact]
    public void Validate_NullFactory_ThrowsArgumentNullException()
    {
        EventFormatterFactory? factory = null;
        Assert.Throws<ArgumentNullException>(() => factory.Validate());
    }

    [Fact]
    public void Validate_ValidFactory_ReturnsEmptyList()
    {
        var factory = EventFormatterFactory.CreateDefault();
        var result = factory.Validate();
        Assert.Empty(result);
    }

    [Fact]
    public void Validate_EmptyFactory_ReturnsProblems()
    {
        var factory = new EventFormatterFactory();
        var result = factory.Validate();
        Assert.NotEmpty(result);
        Assert.Contains("no registered formatters", string.Join(" ", result).ToLowerInvariant());
    }

    [Fact]
    public void IsValid_ValidFactory_ReturnsTrue()
    {
        var factory = EventFormatterFactory.CreateDefault();
        Assert.True(factory.IsValid());
    }

    [Fact]
    public void IsValid_InvalidFactory_ReturnsFalse()
    {
        var factory = new EventFormatterFactory();
        Assert.False(factory.IsValid());
    }

    [Fact]
    public void IsValid_NullFactory_ReturnsFalse()
    {
        EventFormatterFactory? factory = null;
        Assert.False(factory.IsValid());
    }

    [Fact]
    public void EnsureValid_ValidFactory_DoesNotThrow()
    {
        var factory = EventFormatterFactory.CreateDefault();
        factory.EnsureValid();
    }

    [Fact]
    public void EnsureValid_InvalidFactory_ThrowsArgumentException()
    {
        var factory = new EventFormatterFactory();
        Assert.Throws<ArgumentException>(() => factory.EnsureValid());
    }

    [Fact]
    public void EnsureValid_NullFactory_ThrowsArgumentNullException()
    {
        EventFormatterFactory? factory = null;
        Assert.Throws<ArgumentNullException>(() => factory.EnsureValid());
    }
}
