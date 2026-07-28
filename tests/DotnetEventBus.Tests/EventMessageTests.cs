using Xunit;
using DotnetEventBus.Models;
using System;

namespace DotnetEventBus.Tests;

public class EventMessageTests
{
    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        // Arrange
        var eventType = "TestEvent";
        var payload = "TestPayload";

        // Act
        var message = new EventMessage(eventType, payload);

        // Assert
        Assert.NotNull(message.MessageId);
        Assert.NotEmpty(message.MessageId);
        Assert.Equal(eventType, message.EventType);
        Assert.Equal(payload, message.Payload);
        Assert.True((DateTime.UtcNow - message.CreatedAtUtc).TotalSeconds < 1);
        Assert.NotNull(message.Headers);
        Assert.Empty(message.Headers);
        Assert.Equal(MessageScope.InProcess, message.Scope);
        Assert.Equal(0, message.ProcessingAttempts);
        Assert.Null(message.CorrelationId);
        Assert.Null(message.Source);
    }

    [Fact]
    public void Constructor_ThrowsArgumentException_WhenEventTypeIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new EventMessage("", "payload"));
    }

    [Fact]
    public void Constructor_ThrowsArgumentException_WhenPayloadIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new EventMessage("type", ""));
    }

    [Fact]
    public void Validate_DoesNotThrow_WhenMessageIsValid()
    {
        var message = new EventMessage("type", "payload");
        
        var exception = Record.Exception(() => message.Validate());

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_ThrowsArgumentException_WhenPayloadIsCleared()
    {
        var message = new EventMessage("type", "payload");
        message.Payload = "";

        Assert.Throws<ArgumentException>(() => message.Validate());
    }

    [Fact]
    public void CreateRetry_CreatesNewMessageWithIncrementedAttempts()
    {
        // Arrange
        var message = new EventMessage("type", "payload");
        message.CorrelationId = "corr-123";
        message.Source = "TestSource";
        message.ProcessingAttempts = 1;
        message.AddHeader("key1", "value1");

        // Act
        var retry = message.CreateRetry();

        // Assert
        Assert.NotEqual(message.MessageId, retry.MessageId);
        Assert.Equal(message.EventType, retry.EventType);
        Assert.Equal(message.Payload, retry.Payload);
        Assert.Equal(2, retry.ProcessingAttempts);
        Assert.Equal("corr-123", retry.CorrelationId);
        Assert.Equal("TestSource", retry.Source);
        Assert.Equal("value1", retry.GetHeader("key1"));
    }

    [Fact]
    public void AddHeader_ThrowsArgumentException_WhenKeyIsEmpty()
    {
        var message = new EventMessage("type", "payload");

        Assert.Throws<ArgumentException>(() => message.AddHeader("", "value"));
    }

    [Fact]
    public void GetHeader_ReturnsNull_WhenKeyDoesNotExist()
    {
        var message = new EventMessage("type", "payload");

        var result = message.GetHeader("nonexistent");

        Assert.Null(result);
    }
}
