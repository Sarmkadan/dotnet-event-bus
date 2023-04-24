#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetEventBus.Exceptions;

/// <summary>
/// Base exception for all event bus related errors.
/// </summary>
public sealed class EventBusException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventBusException"/> class.
    /// </summary>
    public EventBusException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBusException"/> class with a message.
    /// </summary>
    public EventBusException(string? message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBusException"/> class with a message and inner exception.
    /// </summary>
    public EventBusException(string? message, Exception? innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Thrown when no handlers are registered for a given event type.
/// </summary>
public sealed class NoHandlersRegisteredException : EventBusException
{
    public string EventType { get; }

    public NoHandlersRegisteredException(string eventType)
        : base($"No handlers registered for event type: {eventType}")
    {
        EventType = eventType;
    }
}

/// <summary>
/// Thrown when an event handler invocation fails.
/// </summary>
public sealed class HandlerInvocationException : EventBusException
{
    public string HandlerName { get; }
    public string EventType { get; }

    public HandlerInvocationException(string handlerName, string eventType, Exception? innerException)
        : base($"Handler '{handlerName}' failed to process event '{eventType}'", innerException)
    {
        HandlerName = handlerName;
        EventType = eventType;
    }
}

/// <summary>
/// Thrown when attempting to subscribe with an invalid handler.
/// </summary>
public sealed class InvalidHandlerException : EventBusException
{
    public Type HandlerType { get; }

    public InvalidHandlerException(Type handlerType)
        : base($"Handler type '{handlerType.FullName}' does not implement a valid handler interface")
    {
        HandlerType = handlerType;
    }
}

/// <summary>
/// Thrown when message serialization or deserialization fails.
/// </summary>
public sealed class MessageSerializationException : EventBusException
{
    public string MessageType { get; }

    public MessageSerializationException(string messageType, Exception? innerException)
        : base($"Failed to serialize/deserialize message of type: {messageType}", innerException)
    {
        MessageType = messageType;
    }
}

/// <summary>
/// Thrown when attempting to access a distributed event bus without proper configuration.
/// </summary>
public sealed class DistributedBusNotConfiguredException : EventBusException
{
    public DistributedBusNotConfiguredException()
        : base("Distributed event bus is not properly configured. Ensure transport is registered.")
    {
    }
}

/// <summary>
/// Thrown when a request times out waiting for a reply.
/// </summary>
public sealed class RequestTimeoutException : EventBusException
{
    public string RequestType { get; }
    public TimeSpan Timeout { get; }

    public RequestTimeoutException(string requestType, TimeSpan timeout)
        : base($"Request of type '{requestType}' timed out after {timeout.TotalSeconds} seconds")
    {
        RequestType = requestType;
        Timeout = timeout;
    }
}
