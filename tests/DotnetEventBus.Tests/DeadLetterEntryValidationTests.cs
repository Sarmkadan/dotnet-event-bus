using DotnetEventBus.Models;
using Xunit;

namespace DotnetEventBus.Tests
{
    public class DeadLetterEntryValidationTests
    {
        [Fact]
        public void Validate_HappyPath_ReturnsEmptyList()
        {
            // Arrange
            var deadLetterEntry = new DeadLetterEntry(new EventMessage("EventType", "EventPayload"), "FailedHandlerName", new Exception("ExceptionMessage"), 0);

            // Act
            var errors = DeadLetterEntryValidation.Validate(deadLetterEntry);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void IsValid_HappyPath_ReturnsTrue()
        {
            // Arrange
            var deadLetterEntry = new DeadLetterEntry(new EventMessage("EventType", "EventPayload"), "FailedHandlerName", new Exception("ExceptionMessage"), 0);

            // Act
            var isValid = DeadLetterEntryValidation.IsValid(deadLetterEntry);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void EnsureValid_HappyPath_DoesNotThrow()
        {
            // Arrange
            var deadLetterEntry = new DeadLetterEntry(new EventMessage("EventType", "EventPayload"), "FailedHandlerName", new Exception("ExceptionMessage"), 0);

            // Act and Assert
            DeadLetterEntryValidation.EnsureValid(deadLetterEntry);
        }

        [Fact]
        public void Validate_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => DeadLetterEntryValidation.Validate(null));
        }

        [Fact]
        public void IsValid_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => DeadLetterEntryValidation.IsValid(null));
        }

        [Fact]
        public void EnsureValid_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => DeadLetterEntryValidation.EnsureValid(null));
        }

        [Fact]
        public void EnsureValid_InvalidInput_ThrowsArgumentException()
        {
            // Arrange
            var deadLetterEntry = new DeadLetterEntry(new EventMessage("", ""), "", new Exception(""), 0);

            // Act and Assert
            Assert.Throws<ArgumentException>(() => DeadLetterEntryValidation.EnsureValid(deadLetterEntry));
        }
    }
}
