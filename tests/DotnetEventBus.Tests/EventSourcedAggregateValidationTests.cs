using DotnetEventBus.Advanced;
using FluentAssertions;
using Xunit;

namespace DotnetEventBus.Tests;

public class EventSourcedAggregateValidationTests
{
    private class TestAggregate : EventSourcedAggregate
    {
        public TestAggregate(string? id, int version = 0)
        {
            Id = id;
            if (version != 0)
            {
                LoadSnapshot(new AggregateSnapshot { AggregateId = id, Version = version });
            }
        }
    }

    [Fact]
    public void Validate_WithValidAggregate_ReturnsEmptyList()
    {
        // Arrange
        var aggregate = new TestAggregate("test-id", 1);

        // Act
        var errors = aggregate.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithNullId_ReturnsError()
    {
        // Arrange
        var aggregate = new TestAggregate(null, 1);

        // Act
        var errors = aggregate.Validate();

        // Assert
        errors.Should().ContainSingle().Which.Should().Be("Aggregate Id is null, empty, or whitespace.");
    }

    [Fact]
    public void Validate_WithNegativeVersion_ReturnsError()
    {
        // Arrange
        var aggregate = new TestAggregate("test-id", -1);

        // Act
        var errors = aggregate.Validate();

        // Assert
        errors.Should().ContainSingle().Which.Should().Be("Aggregate Version is negative, which is invalid.");
    }

    [Fact]
    public void IsValid_WithValidAggregate_ReturnsTrue()
    {
        // Arrange
        var aggregate = new TestAggregate("test-id", 1);

        // Act
        var isValid = aggregate.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithInvalidAggregate_ReturnsFalse()
    {
        // Arrange
        var aggregate = new TestAggregate(null, 1);

        // Act
        var isValid = aggregate.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void EnsureValid_WithValidAggregate_DoesNotThrow()
    {
        // Arrange
        var aggregate = new TestAggregate("test-id", 1);

        // Act
        Action act = () => aggregate.EnsureValid();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureValid_WithInvalidAggregate_ThrowsArgumentException()
    {
        // Arrange
        var aggregate = new TestAggregate(null, -1);

        // Act
        Action act = () => aggregate.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Aggregate Id is null, empty, or whitespace.*")
            .WithMessage("*Aggregate Version is negative, which is invalid.*");
    }

    [Fact]
    public void Validate_WithNullAggregate_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((EventSourcedAggregate)null!).Validate());
    }
}
