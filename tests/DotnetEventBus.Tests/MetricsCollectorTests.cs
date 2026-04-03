#nullable enable

using FluentAssertions;
using Xunit;
using DotnetEventBus.Advanced;

namespace DotnetEventBus.Tests;

public sealed class MetricsCollectorTests
{
    [Fact]
    public void RecordEventPublished_ShouldIncrementPublishCount()
    {
        // Arrange
        var collector = new MetricsCollector();

        // Act
        collector.RecordEventPublished("OrderPlaced", 100);
        collector.RecordEventPublished("OrderPlaced", 150);
        collector.RecordEventPublished("OrderShipped", 200);

        // Assert
        var eventMetrics = collector.GetEventMetrics("OrderPlaced");
        eventMetrics.Should().NotBeNull();
        eventMetrics!.PublishCount.Should().Be(2);
    }

    [Fact]
    public void RecordEventPublished_ShouldCalculateAverageDuration()
    {
        // Arrange
        var collector = new MetricsCollector();

        // Act
        collector.RecordEventPublished("TestEvent", 100);
        collector.RecordEventPublished("TestEvent", 200);
        collector.RecordEventPublished("TestEvent", 300);

        // Assert
        var metrics = collector.GetEventMetrics("TestEvent");
        metrics!.AverageDurationMs.Should().Be(200.0);
    }

    [Fact]
    public void RecordEventPublished_ShouldTrackMinMaxDuration()
    {
        // Arrange
        var collector = new MetricsCollector();

        // Act
        collector.RecordEventPublished("TestEvent", 50);
        collector.RecordEventPublished("TestEvent", 200);
        collector.RecordEventPublished("TestEvent", 75);

        // Assert
        var metrics = collector.GetEventMetrics("TestEvent");
        metrics!.MinDurationMs.Should().Be(50);
        metrics.MaxDurationMs.Should().Be(200);
    }

    [Fact]
    public void RecordEventFailed_ShouldIncrementFailureCount()
    {
        // Arrange
        var collector = new MetricsCollector();
        var exception = new InvalidOperationException("Test failure");

        // Act
        collector.RecordEventFailed("OrderPlaced", "OrderHandler", exception);
        collector.RecordEventFailed("OrderPlaced", "AnotherHandler", exception);

        // Assert
        var metrics = collector.GetEventMetrics("OrderPlaced");
        metrics!.FailureCount.Should().Be(2);
    }

    [Fact]
    public void RecordEventFailed_ShouldCaptureErrorMessage()
    {
        // Arrange
        var collector = new MetricsCollector();
        var exception = new InvalidOperationException("Database connection failed");

        // Act
        collector.RecordEventFailed("TestEvent", "Handler1", exception);

        // Assert
        var metrics = collector.GetEventMetrics("TestEvent");
        metrics!.LastError.Should().Contain("Database connection failed");
    }

    [Fact]
    public void RecordHandlerExecution_ShouldTrackHandlerMetrics()
    {
        // Arrange
        var collector = new MetricsCollector();

        // Act
        collector.RecordHandlerExecution("MyHandler", "OrderPlaced", 100, success: true);
        collector.RecordHandlerExecution("MyHandler", "OrderPlaced", 150, success: true);
        collector.RecordHandlerExecution("MyHandler", "OrderPlaced", 200, success: false);

        // Assert
        var handlerMetrics = collector.GetHandlerMetrics("MyHandler", "OrderPlaced");
        handlerMetrics.Should().NotBeNull();
        handlerMetrics!.ExecutionCount.Should().Be(3);
        handlerMetrics.SuccessCount.Should().Be(2);
        handlerMetrics.FailureCount.Should().Be(1);
    }

    [Fact]
    public void GetAllEventMetrics_ShouldReturnAllTrackedEvents()
    {
        // Arrange
        var collector = new MetricsCollector();

        // Act
        collector.RecordEventPublished("Event1", 100);
        collector.RecordEventPublished("Event2", 200);
        collector.RecordEventPublished("Event3", 300);

        // Assert
        var allMetrics = collector.GetAllEventMetrics();
        allMetrics.Should().HaveCount(3);
        allMetrics.Select(m => m.EventType).Should().Contain(new[] { "Event1", "Event2", "Event3" });
    }

