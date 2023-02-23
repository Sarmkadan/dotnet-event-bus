// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetEventBus.Models;

/// <summary>
/// Represents the result of publishing an event to the event bus.
/// </summary>
public class PublishResult
{
    /// <summary>
    /// The message ID of the published event.
    /// </summary>
    public string MessageId { get; set; }

    /// <summary>
    /// Whether the publish operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The number of handlers that processed this message.
    /// </summary>
    public int HandlersInvoked { get; set; }

    /// <summary>
    /// The number of handlers that failed processing.
    /// </summary>
    public int FailedHandlers { get; set; }

    /// <summary>
    /// Error message if the publish failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Exception that occurred during publish if any.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Time taken to publish and process the message.
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }

    /// <summary>
    /// Names of handlers that succeeded.
    /// </summary>
    public List<string> SuccessfulHandlers { get; set; }

    /// <summary>
    /// Names of handlers that failed.
    /// </summary>
    public List<string> FailedHandlerNames { get; set; }

    /// <summary>
    /// Initializes a new instance of the PublishResult class.
    /// </summary>
    public PublishResult(string messageId)
    {
        MessageId = messageId ?? throw new ArgumentNullException(nameof(messageId));
        Success = false;
        HandlersInvoked = 0;
        FailedHandlers = 0;
        SuccessfulHandlers = new List<string>();
        FailedHandlerNames = new List<string>();
        ElapsedTime = TimeSpan.Zero;
    }

    /// <summary>
    /// Marks this result as successful.
    /// </summary>
    public void MarkSuccess(int successfulHandlers = 1)
    {
        Success = true;
        HandlersInvoked = successfulHandlers;
        FailedHandlers = 0;
        ErrorMessage = null;
        Exception = null;
    }

    /// <summary>
    /// Adds a failed handler to the result.
    /// </summary>
    public void AddFailedHandler(string handlerName, Exception exception)
    {
        if (string.IsNullOrWhiteSpace(handlerName))
            throw new ArgumentException("Handler name cannot be empty", nameof(handlerName));

        FailedHandlerNames.Add(handlerName);
        FailedHandlers++;

        if (Exception == null)
        {
            Exception = exception;
            ErrorMessage = exception?.Message;
        }
    }

    /// <summary>
    /// Adds a successful handler to the result.
    /// </summary>
    public void AddSuccessfulHandler(string handlerName)
    {
        if (string.IsNullOrWhiteSpace(handlerName))
            throw new ArgumentException("Handler name cannot be empty", nameof(handlerName));

        SuccessfulHandlers.Add(handlerName);
        HandlersInvoked++;
    }

    /// <summary>
    /// Gets a summary of the publish result.
    /// </summary>
    public string GetSummary()
    {
        return $"Publish [{MessageId}] {(Success ? "Success" : "Failed")}: " +
               $"{HandlersInvoked} invoked, {FailedHandlers} failed, elapsed {ElapsedTime.TotalMilliseconds}ms";
    }

    /// <summary>
    /// Creates a failed result with the given error.
    /// </summary>
    public static PublishResult CreateFailed(string messageId, Exception exception)
    {
        var result = new PublishResult(messageId)
        {
            Success = false,
            Exception = exception,
            ErrorMessage = exception?.Message
        };
        return result;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static PublishResult CreateSuccess(string messageId, int handlersInvoked = 1)
    {
        var result = new PublishResult(messageId);
        result.MarkSuccess(handlersInvoked);
        return result;
    }
}
