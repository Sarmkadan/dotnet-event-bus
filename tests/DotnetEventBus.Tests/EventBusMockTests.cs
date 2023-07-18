#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using Moq;
using Xunit;
using DotnetEventBus.Configuration;
using DotnetEventBus.Exceptions;
using DotnetEventBus.Formatters;
using DotnetEventBus.Models;
using DotnetEventBus.Repositories;
using DotnetEventBus.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetEventBus.Tests;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="EventBus"/> class using mock objects.
/// </summary>
/// <remarks>
/// These tests validate that the event bus correctly handles failed event processing scenarios,
/// including dead letter queue functionality and configuration validation.
/// </remarks>
public sealed class EventBusMockTests
{
    /// <summary>
    /// Tests that when an event handler throws an exception, the event bus adds an entry to the dead letter queue.
    /// </summary>
    /// <remarks>
    /// This test verifies the dead letter queue functionality by creating an event bus with dead letter queue enabled,
    /// subscribing a handler that always throws an exception, publishing an event, and asserting that the
    /// dead letter repository's AddAsync method was called exactly once.
    /// </remarks>
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

    /// <summary>
    /// Tests that when dead letter queue is disabled, failed event handlers do not add entries to the dead letter repository.
    /// </summary>
    /// <remarks>
    /// This test verifies that the dead letter queue can be disabled and that failed handlers do not attempt to
    /// write to the dead letter repository when it is disabled. The test creates an event bus with dead letter queue disabled,
    /// subscribes a failing handler, publishes an event, and asserts that the dead letter repository's AddAsync method
    /// was never called.
    /// </remarks>
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

    /// <summary>
    /// Tests that EventBusOptions validation throws an exception when distributed mode is enabled but no transport type is specified.
    /// </summary>
    /// <remarks>
    /// This test verifies the configuration validation logic in EventBusOptions. When IsDistributed is set to true,
    /// the DistributedTransportType property must be set to a valid value. This test creates an EventBusOptions instance
    /// with IsDistributed=true and DistributedTransportType=null, then asserts that calling Validate() throws a
    /// ValidationException with a message containing "DistributedTransportType".
    /// </remarks>
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
        act.Should().Throw<ValidationException>()
            .WithMessage("*DistributedTransportType*");
    }
}
