using DotnetEventBus.Integration;
using FluentAssertions;
using Xunit;

namespace DotnetEventBus.Tests;

/// <summary>
/// Unit tests for CircuitBreakerValidation class.
/// </summary>
public class CircuitBreakerValidationTests
{
    [Fact]
    public void Validate_WithValidCircuitBreaker_ReturnsEmptyList()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(failureThreshold: 5, timeout: TimeSpan.FromSeconds(30));

        // Act
        var errors = circuitBreaker.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithNullCircuitBreaker_ThrowsArgumentNullException()
    {
        // Arrange
        CircuitBreaker? circuitBreaker = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => circuitBreaker!.Validate());
    }

    [Fact]
    public void Validate_WithDefaultConstructor_ReturnsEmptyList()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker();

        // Act
        var errors = circuitBreaker.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithCustomThresholdAndTimeout_ReturnsEmptyList()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(failureThreshold: 10, timeout: TimeSpan.FromMinutes(2));

        // Act
        var errors = circuitBreaker.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void IsValid_WithValidCircuitBreaker_ReturnsTrue()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(failureThreshold: 5, timeout: TimeSpan.FromSeconds(30));

        // Act
        var isValid = circuitBreaker.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithNullCircuitBreaker_ReturnsFalse()
    {
        // Arrange
        CircuitBreaker? circuitBreaker = null;

        // Act
        var isValid = circuitBreaker.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithDefaultConstructor_ReturnsTrue()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker();

        // Act
        var isValid = circuitBreaker.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void EnsureValid_WithValidCircuitBreaker_DoesNotThrow()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(failureThreshold: 5, timeout: TimeSpan.FromSeconds(30));

        // Act
        Action act = () => circuitBreaker.EnsureValid();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureValid_WithNullCircuitBreaker_ThrowsArgumentNullException()
    {
        // Arrange
        CircuitBreaker? circuitBreaker = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => circuitBreaker!.EnsureValid());
    }

    [Fact]
    public void Validate_ReturnsReadOnlyList()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(failureThreshold: 5, timeout: TimeSpan.FromSeconds(30));

        // Act
        var errors = circuitBreaker.Validate();

        // Assert
        errors.Should().BeAssignableTo<IReadOnlyList<string>>();
    }

    [Fact]
    public void Validate_WithMultipleCircuitBreakers_ReturnsIndependentLists()
    {
        // Arrange
        var breaker1 = new CircuitBreaker(failureThreshold: 5, timeout: TimeSpan.FromSeconds(30));
        var breaker2 = new CircuitBreaker(failureThreshold: 10, timeout: TimeSpan.FromMinutes(1));

        // Act
        var errors1 = breaker1.Validate();
        var errors2 = breaker2.Validate();

        // Assert
        errors1.Should().BeEmpty();
        errors2.Should().BeEmpty();
        errors1.Should().NotBeSameAs(errors2);
    }

    [Fact]
    public void IsValid_AfterReset_ReturnsTrue()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(failureThreshold: 5, timeout: TimeSpan.FromSeconds(30));

        // Reset the circuit breaker
        circuitBreaker.Reset();

        // Act
        var isValid = circuitBreaker.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Constructor_WithNonPositiveFailureThreshold_ThrowsExactArgumentExceptionNamingFailureThreshold(int failureThreshold)
    {
        // Arrange
        Action act = () => _ = new CircuitBreaker(failureThreshold: failureThreshold);

        // Act
        var exception = act.Should().Throw<ArgumentException>().Which;

        // Assert
        exception.GetType().Should().Be(typeof(ArgumentException));
        exception.ParamName.Should().Be("failureThreshold");
        exception.Message.Should().Contain("Failure threshold must be positive");
    }

    [Fact]
    public void Constructor_WithMinimumFailureThreshold_IsAcceptedAndValid()
    {
        // Arrange & Act
        var circuitBreaker = new CircuitBreaker(failureThreshold: 1, timeout: TimeSpan.FromSeconds(30));

        // Assert
        circuitBreaker.IsValid().Should().BeTrue();
        circuitBreaker.Validate().Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithMinimumFailureThreshold_OpensAfterSingleFailure()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(failureThreshold: 1, timeout: TimeSpan.FromMinutes(1));
        Func<Task<int>> failing = () => Task.FromException<int>(new TimeoutException("Failure"));

        // Act
        var act = () => circuitBreaker.ExecuteAsync(failing);

        // Assert
        await act.Should().ThrowAsync<TimeoutException>();
        circuitBreaker.State.Should().Be(CircuitBreakerState.Open);
    }

    [Fact]
    public async Task ExecuteAsync_WithThresholdJustAboveMinimum_RemainsClosedAfterSingleFailure()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(failureThreshold: 2, timeout: TimeSpan.FromMinutes(1));
        Func<Task<int>> failing = () => Task.FromException<int>(new TimeoutException("Failure"));

        // Act
        var act = () => circuitBreaker.ExecuteAsync(failing);

        // Assert
        await act.Should().ThrowAsync<TimeoutException>();
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }
}
