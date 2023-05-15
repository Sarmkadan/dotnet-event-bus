#nullable enable

namespace DotnetEventBus.Models;

/// <summary>
/// Provides useful extension methods for EventMessage to simplify common operations.
/// </summary>
public static class EventMessageExtensions
{
    /// <summary>
    /// Creates a clone of the message with the same properties.
    /// </summary>
    /// <param name="message">The source message to clone</param>
    /// <returns>A deep copy of the message</returns>
    public static EventMessage Clone(this EventMessage message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var clone = new EventMessage(message.EventType, message.Payload)
        {
            MessageId = message.MessageId,
            CorrelationId = message.CorrelationId,
            Source = message.Source,
            Scope = message.Scope,
            ProcessingAttempts = message.ProcessingAttempts,
            CreatedAtUtc = message.CreatedAtUtc
        };

        foreach (var header in message.Headers)
        {
            clone.Headers[header.Key] = header.Value;
        }

        return clone;
    }

    /// <summary>
    /// Checks if the message has exceeded the maximum processing attempts.
    /// </summary>
    /// <param name="message">The message to check</param>
    /// <param name="maxAttempts">Maximum allowed processing attempts</param>
    /// <returns>True if processing attempts exceeded maxAttempts; otherwise false</returns>
    public static bool HasExceededMaxAttempts(this EventMessage message, int maxAttempts)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return message.ProcessingAttempts >= maxAttempts;
    }

    /// <summary>
    /// Adds multiple headers to the message in a single operation.
    /// </summary>
    /// <param name="message">The message to add headers to</param>
    /// <param name="headers">Dictionary of headers to add</param>
    public static void AddHeaders(this EventMessage message, Dictionary<string, string> headers)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (headers == null)
            throw new ArgumentNullException(nameof(headers));

        foreach (var header in headers)
        {
            message.AddHeader(header.Key, header.Value);
        }
    }

    /// <summary>
    /// Gets a header value by key, returning a default value if not found.
    /// </summary>
    /// <param name="message">The message to get header from</param>
    /// <param name="key">Header key</param>
    /// <param name="defaultValue">Default value to return if header not found</param>
    /// <returns>The header value or defaultValue if not found</returns>
    public static string GetHeaderOrDefault(this EventMessage message, string key, string defaultValue = "")
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Header key cannot be empty", nameof(key));

        var value = message.GetHeader(key);
        return value ?? defaultValue;
    }
}