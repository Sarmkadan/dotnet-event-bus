using DotnetEventBus.Models;
using FluentAssertions;
using Xunit;

namespace DotnetEventBus.Tests;

public class DeadLetterEntryTests
{
    private readonly EventMessage _validMessage;
    private readonly string _validHandlerName = "TestHandler";
    private readonly Exception _validException = new InvalidOperationException("Test exception");

    public DeadLetterEntryTests()
    {
        _validMessage = new EventMessage("TestEvent", "{}");
    }

    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var maxRetryAttempts = 5;

        // Act
        var entry = new DeadLetterEntry(_validMessage, _validHandlerName, _validException, maxRetryAttempts);

        // Assert
        entry.Id.Should().NotBeNullOrEmpty();
        entry.Message.Should().BeSameAs(_validMessage);
        entry.FailedHandlerName.Should().Be(_validHandlerName);
        entry.ExceptionMessage.Should().Be(_validException.Message);
        entry.ExceptionStackTrace.Should().Be(_validException.ToString());
        entry.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(1));
        entry.MaxRetryAttempts.Should().Be(maxRetryAttempts);
        entry.Status.Should().Be(DeadLetterStatus.Pending);
        entry.StatusReason.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullMessage_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new DeadLetterEntry(null!, _validHandlerName, _validException);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("message");
    }

    [Fact]
    public void Constructor_WithNullHandlerName_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new DeadLetterEntry(_validMessage, null!, _validException);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("failedHandlerName");
    }

    [Fact]
    public void Constructor_WithNullException_SetsDefaultMessageAndNullStackTrace()
    {
        // Act
        var entry = new DeadLetterEntry(_validMessage, _validHandlerName, null!);

        // Assert
        entry.ExceptionMessage.Should().Be("Unknown exception");
        entry.ExceptionStackTrace.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithDefaultMaxRetryAttempts_SetsToThree()
    {
        // Act
        var entry = new DeadLetterEntry(_validMessage, _validHandlerName, _validException);

        // Assert
        entry.MaxRetryAttempts.Should().Be(3);
    }

    [Fact]
    public void MarkAsReviewed_SetsStatusAndReason()
    {
        // Arrange
        var entry = new DeadLetterEntry(_validMessage, _validHandlerName, _validException);
        const string reason = "Reviewed manually";

        // Act
        entry.MarkAsReviewed(reason);

        // Assert
        entry.Status.Should().Be(DeadLetterStatus.ReviewedNotProcessed);
        entry.StatusReason.Should().Be(reason);
    }

    [Fact]
    public void MarkAsReviewed_WithNullReason_SetsStatusReasonToNull()
    {
        // Arrange
        var entry = new DeadLetterEntry(_validMessage, _validHandlerName, _validException);

        // Act
        entry.MarkAsReviewed(null);

        // Assert
        entry.Status.Should().Be(DeadLetterStatus.ReviewedNotProcessed);
        entry.StatusReason.Should().BeNull();
    }

    [Fact]
    public void MarkAsReprocessed_SetsStatusAndReason()
    {
        // Arrange
        var entry = new DeadLetterEntry(_validMessage, _validHandlerName, _validException);

        // Act
        entry.MarkAsReprocessed();

        // Assert
        entry.Status.Should().Be(DeadLetterStatus.Reprocessed);
        entry.StatusReason.Should().Be("Successfully reprocessed");
    }

    [Fact]
    public void MarkAsReprocessFailed_SetsStatusAndReason()
    {
        // Arrange
        var entry = new DeadLetterEntry(_validMessage, _validHandlerName, _validException);
        const string reason = "Still failing";

        // Act
        entry.MarkAsReprocessFailed(reason);

        // Assert
        entry.Status.Should().Be(DeadLetterStatus.ReprocessFailed);
        entry.StatusReason.Should().Be(reason);
    }

    [Fact]
    public void GetSummary_ReturnsExpectedFormat()
    {
        // Arrange
        var entry = new DeadLetterEntry(_validMessage, _validHandlerName, _validException);
        var expected = $"Dead Letter [{entry.Id}]: {_validHandlerName} failed to process {_validMessage.EventType}" +
                       $" at {entry.CreatedAtUtc:O}. Error: {_validException.Message}";

        // Act
        var actual = entry.GetSummary();

        // Assert
        actual.Should().Be(expected);
    }
}