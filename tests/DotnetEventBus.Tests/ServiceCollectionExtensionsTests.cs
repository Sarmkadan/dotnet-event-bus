using DotnetEventBus.Configuration;
using DotnetEventBus.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotnetEventBus.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEventBus_RabbitMqWithNullOptions_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddEventBus((RabbitMqOptions?)null);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RabbitMqOptions*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddEventBus_RabbitMqWithBlankConnectionString_ThrowsInvalidOperationException(string connectionString)
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new RabbitMqOptions { ConnectionString = connectionString };

        // Act
        var act = () => services.AddEventBus(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionString*");
    }

    [Fact]
    public void AddEventBus_RabbitMqWithValidOptions_ThrowsNotSupportedAndRegistersNothing()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new RabbitMqOptions { ConnectionString = "amqp://localhost:5672/" };

        // Act
        var act = () => services.AddEventBus(options);

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*RabbitMq*");
        services.Should().NotContain(d => d.ServiceType == typeof(IEventBus));
    }

    [Fact]
    public void AddEventBus_RabbitMqAfterInMemory_ThrowsInvalidOperationExceptionNamingBothBuses()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEventBus();
        var options = new RabbitMqOptions { ConnectionString = "amqp://localhost:5672/" };

        // Act
        var act = () => services.AddEventBus(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot register the RabbitMq event bus*InMemory event bus is already registered*");
    }

    [Fact]
    public void AddEventBus_InMemoryAfterInMemory_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEventBus();

        // Act
        var act = () => services.AddEventBus(options => options.MaxRetryAttempts = 1);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddInProcessTransport_ThenAddEventBus_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddInProcessTransport();

        // Act
        var act = () => services.AddEventBus();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddInProcessTransport_ThenRabbitMq_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddInProcessTransport();
        var options = new RabbitMqOptions { ConnectionString = "amqp://localhost:5672/" };

        // Act
        var act = () => services.AddEventBus(options);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetRequiredEventBus_WithoutRegistration_ThrowsInvalidOperationExceptionMentioningAddEventBus()
    {
        // Arrange
        var provider = new ServiceCollection().BuildServiceProvider();

        // Act
        var act = () => provider.GetRequiredEventBus();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AddEventBus*");
    }

    [Fact]
    public void GetRequiredService_IEventBusWithoutRegistration_ThrowsInvalidOperationException()
    {
        // Arrange
        var provider = new ServiceCollection().BuildServiceProvider();

        // Act
        var act = () => provider.GetRequiredService<IEventBus>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*IEventBus*");
    }

    [Fact]
    public void GetRequiredEventBus_WithRegistration_ReturnsEventBus()
    {
        // Arrange
        var provider = new ServiceCollection()
            .AddEventBus()
            .BuildServiceProvider();

        // Act
        var eventBus = provider.GetRequiredEventBus();

        // Assert
        eventBus.Should().NotBeNull();
        eventBus.Should().BeAssignableTo<IEventBus>();
    }

    [Fact]
    public void GetRequiredEventBus_NullProvider_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceProvider? provider = null;

        // Act
        var act = () => provider!.GetRequiredEventBus();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