    [Fact]
    public void GetAllHandlerMetrics_ShouldReturnAllTrackedHandlers()
    {
        // Arrange
        var collector = new MetricsCollector();

        // Act
        collector.RecordHandlerExecution("Handler1", "Event1", 100, true);
        collector.RecordHandlerExecution("Handler2", "Event2", 150, true);
        collector.RecordHandlerExecution("Handler1", "Event2", 200, true);

        // Assert
        var allMetrics = collector.GetAllHandlerMetrics();
        allMetrics.Should().HaveCount(3);
    }

    [Fact]
    public void GetSuccessRate_ShouldCalculatePercentageCorrectly()
    {
        // Arrange
        var collector = new MetricsCollector();

        // Act
        collector.RecordHandlerExecution("Handler1", "Event1", 100, success: true);
        collector.RecordHandlerExecution("Handler1", "Event1", 100, success: true);
        collector.RecordHandlerExecution("Handler1", "Event1", 100, success: false);

        // Assert
        var successRate = collector.GetSuccessRate("Handler1", "Event1");
        successRate.Should().Be(66.67, precision: 0.01);
    }

    [Fact]
    public void GetSuccessRate_WithNoExecutions_ShouldReturnZero()
    {
        // Arrange
        var collector = new MetricsCollector();

        // Act
        var successRate = collector.GetSuccessRate("Handler1", "NonExistentEvent");

        // Assert
        successRate.Should().Be(0);
    }

    [Fact]
    public void GetAverageDuration_ShouldCalculateAverageForHandler()
    {
        // Arrange
        var collector = new MetricsCollector();

        // Act
        collector.RecordHandlerExecution("Handler1", "Event1", 100, true);
        collector.RecordHandlerExecution("Handler1", "Event1", 200, true);
        collector.RecordHandlerExecution("Handler1", "Event1", 300, true);

        // Assert
        var averageDuration = collector.GetAverageDuration("Handler1", "Event1");
        averageDuration.Should().Be(200.0);
    }

    [Fact]
    public void Reset_ShouldClearAllMetrics()
    {
        // Arrange
        var collector = new MetricsCollector();
        collector.RecordEventPublished("Event1", 100);
        collector.RecordHandlerExecution("Handler1", "Event1", 100, true);

        // Act
        collector.Reset();

        // Assert
        collector.GetAllEventMetrics().Should().BeEmpty();
        collector.GetAllHandlerMetrics().Should().BeEmpty();
    }

    [Fact]
    public void RecordEventPublished_WithMultipleEvents_ShouldTrackIndependently()
    {
        // Arrange
        var collector = new MetricsCollector();

        // Act
        for (int i = 0; i < 5; i++)
        {
            collector.RecordEventPublished("Event1", 100 + i * 10);
            collector.RecordEventPublished("Event2", 200 + i * 20);
        }

        // Assert
        var event1Metrics = collector.GetEventMetrics("Event1");
        var event2Metrics = collector.GetEventMetrics("Event2");

        event1Metrics!.PublishCount.Should().Be(5);
        event2Metrics!.PublishCount.Should().Be(5);
        event1Metrics.AverageDurationMs.Should().NotBe(event2Metrics.AverageDurationMs);
    }

    [Fact]
    public void GetLastFailureTime_ShouldUpdateOnFailure()
    {
        // Arrange
        var collector = new MetricsCollector();
        var exception = new Exception("Test");

        // Act
        var beforeTime = DateTime.UtcNow;
        collector.RecordEventFailed("Event1", "Handler1", exception);
        var afterTime = DateTime.UtcNow;

        // Assert
        var metrics = collector.GetEventMetrics("Event1");
        metrics!.LastFailureAt.Should().BeOnOrAfter(beforeTime);
        metrics.LastFailureAt.Should().BeOnOrBefore(afterTime);
    }

    [Fact]
    public void GetLastPublishedTime_ShouldUpdateOnPublish()
    {
        // Arrange
        var collector = new MetricsCollector();

        // Act
        var beforeTime = DateTime.UtcNow;
        collector.RecordEventPublished("Event1", 100);
        var afterTime = DateTime.UtcNow;

        // Assert
        var metrics = collector.GetEventMetrics("Event1");
        metrics!.LastPublishedAt.Should().BeOnOrAfter(beforeTime);
        metrics.LastPublishedAt.Should().BeOnOrBefore(afterTime);
    }
}
