#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetEventBus.Exceptions;

/// <summary>
/// Base exception for all event bus related errors.
/// </summary>
public class EventBusException : Exception
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
    public Type? HandlerType { get; }

    public InvalidHandlerException(Type? handlerType)
    : base(handlerType == null ? "Handler type '' does not implement a valid handler interface" : $"Handler type '{handlerType.FullName}' does not implement a valid handler interface")
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
    private const string DefaultMessage =
        "Distributed event bus is not properly configured. Ensure transport is registered.";

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedBusNotConfiguredException"/> class
    /// with the default message.
    /// </summary>
    public DistributedBusNotConfiguredException()
    : base(DefaultMessage)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedBusNotConfiguredException"/> class with a message.
    /// </summary>
    /// <param name="message">A message describing the configuration problem.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
    public DistributedBusNotConfiguredException(string message)
    : base(message)
    {
        ArgumentNullException.ThrowIfNull(message);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedBusNotConfiguredException"/> class
    /// with a message and inner exception.
    /// </summary>
    /// <param name="message">A message describing the configuration problem.</param>
    /// <param name="innerException">The exception that caused the problem, or <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
    public DistributedBusNotConfiguredException(string message, Exception? innerException)
    : base(message, innerException)
    {
        ArgumentNullException.ThrowIfNull(message);
    }
}

/// <summary>
/// Thrown when a request times out waiting for a reply.
/// </summary>
public sealed class RequestTimeoutException : EventBusException
{
    /// <summary>
    /// Gets the type name of the request that timed out.
    /// </summary>
    public string RequestType { get; }

    /// <summary>
    /// Gets the timeout that was exceeded while waiting for a reply.
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestTimeoutException"/> class.
    /// </summary>
    /// <param name="requestType">The type name of the request that timed out.</param>
    /// <param name="timeout">The timeout that was exceeded.</param>
    public RequestTimeoutException(string requestType, TimeSpan timeout)
    : this(requestType, timeout, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestTimeoutException"/> class
    /// with an inner exception.
    /// </summary>
    /// <param name="requestType">The type name of the request that timed out.</param>
    /// <param name="timeout">The timeout that was exceeded.</param>
    /// <param name="innerException">The exception that caused the timeout, or <see langword="null"/>.</param>
    public RequestTimeoutException(string requestType, TimeSpan timeout, Exception? innerException)
    : base($"Request of type '{requestType}' timed out after {timeout.TotalSeconds} seconds", innerException)
    {
        RequestType = requestType;
        Timeout = timeout;
    }
}