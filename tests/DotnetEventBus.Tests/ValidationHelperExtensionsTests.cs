using System;
using System.Collections.Generic;
using System.Reflection;
using DotnetEventBus.Utilities;
using Xunit;

namespace DotnetEventBus.Tests;

public class ValidationHelperExtensionsTests
{
    private static List<string> GetErrors(ValidationHelper helper)
    {
        var field = typeof(ValidationHelper).GetField("_errors", BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(helper) as List<string> ?? new List<string>();
    }

    [Fact]
    public void RequireNotEmpty_AddsError_WhenValueIsNullOrWhiteSpace()
    {
        var helper = new ValidationHelper();
        helper.RequireNotEmpty(null, "Name", "Name is required");
        helper.RequireNotEmpty("   ", "Name", "Name is required");

        var errors = GetErrors(helper);
        Assert.Equal(2, errors.Count);
        Assert.All(errors, e => Assert.Equal("Name is required", e));
    }

    [Fact]
    public void RequireNotEmpty_DoesNotAddError_WhenValueIsValid()
    {
        var helper = new ValidationHelper();
        helper.RequireNotEmpty("John", "Name", "Name is required");

        var errors = GetErrors(helper);
        Assert.Empty(errors);
    }

    [Fact]
    public void RequireNotNull_AddsError_WhenValueIsNull()
    {
        var helper = new ValidationHelper();
        helper.RequireNotNull<string>(null, "Email", "Email required");

        var errors = GetErrors(helper);
        Assert.Single(errors);
        Assert.Equal("Email required", errors[0]);
    }

    [Fact]
    public void RequireNotNull_DoesNotAddError_WhenValueIsNotNull()
    {
        var helper = new ValidationHelper();
        helper.RequireNotNull("value", "Email", "Email required");

        var errors = GetErrors(helper);
        Assert.Empty(errors);
    }

    [Fact]
    public void RequireNotEmptyCollection_AddsError_WhenCollectionIsNullOrEmpty()
    {
        var helper = new ValidationHelper();
        helper.RequireNotEmpty<int>(null, "Numbers");
        helper.RequireNotEmpty(new List<int>(), "Numbers");

        var errors = GetErrors(helper);
        Assert.Equal(2, errors.Count);
        Assert.All(errors, e => Assert.Equal("Numbers collection is required and cannot be empty", e));
    }

    [Fact]
    public void RequireNotEmptyCollection_DoesNotAddError_WhenCollectionHasItems()
    {
        var helper = new ValidationHelper();
        helper.RequireNotEmpty(new[] { 1, 2 }, "Numbers");

        var errors = GetErrors(helper);
        Assert.Empty(errors);
    }

    [Fact]
    public void RequirePattern_AddsError_WhenPatternDoesNotMatch()
    {
        var helper = new ValidationHelper();
        helper.RequirePattern("abc", @"^\d+$", "Code", "must be numeric");

        var errors = GetErrors(helper);
        Assert.Single(errors);
        Assert.Equal("Code: must be numeric", errors[0]);
    }

    [Fact]
    public void RequirePattern_DoesNotAddError_WhenPatternMatches()
    {
        var helper = new ValidationHelper();
        helper.RequirePattern("123", @"^\d+$", "Code", "must be numeric");

        var errors = GetErrors(helper);
        Assert.Empty(errors);
    }

    [Fact]
    public void RequireAlphanumeric_AddsError_WhenContainsNonAlphanumeric()
    {
        var helper = new ValidationHelper();
        helper.RequireAlphanumeric("abc$", "Tag");

        var errors = GetErrors(helper);
        Assert.Single(errors);
        Assert.Equal("Tag must contain only alphanumeric characters", errors[0]);
    }

    [Fact]
    public void RequireAlphanumeric_DoesNotAddError_WhenAlphanumeric()
    {
        var helper = new ValidationHelper();
        helper.RequireAlphanumeric("abc123", "Tag");

        var errors = GetErrors(helper);
        Assert.Empty(errors);
    }

    [Fact]
    public void RequireAlphabetic_AddsError_WhenContainsNonAlphabetic()
    {
        var helper = new ValidationHelper();
        helper.RequireAlphabetic("abc1", "Name");

        var errors = GetErrors(helper);
        Assert.Single(errors);
        Assert.Equal("Name must contain only alphabetic characters", errors[0]);
    }

    [Fact]
    public void RequireAlphabetic_DoesNotAddError_WhenAlphabetic()
    {
        var helper = new ValidationHelper();
        helper.RequireAlphabetic("abcXYZ", "Name");

        var errors = GetErrors(helper);
        Assert.Empty(errors);
    }

    [Fact]
    public void RequireNumeric_AddsError_WhenContainsNonNumeric()
    {
        var helper = new ValidationHelper();
        helper.RequireNumeric("12a3", "Count");

        var errors = GetErrors(helper);
        Assert.Single(errors);
        Assert.Equal("Count must contain only numeric characters", errors[0]);
    }

    [Fact]
    public void RequireNumeric_DoesNotAddError_WhenNumeric()
    {
        var helper = new ValidationHelper();
        helper.RequireNumeric("01234", "Count");

        var errors = GetErrors(helper);
        Assert.Empty(errors);
    }

    [Fact]
    public void RequireMinLength_AddsError_WhenTooShort()
    {
        var helper = new ValidationHelper();
        helper.RequireMinLength("ab", 3, "Password");

        var errors = GetErrors(helper);
        Assert.Single(errors);
        Assert.Equal("Password must be at least 3 characters long", errors[0]);
    }

    [Fact]
    public void RequireMinLength_DoesNotAddError_WhenLengthIsSufficient()
    {
        var helper = new ValidationHelper();
        helper.RequireMinLength("abcd", 3, "Password");

        var errors = GetErrors(helper);
        Assert.Empty(errors);
    }

    [Fact]
    public void RequireMaxLength_AddsError_WhenTooLong()
    {
        var helper = new ValidationHelper();
        helper.RequireMaxLength("abcdef", 5, "Code");

        var errors = GetErrors(helper);
        Assert.Single(errors);
        Assert.Equal("Code cannot exceed 5 characters", errors[0]);
    }

    [Fact]
    public void RequireMaxLength_DoesNotAddError_WhenLengthIsWithinLimit()
    {
        var helper = new ValidationHelper();
        helper.RequireMaxLength("abc", 5, "Code");

        var errors = GetErrors(helper);
        Assert.Empty(errors);
    }

    [Fact]
    public void RequireExactItems_AddsError_WhenCountDiffers()
    {
        var helper = new ValidationHelper();
        helper.RequireExactItems(new[] { 1, 2, 3 }, 2, "Numbers");

        var errors = GetErrors(helper);
        Assert.Single(errors);
        Assert.Equal("Numbers must contain exactly 2 items, but contains 3", errors[0]);
    }

    [Fact]
    public void RequireExactItems_DoesNotAddError_WhenCountMatches()
    {
        var helper = new ValidationHelper();
        helper.RequireExactItems(new[] { "a", "b" }, 2, "Letters");

        var errors = GetErrors(helper);
        Assert.Empty(errors);
    }

    [Fact]
    public void Methods_ThrowArgumentNullException_ForNullFieldName()
    {
        var helper = new ValidationHelper();

        Assert.Throws<ArgumentNullException>(() => helper.RequireNotEmpty("value", null!, "msg"));
        Assert.Throws<ArgumentNullException>(() => helper.RequireNotNull("value", null!, "msg"));
        Assert.Throws<ArgumentNullException>(() => helper.RequireNotEmpty<int>(new[] { 1 }, null!));
        Assert.Throws<ArgumentNullException>(() => helper.RequirePattern("value", @"\d+", null!, "msg"));
        Assert.Throws<ArgumentNullException>(() => helper.RequirePattern("value", null!, "field", "msg"));
        Assert.Throws<ArgumentNullException>(() => helper.RequirePattern(null, @"\d+", "field", null!));
        Assert.Throws<ArgumentNullException>(() => helper.RequireAlphanumeric("value", null!));
        Assert.Throws<ArgumentNullException>(() => helper.RequireAlphabetic("value", null!));
        Assert.Throws<ArgumentNullException>(() => helper.RequireNumeric("value", null!));
        Assert.Throws<ArgumentNullException>(() => helper.RequireMinLength("value", 1, null!));
        Assert.Throws<ArgumentNullException>(() => helper.RequireMaxLength("value", 1, null!));
        Assert.Throws<ArgumentNullException>(() => helper.RequireExactItems(new[] { 1 }, 1, null!));
    }
}
