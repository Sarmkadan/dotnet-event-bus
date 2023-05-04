#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using Moq;
using Xunit;
using DotnetEventBus.Configuration;
using DotnetEventBus.Formatters;
using DotnetEventBus.Models;
using DotnetEventBus.Repositories;
using DotnetEventBus.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetEventBus.Tests;

public sealed class EventBusMockTests
{
    [Fact]
    public async Task PublishAsync_WithFailingHandler_ShouldAddEntryToDeadLetterRepository()
    {
        // Arrange
        var mockDeadLetterRepo = new Mock<IDeadLetterRepository>();
        mockDeadLetterRepo
            .Setup(r => r.AddAsync(It.IsAny<DeadLetterEntry>(), It.IsAny<CancellationToken>()))
            .Returns<DeadLetterEntry, CancellationToken>((entry, _) => Task.FromResult(entry));

        var options = new EventBusOptions
        {
            MaxRetryAttempts = 0,
            EnableDeadLetterQueue = true,
            AllowParallelHandling = false
        };

        var eventBus = new EventBus(
            new InMemoryEventMessageRepository(),
            new InMemorySubscriptionRepository(),
            mockDeadLetterRepo.Object,
            new DeadLetterService(mockDeadLetterRepo.Object),
            new JsonEventFormatter(),
            new ServiceCollection().BuildServiceProvider(),
            options);

        eventBus.Subscribe<TestEvent>(
            async (e, ct) =>
            {
                await Task.CompletedTask;
                throw new InvalidOperationException("Handler always fails");
            },
            handlerName: "FailingHandler");

        // Act
        await eventBus.PublishAsync(new TestEvent { Data = "fail-test", Value = 0 });

        // Assert
        mockDeadLetterRepo.Verify(
            r => r.AddAsync(It.IsAny<DeadLetterEntry>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithDeadLetterQueueDisabled_FailingHandlerShouldNotCallDeadLetterRepository()
    {
        // Arrange
        var mockDeadLetterRepo = new Mock<IDeadLetterRepository>();

        var options = new EventBusOptions
        {
            MaxRetryAttempts = 0,
            EnableDeadLetterQueue = false,
            AllowParallelHandling = false
        };

        var eventBus = new EventBus(
            new InMemoryEventMessageRepository(),
            new InMemorySubscriptionRepository(),
            mockDeadLetterRepo.Object,
            new DeadLetterService(mockDeadLetterRepo.Object),
            new JsonEventFormatter(),
            new ServiceCollection().BuildServiceProvider(),
            options);

        eventBus.Subscribe<TestEvent>(
            async (e, ct) =>
            {
                await Task.CompletedTask;
                throw new InvalidOperationException("Handler fails");
            },
            handlerName: "FailingHandler");

        // Act
        await eventBus.PublishAsync(new TestEvent { Data = "no-dlq", Value = 1 });

        // Assert
        mockDeadLetterRepo.Verify(
            r => r.AddAsync(It.IsAny<DeadLetterEntry>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void EventBusOptions_Validate_WithDistributedModeAndNoTransportType_ShouldThrow()
    {
        // Arrange
        var options = new EventBusOptions
        {
            IsDistributed = true,
            DistributedTransportType = null
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*DistributedTransportType*");
    }
}
