#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using Xunit;
using DotnetEventBus.Models;

/// <summary>
/// Contains unit tests for the <see cref="PublishResult"/> model.
/// </summary>
public sealed class PublishResultTests
{
    /// <summary>
    /// Verifies that <see cref="PublishResult.AddSuccessfulHandler(string)"/> increments the
    /// <see cref="PublishResult.HandlersInvoked"/> count and appends the handler names to the
    /// <see cref="PublishResult.SuccessfulHandlers"/> collection in the order they were added.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="PublishResult.AddFailedHandler(string, Exception)"/> increments the
    /// failed handler count, records each failed handler name, and captures the first exception
    /// (including its message) as the result's <see cref="PublishResult.Exception"/> and
    /// <see cref="PublishResult.ErrorMessage"/>.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="PublishResult.CreateFailed(string, Exception)"/> creates a result
    /// marked as unsuccessful, populates the <see cref="PublishResult.Exception"/> and
    /// <see cref="PublishResult.ErrorMessage"/> properties with the supplied exception, and
    /// preserves the supplied message identifier.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="PublishResult.GetSummary()"/> returns a string containing the
    /// message identifier, a success indicator, and the number of successful handlers when the
    /// result represents a successful publish operation.
    /// </summary>
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
