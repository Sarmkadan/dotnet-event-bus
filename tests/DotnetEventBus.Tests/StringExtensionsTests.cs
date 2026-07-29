using System;
using System.Collections.Generic;
using DotnetEventBus.Utilities;
using Xunit;

namespace DotnetEventBus.Tests;

public class StringExtensionsTests
{
    [Fact]
    public void ToPascalCase_ConvertsCorrectly()
    {
        // Arrange
        var input = "user_created-event name";

        // Act
        var result = input.ToPascalCase();

        // Assert
        Assert.Equal("UserCreatedEventName", result);
    }

    [Fact]
    public void ToPascalCase_NullInput_ThrowsArgumentNullException()
    {
        string? input = null;
        Assert.Throws<ArgumentNullException>(() => input!.ToPascalCase());
    }

    [Fact]
    public void ToSnakeCase_ConvertsCorrectly()
    {
        var input = "UserCreatedEvent";

        var result = input.ToSnakeCase();

        Assert.Equal("user_created_event", result);
    }

    [Fact]
    public void ToKebabCase_ConvertsCorrectly()
    {
        var input = "UserCreatedEvent";

        var result = input.ToKebabCase();

        Assert.Equal("user-created-event", result);
    }

    [Theory]
    [InlineData("Order.Created", true)]
    [InlineData("order_created", true)]
    [InlineData("order-created", false)] // dash not allowed
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidEventTypeName_ValidatesCorrectly(string? input, bool expected)
    {
        if (input is null)
        {
            Assert.Throws<ArgumentNullException>(() => input!.IsValidEventTypeName());
        }
        else
        {
            var result = input.IsValidEventTypeName();
            Assert.Equal(expected, result);
        }
    }

    [Fact]
    public void Truncate_RespectsMaxLengthAndEllipsis()
    {
        var input = "HelloWorld";

        var truncated = input.Truncate(5);
        Assert.Equal("Hello", truncated);

        var truncatedEllipsis = input.Truncate(5, addEllipsis: true);
        Assert.Equal("Hello...", truncatedEllipsis);
    }

    [Fact]
    public void Truncate_InvalidArguments_Throw()
    {
        string? input = null;
        Assert.Throws<ArgumentNullException>(() => input!.Truncate(5));

        Assert.Throws<ArgumentOutOfRangeException>(() => "test".Truncate(-1));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("abc", false)]
    public void IsNullOrWhitespace_BehavesAsExpected(string? input, bool expected)
    {
        var result = input.IsNullOrWhitespace();
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToSlug_ConvertsAndHandlesEdgeCases()
    {
        var input = "Hello World! 2023";
        var slug = input.ToSlug();

        Assert.Equal("hello-world-2023", slug);
    }

    [Fact]
    public void ToSlug_NullInput_ThrowsArgumentNullException()
    {
        string? input = null;
        Assert.Throws<ArgumentNullException>(() => input!.ToSlug());
    }

    [Theory]
    [InlineData("user.created.event", "user")]
    [InlineData("UserCreatedEvent", "User")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void GetEventCategory_ExtractsCorrectly(string? input, string expected)
    {
        if (input is null)
        {
            Assert.Throws<ArgumentNullException>(() => input!.GetEventCategory());
        }
        else
        {
            var result = input.GetEventCategory();
            Assert.Equal(expected, result);
        }
    }

    [Fact]
    public void Repeat_RepeatsStringAndHandlesErrors()
    {
        var result = "ab".Repeat(3);
        Assert.Equal("ababab", result);

        var empty = "x".Repeat(0);
        Assert.Equal(string.Empty, empty);
    }

    [Fact]
    public void Repeat_InvalidArguments_Throw()
    {
        string? input = null;
        Assert.Throws<ArgumentNullException>(() => input!.Repeat(2));

        Assert.Throws<ArgumentOutOfRangeException>(() => "test".Repeat(-5));
    }
}
