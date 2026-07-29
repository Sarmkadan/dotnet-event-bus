#nullable enable

using System;
using System.Collections.Generic;
using DotnetEventBus.Utilities;
using Xunit;

namespace DotnetEventBus.Tests;

public sealed class ValidationHelperTests
{
    [Fact]
    public void RequireString_ValidInputs_PassesValidation()
    {
        var validator = new ValidationHelper();
        validator.RequireNotEmpty("test", "Field1")
                 .RequireLength("test", 1, 10, "Field2")
                 .RequirePattern("abc", "^[a-z]+$", "Field3", "Invalid")
                 .RequireValidEmail("test@example.com", "Email")
                 .RequireValidUrl("http://example.com", "Url");

        Assert.True(validator.IsValid);
        Assert.Empty(validator.GetErrors());
    }

    [Fact]
    public void RequireString_InvalidInputs_AddsErrors()
    {
        var validator = new ValidationHelper();
        validator.RequireNotEmpty("", "EmptyField")
                 .RequireLength("toolongtext", 1, 5, "LengthField")
                 .RequirePattern("123", "^[a-z]+$", "PatternField", "Letters only")
                 .RequireValidEmail("invalid-email", "EmailField")
                 .RequireValidUrl("not-a-url", "UrlField");

        Assert.False(validator.IsValid);
        Assert.Equal(5, validator.GetErrors().Count);
    }

    [Fact]
    public void RequireNotNullAndCondition_InvalidInputs_AddsErrors()
    {
        var validator = new ValidationHelper();
        validator.RequireNotNull<string>(null, "NullField")
                 .RequireCondition(false, "Condition failed");

        Assert.False(validator.IsValid);
        Assert.Equal(2, validator.GetErrors().Count);
    }

    [Fact]
    public void RequireRange_OutOfRange_AddsError()
    {
        var validator = new ValidationHelper();
        validator.RequireRange(10, 1, 5, "IntRange")
                 .RequireRange(10.5, 1.0, 5.0, "DoubleRange");

        Assert.False(validator.IsValid);
        Assert.Equal(2, validator.GetErrors().Count);
    }

    [Fact]
    public void RequireItems_BoundaryChecks_AddsErrors()
    {
        var validator = new ValidationHelper();
        var list = new List<int> { 1, 2, 3 };
        validator.RequireMinimumItems(list, 5, "MinItems")
                 .RequireMaximumItems(list, 2, "MaxItems");

        Assert.False(validator.IsValid);
        Assert.Equal(2, validator.GetErrors().Count);
    }

    [Fact]
    public void ThrowIfInvalid_HasErrors_ThrowsValidationException()
    {
        var validator = new ValidationHelper();
        validator.RequireNotEmpty("", "Field");

        var ex = Assert.Throws<ValidationException>(() => validator.ThrowIfInvalid());
        Assert.Contains("Field", ex.Message);
    }
}
