using DotnetEventBus.Workers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetEventBus.Tests;

public class DeadLetterProcessorTests
{
    private readonly Mock<ILogger<DeadLetterProcessor>> _loggerMock;
    private readonly DeadLetterProcessor _processor;

    public DeadLetterProcessorTests()
    {
        _loggerMock = new Mock<ILogger<DeadLetterProcessor>>();
        _processor = new DeadLetterProcessor(_loggerMock.Object);
    }

    [Fact]
    public void Enqueue_ShouldAddItemToQueue()
    {
        // Arrange
        var eventType = "TestEvent";
        var eventData = new { Id = 1 };
        var exception = new Exception("Test Exception");

        // Act
        _processor.Enqueue(eventType, eventData, exception);

        // Assert
        var items = _processor.GetAllItems();
        items.Should().HaveCount(1);
        var item = items.First();
        item.EventType.Should().Be(eventType);
        item.EventData.Should().Be(eventData);
        item.ErrorMessage.Should().Be(exception.Message);
        item.Status.Should().Be(DeadLetterStatus.Pending);
    }

    [Fact]
    public void GetStats_ShouldReturnCorrectCounts()
    {
        // Arrange
        _processor.Enqueue("Type1", new object(), new Exception("Err1"));
        _processor.Enqueue("Type2", new object(), new Exception("Err2"));

        // Act
        var stats = _processor.GetStats();

        // Assert
        stats.TotalItems.Should().Be(2);
        stats.PendingItems.Should().Be(2);
    }

    [Fact]
    public void RemoveItem_ExistingId_ShouldReturnTrueAndRemoveItem()
    {
        // Arrange
        var exception = new Exception("Err");
        _processor.Enqueue("Type", new object(), exception);
        var itemId = _processor.GetAllItems().First().Id;

        // Act
        var result = _processor.RemoveItem(itemId!);

        // Assert
        result.Should().BeTrue();
        _processor.GetAllItems().Should().BeEmpty();
    }

    [Fact]
    public void RemoveItem_NonExistingId_ShouldReturnFalse()
    {
        // Act
        var result = _processor.RemoveItem("non-existent-id");

        // Assert
        result.Should().BeFalse();
    }
}
