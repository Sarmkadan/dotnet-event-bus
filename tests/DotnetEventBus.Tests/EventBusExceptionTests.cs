using DotnetEventBus.Exceptions;
using FluentAssertions;
using Xunit;

namespace DotnetEventBus.Tests;

/// <summary>
/// Unit tests for EventBusException and its derived exceptions.
/// </summary>
public class EventBusExceptionTests
{
    [Fact]
    public void EventBusException_DefaultConstructor_ShouldCreateInstance()
    {
        // Act
        var exception = new EventBusException();

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().NotBeEmpty();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void EventBusException_MessageConstructor_ShouldSetMessage()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new EventBusException(message);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void EventBusException_NullMessageConstructor_ShouldHandleNullMessage()
    {
        // Act
        var exception = new EventBusException(null);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().NotBeEmpty();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void EventBusException_MessageAndInnerExceptionConstructor_ShouldSetBoth()
    {
        // Arrange
        var message = "Test error message";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new EventBusException(message, innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void EventBusException_NullInnerExceptionConstructor_ShouldHandleNullInnerException()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new EventBusException(message, null);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void NoHandlersRegisteredException_Constructor_ShouldSetProperties()
    {
        // Arrange
        var eventType = "TestEvent";

        // Act
        var exception = new NoHandlersRegisteredException(eventType);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be($"No handlers registered for event type: {eventType}");
        exception.EventType.Should().Be(eventType);
    }

    [Fact]
    public void NoHandlersRegisteredException_EmptyEventType_ShouldHandleEmptyString()
    {
        // Arrange
        var eventType = "";

        // Act
        var exception = new NoHandlersRegisteredException(eventType);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("No handlers registered for event type: ");
        exception.EventType.Should().BeEmpty();
    }

    [Fact]
    public void NoHandlersRegisteredException_NullEventType_ShouldHandleNull()
    {
        // Arrange
        string eventType = null!;

        // Act
        var exception = new NoHandlersRegisteredException(eventType);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("No handlers registered for event type: ");
        exception.EventType.Should().BeNull();
    }

    [Fact]
    public void HandlerInvocationException_Constructor_ShouldSetProperties()
    {
        // Arrange
        var handlerName = "TestHandler";
        var eventType = "TestEvent";
        var innerException = new InvalidOperationException("Handler failed");

        // Act
        var exception = new HandlerInvocationException(handlerName, eventType, innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be($"Handler '{handlerName}' failed to process event '{eventType}'");
        exception.HandlerName.Should().Be(handlerName);
        exception.EventType.Should().Be(eventType);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void HandlerInvocationException_NullHandlerName_ShouldHandleNull()
    {
        // Arrange
        string handlerName = null!;
        var eventType = "TestEvent";
        var innerException = new InvalidOperationException("Handler failed");

        // Act
        var exception = new HandlerInvocationException(handlerName, eventType, innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be($"Handler '' failed to process event '{eventType}'");
        exception.HandlerName.Should().BeNull();
        exception.EventType.Should().Be(eventType);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void HandlerInvocationException_EmptyHandlerName_ShouldHandleEmptyString()
    {
        // Arrange
        var handlerName = "";
        var eventType = "TestEvent";

        // Act
        var exception = new HandlerInvocationException(handlerName, eventType, null);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be($"Handler '' failed to process event '{eventType}'");
        exception.HandlerName.Should().BeEmpty();
        exception.EventType.Should().Be(eventType);
    }

    [Fact]
    public void HandlerInvocationException_NullEventType_ShouldHandleNull()
    {
        // Arrange
        var handlerName = "TestHandler";
        string eventType = null!;

        // Act
        var exception = new HandlerInvocationException(handlerName, eventType, null);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be($"Handler '{handlerName}' failed to process event ''");
        exception.HandlerName.Should().Be(handlerName);
        exception.EventType.Should().BeNull();
    }

    [Fact]
    public void InvalidHandlerException_Constructor_ShouldSetProperties()
    {
        // Arrange
        var handlerType = typeof(EventBusExceptionTests);

        // Act
        var exception = new InvalidHandlerException(handlerType);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be($"Handler type '{handlerType.FullName}' does not implement a valid handler interface");
        exception.HandlerType.Should().Be(handlerType);
    }

    [Fact]
    public void InvalidHandlerException_NullHandlerType_ShouldHandleNull()
    {
        // Arrange
        Type handlerType = null!;

        // Act
        var exception = new InvalidHandlerException(handlerType);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Handler type '' does not implement a valid handler interface");
        exception.HandlerType.Should().BeNull();
    }

    [Fact]
    public void MessageSerializationException_Constructor_ShouldSetProperties()
    {
        // Arrange
        var messageType = "TestMessage";
        var innerException = new InvalidOperationException("Serialization failed");

        // Act
        var exception = new MessageSerializationException(messageType, innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be($"Failed to serialize/deserialize message of type: {messageType}");
        exception.MessageType.Should().Be(messageType);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void MessageSerializationException_NullMessageType_ShouldHandleNull()
    {
        // Arrange
        string messageType = null!;

        // Act
        var exception = new MessageSerializationException(messageType, null);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Failed to serialize/deserialize message of type: ");
        exception.MessageType.Should().BeNull();
    }

    [Fact]
    public void MessageSerializationException_EmptyMessageType_ShouldHandleEmptyString()
    {
        // Arrange
        var messageType = "";

        // Act
        var exception = new MessageSerializationException(messageType, null);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Failed to serialize/deserialize message of type: ");
        exception.MessageType.Should().BeEmpty();
    }

    [Fact]
    public void DistributedBusNotConfiguredException_Constructor_ShouldCreateInstance()
    {
        // Act
        var exception = new DistributedBusNotConfiguredException();

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Distributed event bus is not properly configured. Ensure transport is registered.");
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void RequestTimeoutException_Constructor_ShouldSetProperties()
    {
        // Arrange
        var requestType = "TestRequest";
        var timeout = TimeSpan.FromSeconds(30);

        // Act
        var exception = new RequestTimeoutException(requestType, timeout);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be($"Request of type '{requestType}' timed out after {timeout.TotalSeconds} seconds");
        exception.RequestType.Should().Be(requestType);
        exception.Timeout.Should().Be(timeout);
    }

    [Fact]
    public void RequestTimeoutException_NullRequestType_ShouldHandleNull()
    {
        // Arrange
        string requestType = null!;
        var timeout = TimeSpan.FromSeconds(15);

        // Act
        var exception = new RequestTimeoutException(requestType, timeout);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be($"Request of type '' timed out after {timeout.TotalSeconds} seconds");
        exception.RequestType.Should().BeNull();
    }

    [Fact]
    public void RequestTimeoutException_ZeroTimeout_ShouldHandleZeroTimeout()
    {
        // Arrange
        var requestType = "TestRequest";
        var timeout = TimeSpan.Zero;

        // Act
        var exception = new RequestTimeoutException(requestType, timeout);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be($"Request of type '{requestType}' timed out after {timeout.TotalSeconds} seconds");
        exception.RequestType.Should().Be(requestType);
        exception.Timeout.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void RequestTimeoutException_VeryLargeTimeout_ShouldHandleLargeTimeout()
    {
        // Arrange
        var requestType = "TestRequest";
        var timeout = TimeSpan.FromHours(24);

        // Act
        var exception = new RequestTimeoutException(requestType, timeout);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be($"Request of type '{requestType}' timed out after {timeout.TotalSeconds} seconds");
        exception.RequestType.Should().Be(requestType);
        exception.Timeout.Should().Be(TimeSpan.FromHours(24));
    }

    [Fact]
    public void EventBusException_IsBaseExceptionType_ShouldBeAssignableToException()
    {
        // Arrange
        var exception = new EventBusException("Test");

        // Act & Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void DerivedExceptions_AreEventBusExceptions_ShouldBeAssignableToEventBusException()
    {
        // Arrange & Act
        var noHandlers = new NoHandlersRegisteredException("Test");
        var handlerInvocation = new HandlerInvocationException("Handler", "Event", null);
        var invalidHandler = new InvalidHandlerException(typeof(EventBusExceptionTests));
        var messageSerialization = new MessageSerializationException("Message", null);
        var distributedNotConfigured = new DistributedBusNotConfiguredException();
        var requestTimeout = new RequestTimeoutException("Request", TimeSpan.FromSeconds(10));

        // Assert
        noHandlers.Should().BeAssignableTo<EventBusException>();
        handlerInvocation.Should().BeAssignableTo<EventBusException>();
        invalidHandler.Should().BeAssignableTo<EventBusException>();
        messageSerialization.Should().BeAssignableTo<EventBusException>();
        distributedNotConfigured.Should().BeAssignableTo<EventBusException>();
        requestTimeout.Should().BeAssignableTo<EventBusException>();
    }
}