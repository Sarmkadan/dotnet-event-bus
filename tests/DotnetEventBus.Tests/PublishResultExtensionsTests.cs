using System;
using DotnetEventBus.Models;
using DotnetEventBus.Exceptions;
using Xunit;

namespace DotnetEventBus.Tests
{
    public class PublishResultExtensionsTests
    {
        [Fact]
        public void Match_ReturnsOnSuccess_WhenResultIsSuccessful()
        {
            // Arrange
            var result = PublishResult.CreateSuccess("msg-1");

            // Act
            var value = result.Match(
                onSuccess: () => "success",
                onFailure: err => $"failed: {err}");

            // Assert
            Assert.Equal("success", value);
        }

        [Fact]
        public void Match_ReturnsOnFailure_WithErrorMessage_WhenResultIsFailed()
        {
            // Arrange
            var ex = new InvalidOperationException("boom");
            var result = PublishResult.CreateFailed("msg-2", ex);

            // Act
            var value = result.Match(
                onSuccess: () => "success",
                onFailure: err => $"failed: {err}");

            // Assert
            Assert.Equal($"failed: {ex.Message}", value);
        }

        [Fact]
        public void ThrowIfFailed_DoesNotThrow_WhenResultIsSuccessful()
        {
            // Arrange
            var result = PublishResult.CreateSuccess("msg-3");

            // Act & Assert
            var exception = Record.Exception(() => result.ThrowIfFailed());
            Assert.Null(exception);
        }

        [Fact]
        public void ThrowIfFailed_ThrowsEventBusException_WithErrorMessage_WhenResultIsFailed()
        {
            // Arrange
            var ex = new InvalidOperationException("something went wrong");
            var result = PublishResult.CreateFailed("msg-4", ex);

            // Act & Assert
            var thrown = Assert.Throws<EventBusException>(() => result.ThrowIfFailed());
            Assert.Equal(ex.Message, thrown.Message);
        }
    }
}
