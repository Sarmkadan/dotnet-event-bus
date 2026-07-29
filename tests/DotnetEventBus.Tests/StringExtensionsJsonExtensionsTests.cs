using System;
using System.Text.Json;
using Xunit;
using DotnetEventBus.Utilities;

namespace DotnetEventBus.Tests
{
    public class StringExtensionsJsonExtensionsTests
    {
        [Fact]
        public void ToJson_Happy_PATH_NULL_INPUT()
        {
            // Arrange
            string input = null;

            // Act
            var result = StringExtensionsJsonExtensions.ToJson(input);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ToJson_HAPPY_PATH_EMPTY_INPUT()
        {
            // Arrange
            string input = "";

            // Act
            var result = StringExtensionsJsonExtensions.ToJson(input);

            // Assert
            Assert.Equal("\"\"", result);
        }

        [Fact]
        public void ToJson_HAPPY_PATH_NONEMPTY_INPUT()
        {
            // Arrange
            string input = "Hello World!";

            // Act
            var result = StringExtensionsJsonExtensions.ToJson(input);

            // Assert
            Assert.Equal("\"Hello World!\"", result);
        }

        [Fact]
        public void FromJson_HAPPY_PATH_NULL_INPUT()
        {
            // Arrange
            string input = "null";

            // Act
            var result = StringExtensionsJsonExtensions.FromJson(input);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FromJson_HAPPY_PATH_EMPTY_INPUT()
        {
            // Arrange
            string input = "";

            // Act
            var result = StringExtensionsJsonExtensions.FromJson(input);

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void FromJson_HAPPY_PATH_NONEMPTY_INPUT()
        {
            // Arrange
            string input = "Hello World!";

            // Act
            var result = StringExtensionsJsonExtensions.FromJson(input);

            // Assert
            Assert.Equal("Hello World!", result);
        }

        [Fact]
        public void TryFromJson_HAPPY_PATH_NULL_INPUT()
        {
            // Arrange
            string input = null;
            string? result = null;

            // Act
            var succeeded = StringExtensionsJsonExtensions.TryFromJson(input, out result);

            // Assert
            Assert.False(succeeded);
            Assert.Null(result);
        }

        [Fact]
        public void TryFromJson_HAPPY_PATH_EMPTY_INPUT()
        {
            // Arrange
            string input = "";
            string? result = null;

            // Act
            var succeeded = StringExtensionsJsonExtensions.TryFromJson(input, out result);

            // Assert
            Assert.True(succeeded);
            Assert.Equal("", result);
        }

        [Fact]
        public void TryFromJson_HAPPY_PATH_NONEMPTY_INPUT()
        {
            // Arrange
            string input = "Hello World!";
            string? result = null;

            // Act
            var succeeded = StringExtensionsJsonExtensions.TryFromJson(input, out result);

            // Assert
            Assert.True(succeeded);
            Assert.Equal("Hello World!", result);
        }
    }
}