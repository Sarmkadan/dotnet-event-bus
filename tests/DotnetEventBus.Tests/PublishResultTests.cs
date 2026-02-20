#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using Xunit;
using DotnetEventBus.Models;

namespace DotnetEventBus.Tests;

public sealed class PublishResultTests
{
    [Fact]
    public void AddSuccessfulHandler_ShouldIncrementHandlersInvokedAndAppendToList()
    {
        // Arrange
        var result = new PublishResult("msg-001");

        // Act
        result.AddSuccessfulHandler("HandlerAlpha");
        result.AddSuccessfulHandler("HandlerBeta");

        // Assert
        result.HandlersInvoked.Should().Be(2);
        result.SuccessfulHandlers.Should().ContainInOrder("HandlerAlpha", "HandlerBeta");
        result.FailedHandlers.Should().Be(0);
    }

    [Fact]
    public void AddFailedHandler_ShouldIncrementFailedCountAndCaptureFirstException()
    {
        // Arrange
        var result = new PublishResult("msg-002");
        var firstEx = new InvalidOperationException("First failure");
        var secondEx = new TimeoutException("Second failure");

        // Act
        result.AddFailedHandler("HandlerX", firstEx);
        result.AddFailedHandler("HandlerY", secondEx);

        // Assert
        result.FailedHandlers.Should().Be(2);
        result.FailedHandlerNames.Should().Contain("HandlerX").And.Contain("HandlerY");
        result.Exception.Should().BeSameAs(firstEx);
        result.ErrorMessage.Should().Be(firstEx.Message);
    }

    [Fact]
    public void CreateFailed_ShouldPopulateExceptionAndMarkAsUnsuccessful()
    {
        // Arrange
        var ex = new Exception("Infrastructure failure");

        // Act
        var result = PublishResult.CreateFailed("msg-003", ex);

        // Assert
        result.Success.Should().BeFalse();
        result.Exception.Should().BeSameAs(ex);
        result.ErrorMessage.Should().Be(ex.Message);
        result.MessageId.Should().Be("msg-003");
    }

    [Fact]
    public void GetSummary_ShouldIncludeMessageIdAndSuccessIndicator()
    {
        // Arrange
        var result = PublishResult.CreateSuccess("msg-004", 3);

        // Act
        var summary = result.GetSummary();

        // Assert
        summary.Should().Contain("msg-004");
        summary.Should().Contain("Success");
        summary.Should().Contain("3");
    }
}
