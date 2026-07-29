// Copyright (c) 2024
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Reflection;
using DotnetEventBus.Configuration;
using DotnetEventBus.Middleware;
using Xunit;

namespace DotnetEventBus.Tests;

public class PipelineBuilderExtensionsJsonExtensionsTests
{
    private static void SetMiddlewareCount(PipelineBuilder builder, int count)
    {
        // Access the private _middlewares field and fill it with dummy delegates
        var field = typeof(PipelineBuilder).GetField(
            "_middlewares",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (field == null)
            throw new InvalidOperationException("Unable to locate _middlewares field on PipelineBuilder.");

        var list = new List<Func<EventBusMiddleware, EventBusMiddleware>>();
        for (int i = 0; i < count; i++)
        {
            // A no‑op middleware that just returns the input
            list.Add(m => m);
        }

        field.SetValue(builder, list);
    }

    [Fact]
    public void ToJson_NullBuilder_ThrowsArgumentNullException()
    {
        PipelineBuilder? builder = null;
        Assert.Throws<ArgumentNullException>(() => builder!.ToJson());
    }

    [Fact]
    public void ToJson_WithBuilder_ReturnsValidJson()
    {
        var builder = new PipelineBuilder();
        SetMiddlewareCount(builder, 3);

        string json = builder.ToJson(indented: false);
        Assert.False(string.IsNullOrWhiteSpace(json));

        // The JSON should contain the middleware count we set (3)
        Assert.Contains(@"""middlewareCount"":3", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToJson_WithIndentation_ProducesReadableJson()
    {
        var builder = new PipelineBuilder();
        SetMiddlewareCount(builder, 1);

        string json = builder.ToJson(indented: true);
        // Indented JSON should contain line breaks
        Assert.Contains('\n', json);
    }

    [Fact]
    public void FromJson_NullJson_ThrowsArgumentNullException()
    {
        string? json = null;
        Assert.Throws<ArgumentNullException>(() => PipelineBuilderExtensionsJsonExtensions.FromJson(json!));
    }

    [Fact]
    public void FromJson_EmptyString_ReturnsNull()
    {
        var result = PipelineBuilderExtensionsJsonExtensions.FromJson(string.Empty);
        Assert.Null(result);
    }

    [Fact]
    public void FromJson_ValidJson_ReturnsBuilder()
    {
        var builder = new PipelineBuilder();
        SetMiddlewareCount(builder, 2);
        string json = builder.ToJson();

        var deserialized = PipelineBuilderExtensionsJsonExtensions.FromJson(json);
        Assert.NotNull(deserialized);
        // The deserialized builder is a new instance; we only verify it is created.
    }

    [Fact]
    public void TryFromJson_NullJson_ThrowsArgumentNullException()
    {
        string? json = null;
        Assert.Throws<ArgumentNullException>(() => PipelineBuilderExtensionsJsonExtensions.TryFromJson(json!, out _));
    }

    [Fact]
    public void TryFromJson_EmptyString_ReturnsTrueAndNullBuilder()
    {
        bool success = PipelineBuilderExtensionsJsonExtensions.TryFromJson(string.Empty, out var builder);
        Assert.True(success);
        Assert.Null(builder);
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalse()
    {
        bool success = PipelineBuilderExtensionsJsonExtensions.TryFromJson("{{invalid json}}", out var builder);
        Assert.False(success);
        Assert.Null(builder);
    }

    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndBuilder()
    {
        var builder = new PipelineBuilder();
        SetMiddlewareCount(builder, 4);
        string json = builder.ToJson();

        bool success = PipelineBuilderExtensionsJsonExtensions.TryFromJson(json, out var deserialized);
        Assert.True(success);
        Assert.NotNull(deserialized);
    }
}
