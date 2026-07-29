#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using DotnetEventBus.Formatters;
using Xunit;

namespace DotnetEventBus.Tests;

public sealed class EventFormatterFactoryTests
{
    [Fact]
    public void CreateDefault_ReturnsFactoryWithDefaultFormatters()
    {
        var factory = EventFormatterFactory.CreateDefault();
        Assert.NotNull(factory);
        Assert.Equal(3, factory.GetAllFormatters().Count());
        Assert.Contains(factory.GetFormatter("json"), factory.GetAllFormatters());
        Assert.Contains(factory.GetFormatter("csv"), factory.GetAllFormatters());
        Assert.Contains(factory.GetFormatter("xml"), factory.GetAllFormatters());
    }

    [Fact]
    public void Register_FormatterAddedToFactory()
    {
        var factory = new EventFormatterFactory();
        var formatter = new JsonEventFormatter();
        factory.Register(formatter);
        Assert.NotNull(factory.GetFormatter("json"));
    }

    [Fact]
    public void GetFormatter_FormatterFound()
    {
        var factory = new EventFormatterFactory();
        var formatter = new JsonEventFormatter();
        factory.Register(formatter);
        Assert.NotNull(factory.GetFormatter("json"));
    }

    [Fact]
    public void GetFormatter_FormatterNotFound_ReturnsNull()
    {
        var factory = new EventFormatterFactory();
        Assert.Null(factory.GetFormatter("unknown"));
    }

    [Fact]
    public void GetFormatterByContentType_FormatterFound()
    {
        var factory = new EventFormatterFactory();
        var formatter = new JsonEventFormatter();
        factory.Register(formatter);
        Assert.NotNull(factory.GetFormatterByContentType("application/json"));
    }

    [Fact]
    public void GetFormatterByContentType_FormatterNotFound_ReturnsNull()
    {
        var factory = new EventFormatterFactory();
        Assert.Null(factory.GetFormatterByContentType("unknown"));
    }

    [Fact]
    public void GetAllFormatters_ReturnsAllRegisteredFormatters()
    {
        var factory = new EventFormatterFactory();
        var formatter1 = new JsonEventFormatter();
        var formatter2 = new CsvEventFormatter();
        factory.Register(formatter1);
        factory.Register(formatter2);
        Assert.Equal(2, factory.GetAllFormatters().Count());
    }

    [Fact]
    public void GetSupportedFormats_ReturnsAllSupportedFormats()
    {
        var factory = new EventFormatterFactory();
        var formatter1 = new JsonEventFormatter();
        var formatter2 = new CsvEventFormatter();
        factory.Register(formatter1);
        factory.Register(formatter2);
        Assert.Equal(2, factory.GetSupportedFormats().Count());
    }

    [Fact]
    public void IsFormatSupported_FormatSupported_ReturnsTrue()
    {
        var factory = new EventFormatterFactory();
        var formatter = new JsonEventFormatter();
        factory.Register(formatter);
        Assert.True(factory.IsFormatSupported("json"));
    }

    [Fact]
    public void IsFormatSupported_FormatNotSupported_ReturnsFalse()
    {
        var factory = new EventFormatterFactory();
        Assert.False(factory.IsFormatSupported("unknown"));
    }

    [Fact]
    public void Unregister_FormatRemovedFromFactory()
    {
        var factory = new EventFormatterFactory();
        var formatter = new JsonEventFormatter();
        factory.Register(formatter);
        factory.Unregister("json");
        Assert.Null(factory.GetFormatter("json"));
    }

    [Fact]
    public void Unregister_FormatNotRegistered_DoesNothing()
    {
        var factory = new EventFormatterFactory();
        factory.Unregister("json");
        Assert.NotNull(factory.GetFormatter("json"));
    }
}
