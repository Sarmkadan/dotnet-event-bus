using DotnetEventBus;
using DotnetEventBus.Configuration;
using DotnetEventBus.Repositories;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DotnetEventBus.Tests;

public class EventBusBuilderTests
{
    [Fact]
    public void Constructor_WithNullServiceCollection_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EventBusBuilder(null!));
    }

    [Fact]
    public void WithOptions_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithOptions(null!));
    }

    [Fact]
    public void WithOptions_WithValidAction_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act
        builder.WithOptions(o =>
        {
            o.MaxRetryAttempts = 5;
            o.EnableDeadLetterQueue = true;
            o.DefaultHandlerTimeout = TimeSpan.FromSeconds(30);
        });

        // Assert
        var errors = builder.Validate();
        errors.Should().BeEmpty();

        // Verify the options were applied by building the service collection
        var serviceProvider = builder.Build().BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<EventBusOptions>();
        options.MaxRetryAttempts.Should().Be(5);
        options.EnableDeadLetterQueue.Should().BeTrue();
        options.DefaultHandlerTimeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void WithMessageRepository_WithNullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithMessageRepository(null!));
    }

    [Fact]
    public void WithMessageRepository_WithValidRepository_SetsRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        var mockRepository = new Mock<IEventMessageRepository>();

        // Act
        var result = builder.WithMessageRepository(mockRepository.Object);

        // Assert
        result.Should().BeSameAs(builder);
        // The repository is stored internally, we can't directly verify it,
        // but we can verify it was used by building the service collection
        var serviceProvider = builder.Build().BuildServiceProvider();
        serviceProvider.GetService<IEventMessageRepository>().Should().Be(mockRepository.Object);
    }

    [Fact]
    public void WithSubscriptionRepository_WithNullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithSubscriptionRepository(null!));
    }

    [Fact]
    public void WithSubscriptionRepository_WithValidRepository_SetsRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        var mockRepository = new Mock<ISubscriptionRepository>();

        // Act
        var result = builder.WithSubscriptionRepository(mockRepository.Object);

        // Assert
        result.Should().BeSameAs(builder);
        var serviceProvider = builder.Build().BuildServiceProvider();
        serviceProvider.GetService<ISubscriptionRepository>().Should().Be(mockRepository.Object);
    }

    [Fact]
    public void WithDeadLetterRepository_WithNullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithDeadLetterRepository(null!));
    }

    [Fact]
    public void WithDeadLetterRepository_WithValidRepository_SetsRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        var mockRepository = new Mock<IDeadLetterRepository>();

        // Act
        var result = builder.WithDeadLetterRepository(mockRepository.Object);

        // Assert
        result.Should().BeSameAs(builder);
        var serviceProvider = builder.Build().BuildServiceProvider();
        serviceProvider.GetService<IDeadLetterRepository>().Should().Be(mockRepository.Object);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void WithMaxRetries_WithNegativeValue_ThrowsArgumentException(int negativeValue)
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => builder.WithMaxRetries(negativeValue));
        exception.Message.Should().Contain("Max retry attempts cannot be negative");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public void WithMaxRetries_WithValidValue_SetsMaxRetryAttempts(int maxAttempts)
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act
        var result = builder.WithMaxRetries(maxAttempts);

        // Assert
        result.Should().BeSameAs(builder);
        var errors = builder.Validate();
        errors.Should().BeEmpty();

        var serviceProvider = builder.Build().BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<EventBusOptions>();
        options.MaxRetryAttempts.Should().Be(maxAttempts);
    }

    [Fact]
    public void WithHandlerTimeout_WithZeroTimeout_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => builder.WithHandlerTimeout(TimeSpan.Zero));
        exception.Message.Should().Contain("Timeout must be greater than zero");
    }

    [Fact]
    public void WithHandlerTimeout_WithNegativeTimeout_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => builder.WithHandlerTimeout(TimeSpan.FromSeconds(-1)));
        exception.Message.Should().Contain("Timeout must be greater than zero");
    }

    [Fact]
    public void WithHandlerTimeout_WithValidTimeout_SetsDefaultHandlerTimeout()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        var timeout = TimeSpan.FromSeconds(45);

        // Act
        var result = builder.WithHandlerTimeout(timeout);

        // Assert
        result.Should().BeSameAs(builder);
        var errors = builder.Validate();
        errors.Should().BeEmpty();

        var serviceProvider = builder.Build().BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<EventBusOptions>();
        options.DefaultHandlerTimeout.Should().Be(timeout);
    }

    [Fact]
    public void WithParallelHandling_WithTrue_EnablesParallelHandling()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act
        var result = builder.WithParallelHandling(true);

        // Assert
        result.Should().BeSameAs(builder);
        var errors = builder.Validate();
        errors.Should().BeEmpty();

        var serviceProvider = builder.Build().BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<EventBusOptions>();
        options.AllowParallelHandling.Should().BeTrue();
    }

    [Fact]
    public void WithParallelHandling_WithFalse_DisablesParallelHandling()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        builder.WithParallelHandling(true); // Set to true first

        // Act
        var result = builder.WithParallelHandling(false);

        // Assert
        result.Should().BeSameAs(builder);
        var errors = builder.Validate();
        errors.Should().BeEmpty();

        var serviceProvider = builder.Build().BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<EventBusOptions>();
        options.AllowParallelHandling.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void WithMaxConcurrentHandlers_WithInvalidValue_ThrowsArgumentException(int invalidValue)
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => builder.WithMaxConcurrentHandlers(invalidValue));
        exception.Message.Should().Contain("Max concurrent handlers must be at least 1");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public void WithMaxConcurrentHandlers_WithValidValue_SetsMaxConcurrentHandlers(int maxConcurrent)
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act
        var result = builder.WithMaxConcurrentHandlers(maxConcurrent);

        // Assert
        result.Should().BeSameAs(builder);
        var errors = builder.Validate();
        errors.Should().BeEmpty();

        var serviceProvider = builder.Build().BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<EventBusOptions>();
        options.MaxConcurrentHandlers.Should().Be(maxConcurrent);
    }

    [Fact]
    public void WithDeadLetterQueue_WithTrue_EnablesDeadLetterQueue()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act
        var result = builder.WithDeadLetterQueue(true);

        // Assert
        result.Should().BeSameAs(builder);
        var errors = builder.Validate();
        errors.Should().BeEmpty();

        var serviceProvider = builder.Build().BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<EventBusOptions>();
        options.EnableDeadLetterQueue.Should().BeTrue();
    }

    [Fact]
    public void WithDeadLetterQueue_WithFalse_DisablesDeadLetterQueue()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        builder.WithDeadLetterQueue(true); // Set to true first

        // Act
        var result = builder.WithDeadLetterQueue(false);

        // Assert
        result.Should().BeSameAs(builder);
        var errors = builder.Validate();
        errors.Should().BeEmpty();

        var serviceProvider = builder.Build().BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<EventBusOptions>();
        options.EnableDeadLetterQueue.Should().BeFalse();
    }

    [Fact]
    public void WithThrowOnHandlerFailure_WithTrue_EnablesThrowOnFailure()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act
        var result = builder.WithThrowOnHandlerFailure(true);

        // Assert
        result.Should().BeSameAs(builder);
        var errors = builder.Validate();
        errors.Should().BeEmpty();

        var serviceProvider = builder.Build().BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<EventBusOptions>();
        options.ThrowOnHandlerFailure.Should().BeTrue();
    }

    [Fact]
    public void WithThrowOnHandlerFailure_WithFalse_DisablesThrowOnFailure()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        builder.WithThrowOnHandlerFailure(true); // Set to true first

        // Act
        var result = builder.WithThrowOnHandlerFailure(false);

        // Assert
        result.Should().BeSameAs(builder);
        var errors = builder.Validate();
        errors.Should().BeEmpty();

        var serviceProvider = builder.Build().BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<EventBusOptions>();
        options.ThrowOnHandlerFailure.Should().BeFalse();
    }

    [Fact]
    public void AsDistributed_WithNullTransportType_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => builder.AsDistributed(null!));
        exception.Message.Should().Contain("Transport type cannot be empty");
    }

    [Fact]
    public void AsDistributed_WithEmptyTransportType_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => builder.AsDistributed(""));
        exception.Message.Should().Contain("Transport type cannot be empty");
    }

    [Fact]
    public void AsDistributed_WithWhitespaceTransportType_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => builder.AsDistributed("   "));
        exception.Message.Should().Contain("Transport type cannot be empty");
    }

    [Fact]
    public void AsDistributed_WithValidTransportType_SetsDistributedConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        var transportType = "RabbitMQ";
        var connectionString = "host=localhost;port=5672";

        // Act
        var result = builder.AsDistributed(transportType, connectionString);

        // Assert
        result.Should().BeSameAs(builder);
        var errors = builder.Validate();
        errors.Should().BeEmpty();

        var serviceProvider = builder.Build().BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<EventBusOptions>();
        options.IsDistributed.Should().BeTrue();
        options.DistributedTransportType.Should().Be(transportType);
        options.DistributedTransportConnectionString.Should().Be(connectionString);
    }

    [Fact]
    public void AsDistributed_WithValidTransportTypeWithoutConnectionString_SetsDistributedConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        var transportType = "AzureServiceBus";

        // Act
        var result = builder.AsDistributed(transportType);

        // Assert
        result.Should().BeSameAs(builder);
        var errors = builder.Validate();
        errors.Should().BeEmpty();

        var serviceProvider = builder.Build().BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<EventBusOptions>();
        options.IsDistributed.Should().BeTrue();
        options.DistributedTransportType.Should().Be(transportType);
        options.DistributedTransportConnectionString.Should().BeNull();
    }

    [Fact]
    public void Build_WithNoRepositories_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        builder.WithMaxRetries(3)
              .WithHandlerTimeout(TimeSpan.FromSeconds(30))
              .WithParallelHandling(false)
              .WithMaxConcurrentHandlers(10)
              .WithDeadLetterQueue(true)
              .WithThrowOnHandlerFailure(true);

        // Act
        var result = builder.Build();

        // Assert
        result.Should().BeSameAs(services);

        // Verify the service collection can be built
        var serviceProvider = result.BuildServiceProvider();
        serviceProvider.GetService<EventBusOptions>().Should().NotBeNull();
    }

    [Fact]
    public void Build_WithCustomRepositories_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();
        var mockMessageRepo = new Mock<IEventMessageRepository>();
        var mockSubscriptionRepo = new Mock<ISubscriptionRepository>();
        var mockDeadLetterRepo = new Mock<IDeadLetterRepository>();

        builder.WithMessageRepository(mockMessageRepo.Object)
              .WithSubscriptionRepository(mockSubscriptionRepo.Object)
              .WithDeadLetterRepository(mockDeadLetterRepo.Object);

        // Act
        var result = builder.Build();

        // Assert
        result.Should().BeSameAs(services);

        // Verify the service collection can be built
        var serviceProvider = result.BuildServiceProvider();
        serviceProvider.GetService<IEventMessageRepository>().Should().Be(mockMessageRepo.Object);
        serviceProvider.GetService<ISubscriptionRepository>().Should().Be(mockSubscriptionRepo.Object);
        serviceProvider.GetService<IDeadLetterRepository>().Should().Be(mockDeadLetterRepo.Object);
    }

    [Fact]
    public void FluentInterface_AllMethodsReturnBuilder_EnablesMethodChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddEventBusBuilder();

        // Act & Assert - all methods should return the builder for fluent chaining
        var result = builder
            .WithMaxRetries(5)
            .WithHandlerTimeout(TimeSpan.FromSeconds(30))
            .WithParallelHandling(true)
            .WithMaxConcurrentHandlers(5)
            .WithDeadLetterQueue(true)
            .WithThrowOnHandlerFailure(false)
            .AsDistributed("RabbitMQ", "host=localhost")
            .WithOptions(o => o.RequestTimeout = TimeSpan.FromSeconds(10));

        // Assert
        result.Should().BeSameAs(builder);
        var errors = builder.Validate();
        errors.Should().BeEmpty();
    }
}
