using DotnetEventBus.Handlers;
using DotnetEventBus.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace DotnetEventBus.Tests;

/// <summary>
/// Contains unit tests for validating the configuration of PredicateSubscriptionBuilder.
/// </summary>
public class PredicateSubscriptionBuilderValidationTests
{
    private class TestEvent
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    /// <summary>
    /// Tests that Validate throws ArgumentNullException when the builder is null.
    /// </summary>
    [Fact]
    public void Validate_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        PredicateSubscriptionBuilder<TestEvent>? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.Validate<TestEvent>());
    }

    /// <summary>
    /// Tests that Validate returns an empty list when the builder is valid.
    /// </summary>
    [Fact]
    public void Validate_WithValidBuilder_ReturnsEmptyList()
    {
        // Arrange
        var mockEventBus = new Mock<IEventBus>();
        var builder = mockEventBus.Object.CreatePredicateSubscription<TestEvent>();
        builder.WithHandler(async (e, ct) => await Task.CompletedTask);

        // Act
        var errors = builder.Validate<TestEvent>();

        // Assert
        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Validate returns an error when no handler is configured.
    /// </summary>
    [Fact]
    public void Validate_WithHandlerNotConfigured_ReturnsError()
    {
        // Arrange
        var mockEventBus = new Mock<IEventBus>();
        var builder = mockEventBus.Object.CreatePredicateSubscription<TestEvent>();

        // Act
        var errors = builder.Validate<TestEvent>();

        // Assert
        errors.Should().HaveCount(1);
        errors[0].Should().Be("No handler configured. Call WithHandler before calling Register.");
    }

    /// <summary>
    /// Tests that Validate returns an error when the handler name is empty.
    /// </summary>
    [Fact]
    public void Validate_WithEmptyHandlerName_ReturnsError()
    {
        // Arrange
        var mockEventBus = new Mock<IEventBus>();
        var builder = mockEventBus.Object.CreatePredicateSubscription<TestEvent>();
        builder.WithHandler(async (e, ct) => await Task.CompletedTask);
        builder.WithHandlerName("   ");

        // Act
        var errors = builder.Validate<TestEvent>();

        // Assert
        errors.Should().HaveCount(1);
        errors[0].Should().Be("Handler name cannot be empty or whitespace.");
    }

    /// <summary>
    /// Tests that Validate returns an error when the handler name is null.
    /// </summary>
    [Fact]
    public void Validate_WithNullHandlerName_ReturnsError()
    {
        // Arrange
        var mockEventBus = new Mock<IEventBus>();
        var builder = mockEventBus.Object.CreatePredicateSubscription<TestEvent>();
        builder.WithHandler(async (e, ct) => await Task.CompletedTask);
        builder.WithHandlerName(null);

        // Act
        var errors = builder.Validate<TestEvent>();

        // Assert
        errors.Should().HaveCount(1);
        errors[0].Should().Be("Handler name cannot be empty or whitespace.");
    }

    /// <summary>
    /// Tests that Validate returns an error when the priority is below the minimum allowed value.
    /// </summary>
    [Fact]
    public void Validate_WithPriorityBelowMinimum_ReturnsError()
    {
        // Arrange
        var mockEventBus = new Mock<IEventBus>();
        var builder = mockEventBus.Object.CreatePredicateSubscription<TestEvent>();
        builder.WithHandler(async (e, ct) => await Task.CompletedTask);
        builder.WithPriority(-1001); // MinPriority is -1000

        // Act
        var errors = builder.Validate<TestEvent>();

        // Assert
        errors.Should().HaveCount(1);
        errors[0].Should().Be("Priority must be between -1000 and 1000.");
    }

    /// <summary>
    /// Tests that Validate returns an error when the priority is above the maximum allowed value.
    /// </summary>
    [Fact]
    public void Validate_WithPriorityAboveMaximum_ReturnsError()
    {
        // Arrange
        var mockEventBus = new Mock<IEventBus>();
        var builder = mockEventBus.Object.CreatePredicateSubscription<TestEvent>();
        builder.WithHandler(async (e, ct) => await Task.CompletedTask);
        builder.WithPriority(1001); // MaxPriority is 1000

        // Act
        var errors = builder.Validate<TestEvent>();

        // Assert
        errors.Should().HaveCount(1);
        errors[0].Should().Be("Priority must be between -1000 and 1000.");
    }

    /// <summary>
    /// Tests that Validate returns an empty list when the priority is valid.
    /// </summary>
    [Fact]
    public void Validate_WithValidPriority_ReturnsEmptyList()
    {
        // Arrange
        var mockEventBus = new Mock<IEventBus>();
        var builder = mockEventBus.Object.CreatePredicateSubscription<TestEvent>();
        builder.WithHandler(async (e, ct) => await Task.CompletedTask);
        builder.WithPriority(0); // Default priority

        // Act
        var errors = builder.Validate<TestEvent>();

        // Assert
        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Validate returns an empty list when the priority is within the valid boundaries.
    /// </summary>
    [Fact]
    public void Validate_WithValidPriorityBoundaries_ReturnsEmptyList()
    {
        // Arrange
        var mockEventBus = new Mock<IEventBus>();
        var builder = mockEventBus.Object.CreatePredicateSubscription<TestEvent>();
        builder.WithHandler(async (e, ct) => await Task.CompletedTask);
        builder.WithPriority(-1000); // Min boundary

        // Act
        var errors = builder.Validate<TestEvent>();

        // Assert
        errors.Should().BeEmpty();

        // Arrange
        builder.WithPriority(1000); // Max boundary

        // Act
        errors = builder.Validate<TestEvent>();

        // Assert
        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Validate returns all errors when there are multiple configuration issues.
    /// </summary>
    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var mockEventBus = new Mock<IEventBus>();
        var builder = mockEventBus.Object.CreatePredicateSubscription<TestEvent>();
        builder.WithHandlerName("   ");
        builder.WithPriority(-1001);

        // Act
        var errors = builder.Validate<TestEvent>();

        // Assert
        errors.Should().HaveCount(2);
        errors.Should().Contain("Handler name cannot be empty or whitespace.");
        errors.Should().Contain("Priority must be between -1000 and 1000.");
    }

    /// <summary>
    /// Tests that Validate returns a read-only list of errors.
    /// </summary>
    [Fact]
    public void Validate_ReturnsReadOnlyList()
    {
        // Arrange
        var mockEventBus = new Mock<IEventBus>();
        var builder = mockEventBus.Object.CreatePredicateSubscription<TestEvent>();
        builder.WithHandler(async (e, ct) => await Task.CompletedTask);

        // Act
        var errors = builder.Validate<TestEvent>();

        // Assert
        errors.Should().BeAssignableTo<IReadOnlyList<string>>();
    }

    /// <summary>
    /// Tests that IsValid throws ArgumentNullException when the builder is null.
    /// </summary>
    [Fact]
    public void IsValid_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        PredicateSubscriptionBuilder<TestEvent>? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.IsValid<TestEvent>());
    }

    /// <summary>
    /// Tests that IsValid returns true when the builder is valid.
    /// </summary>
    [Fact]
    public void IsValid_WithValidBuilder_ReturnsTrue()
    {
        // Arrange
        var mockEventBus = new Mock<IEventBus>();
        var builder = mockEventBus.Object.CreatePredicateSubscription<TestEvent>();
        builder.WithHandler(async (e, ct) => await Task.CompletedTask);

        // Act
        var isValid = builder.IsValid<TestEvent>();

        // Assert
        isValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests that IsValid returns false when no handler is configured.
    /// </summary>
    [Fact]
    public void IsValid_WithHandlerNotConfigured_ReturnsFalse()
    {
        // Arrange
        var mockEventBus = new Mock<IEventBus>();
        var builder = mockEventBus.Object.CreatePredicateSubscription<TestEvent>();

        // Act
        var isValid = builder.IsValid<TestEvent>();

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsValid returns false when the handler name is empty.
    /// </summary>
    [Fact]
    public void IsValid_WithEmptyHandlerName_ReturnsFalse()
    {
        // Arrange
        var mockEventBus = new Mock<IEventBus>();
        var builder = mockEventBus.Object.CreatePredicateSubscription<TestEvent>();
        builder.WithHandler(async (e, ct) => await Task.CompletedTask);
        builder.WithHandlerName("   ");

        // Act
        var isValid = builder.IsValid<TestEvent>();

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsValid returns false when the priority is out of the valid range.
    /// </summary>
    [Fact]
    public void IsValid_WithPriorityOutOfRange_ReturnsFalse()
    {
        // Arrange
        var mockEventBus = new Mock<IEventBus>();
        var builder = mockEventBus.Object.CreatePredicateSubscription<TestEvent>();
        builder.WithHandler(async (e, ct) => await Task.CompletedTask);
        builder.WithPriority(1001);

        // Act
        var isValid = builder.IsValid<TestEvent>();

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// Tests that EnsureValid throws ArgumentNullException when the builder is null.
    /// </summary>
    [Fact]
    public void EnsureValid_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        PredicateSubscriptionBuilder<TestEvent>? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.EnsureValid<TestEvent>());
    }

    /// <summary>
    /// Tests that EnsureValid does not throw an exception when the builder is valid.
    /// </summary>
    [Fact]
    public void EnsureValid_WithValidBuilder_DoesNotThrow()
    {
        // Arrange
        var mockEventBus = new Mock<IEventBus>();
        var builder = mockEventBus.Object.CreatePredicateSubscription<TestEvent>();
        builder.WithHandler(async (e, ct) => await Task.CompletedTask);

        // Act
        Action act = () => builder.EnsureValid<TestEvent>();

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that EnsureValid throws an ArgumentException when no handler is configured.
    /// </summary>
    [Fact]
    public void EnsureValid_WithHandlerNotConfigured_ThrowsArgumentException()
    {
        // Arrange
        var mockEventBus = new Mock<IEventBus>();
        var builder = mockEventBus.Object.CreatePredicateSubscription<TestEvent>();

        // Act
        Action act = () => builder.EnsureValid<TestEvent>();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*No handler configured. Call WithHandler before calling Register.*");
    }

    /// <summary>
    /// Tests that EnsureValid throws an ArgumentException when the handler name is empty.
    /// </summary>
    [Fact]
    public void EnsureValid_WithEmptyHandlerName_ThrowsArgumentException()
    {
        // Arrange
        var mockEventBus = new Mock<IEventBus>();
        var builder = mockEventBus.Object.CreatePredicateSubscription<TestEvent>();
        builder.WithHandler(async (e, ct) => await Task.CompletedTask);
        builder.WithHandlerName("   ");

        // Act
        Action act = () => builder.EnsureValid<TestEvent>();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Handler name cannot be empty or whitespace*");
    }

    /// <summary>
    /// Tests that EnsureValid throws an ArgumentException when the priority is out of the valid range.
    /// </summary>
    [Fact]
    public void EnsureValid_WithPriorityOutOfRange_ThrowsArgumentException()
    {
        // Arrange
        var mockEventBus = new Mock<IEventBus>();
        var builder = mockEventBus.Object.CreatePredicateSubscription<TestEvent>();
        builder.WithHandler(async (e, ct) => await Task.CompletedTask);
        builder.WithPriority(1001);

        // Act
        Action act = () => builder.EnsureValid<TestEvent>();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Priority must be between -1000 and 1000.*");
    }

    /// <summary>
    /// Tests that EnsureValid throws an ArgumentException with all errors when there are multiple configuration issues.
    /// </summary>
    [Fact]
    public void EnsureValid_WithMultipleErrors_ThrowsArgumentExceptionWithAllErrors()
    {
        // Arrange
        var mockEventBus = new Mock<IEventBus>();
        var builder = mockEventBus.Object.CreatePredicateSubscription<TestEvent>();
        builder.WithHandlerName("   ");
        builder.WithPriority(-1001);

        // Act
        Action act = () => builder.EnsureValid<TestEvent>();

        // Assert
        var exception = Assert.Throws<ArgumentException>(act);
        exception.Message.Should().Contain("Handler name cannot be empty or whitespace");
        exception.Message.Should().Contain("Priority must be between -1000 and 1000");
    }
}