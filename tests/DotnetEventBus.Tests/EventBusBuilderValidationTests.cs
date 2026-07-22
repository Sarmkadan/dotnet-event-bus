using DotnetEventBus;
using DotnetEventBus.Configuration;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotnetEventBus.Tests;

public class EventBusBuilderValidationTests
{
    [Fact]
    public void Validate_WithValidBuilder_ReturnsEmptyList()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act
        var errors = builder.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        EventBusBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.Validate());
    }

    [Fact]
    public void IsValid_WithValidBuilder_ReturnsTrue()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act
        var isValid = builder.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        EventBusBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.IsValid());
    }

    [Fact]
    public void EnsureValid_WithValidBuilder_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act
        Action act = () => builder.EnsureValid();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureValid_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        EventBusBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.EnsureValid());
    }

    [Fact]
    public void EnsureValid_WithInvalidBuilder_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        builder.WithOptions(options =>
        {
            options.RequestTimeout = TimeSpan.Zero;
            options.MaxRetryAttempts = -1;
            options.RetryDelay = TimeSpan.FromMilliseconds(-100);
            options.RetryDelayMultiplier = 0.5;
            options.MaxConcurrentHandlers = 0;
            options.DefaultHandlerTimeout = TimeSpan.Zero;
            options.MaxRetryDelay = TimeSpan.Zero;
        });

        // Act & Assert
        var act = () => builder.EnsureValid();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*EventBusBuilder is invalid*");
    }

    [Fact]
    public void Validate_WithInvalidRequestTimeout_ReturnsError()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        builder.WithOptions(options => options.RequestTimeout = TimeSpan.Zero);

        // Act
        var errors = builder.Validate();

        // Assert
        errors.Should().HaveCount(1);
        errors[0].Should().Contain("RequestTimeout must be greater than zero");
    }

    [Fact]
    public void Validate_WithNegativeMaxRetryAttempts_ReturnsError()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        builder.WithOptions(options => options.MaxRetryAttempts = -1);

        // Act
        var errors = builder.Validate();

        // Assert
        errors.Should().HaveCount(1);
        errors[0].Should().Contain("MaxRetryAttempts cannot be negative");
    }

    [Fact]
    public void Validate_WithNegativeRetryDelay_ReturnsError()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        builder.WithOptions(options => options.RetryDelay = TimeSpan.FromMilliseconds(-100));

        // Act
        var errors = builder.Validate();

        // Assert
        errors.Should().HaveCount(1);
        errors[0].Should().Contain("RetryDelay cannot be negative");
    }

    [Fact]
    public void Validate_WithRetryDelayMultiplierLessThanOne_ReturnsError()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        builder.WithOptions(options => options.RetryDelayMultiplier = 0.5);

        // Act
        var errors = builder.Validate();

        // Assert
        errors.Should().HaveCount(1);
        errors[0].Should().Contain("RetryDelayMultiplier must be at least 1.0");
    }

    [Fact]
    public void Validate_WithZeroMaxConcurrentHandlers_ReturnsError()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        builder.WithOptions(options => options.MaxConcurrentHandlers = 0);

        // Act
        var errors = builder.Validate();

        // Assert
        errors.Should().HaveCount(1);
        errors[0].Should().Contain("MaxConcurrentHandlers must be at least 1");
    }

    [Fact]
    public void Validate_WithZeroDefaultHandlerTimeout_ReturnsError()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        builder.WithOptions(options => options.DefaultHandlerTimeout = TimeSpan.Zero);

        // Act
        var errors = builder.Validate();

        // Assert
        errors.Should().HaveCount(1);
        errors[0].Should().Contain("DefaultHandlerTimeout must be greater than zero");
    }

    [Fact]
    public void Validate_WithZeroMaxRetryDelay_ReturnsError()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        builder.WithOptions(options => options.MaxRetryDelay = TimeSpan.Zero);

        // Act
        var errors = builder.Validate();

        // Assert
        errors.Should().HaveCount(1);
        errors[0].Should().Contain("MaxRetryDelay must be greater than zero");
    }

    [Fact]
    public void Validate_WithDistributedButNoTransportType_ReturnsError()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        builder.AsDistributed("RabbitMQ");
        builder.WithOptions(options => options.IsDistributed = true);
        builder.WithOptions(options => options.DistributedTransportType = null);

        // Act
        var errors = builder.Validate();

        // Assert
        errors.Should().HaveCount(1);
        errors[0].Should().Contain("DistributedTransportType must be specified when IsDistributed is true");
    }

    [Fact]
    public void Validate_WithValidDistributedConfiguration_ReturnsEmptyList()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        builder.AsDistributed("RabbitMQ", "host=localhost");

        // Act
        var errors = builder.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithMultipleInvalidOptions_ReturnsAllErrors()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        builder.WithOptions(options =>
        {
            options.RequestTimeout = TimeSpan.Zero;
            options.MaxRetryAttempts = -1;
            options.MaxConcurrentHandlers = 0;
        });

        // Act
        var errors = builder.Validate();

        // Assert
        errors.Should().HaveCount(3);
    }

    [Fact]
    public void IsValid_WithInvalidBuilder_ReturnsFalse()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        builder.WithOptions(options => options.RequestTimeout = TimeSpan.Zero);

        // Act
        var isValid = builder.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ReturnsReadOnlyList()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act
        var errors = builder.Validate();

        // Assert
        errors.Should().BeAssignableTo<IReadOnlyList<string>>();
    }

    [Fact]
    public void EnsureValid_WithInvalidBuilder_ContainsAllValidationErrorsInMessage()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        builder.WithOptions(options =>
        {
            options.RequestTimeout = TimeSpan.Zero;
            options.MaxRetryAttempts = -1;
            options.RetryDelay = TimeSpan.FromMilliseconds(-100);
        });

        // Act
        var act = () => builder.EnsureValid();

        // Assert
        var exception = Assert.Throws<ArgumentException>(act);
        exception.Message.Should().Contain("RequestTimeout must be greater than zero");
        exception.Message.Should().Contain("MaxRetryAttempts cannot be negative");
        exception.Message.Should().Contain("RetryDelay cannot be negative");
    }
}