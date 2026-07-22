using DotnetEventBus.Handlers;
using DotnetEventBus.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace DotnetEventBus.Tests;

public class PredicateSubscriptionBuilderValidationTests
{
    private class TestEvent
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    [Fact]
    public void Validate_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        PredicateSubscriptionBuilder<TestEvent>? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.Validate<TestEvent>());
    }

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

    [Fact]
    public void IsValid_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        PredicateSubscriptionBuilder<TestEvent>? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.IsValid<TestEvent>());
    }

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

    [Fact]
    public void EnsureValid_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        PredicateSubscriptionBuilder<TestEvent>? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.EnsureValid<TestEvent>());
    }

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