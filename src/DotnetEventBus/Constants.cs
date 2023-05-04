#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetEventBus;

/// <summary>
/// Constants used throughout the event bus system.
/// </summary>
public static class EventBusConstants
{
    /// <summary>
    /// Default maximum number of retry attempts for failed handlers.
    /// </summary>
    public const int DefaultMaxRetryAttempts = 3;

    /// <summary>
    /// Default timeout for handler execution in seconds.
    /// </summary>
    public const int DefaultHandlerTimeoutSeconds = 30;

    /// <summary>
    /// Default base delay for retry backoff in milliseconds.
    /// </summary>
    public const int DefaultRetryDelayMilliseconds = 100;

    /// <summary>
    /// Default multiplier for exponential backoff.
    /// </summary>
    public const double DefaultRetryDelayMultiplier = 2.0;

    /// <summary>
    /// Maximum default retry delay in seconds.
    /// </summary>
    public const int MaxDefaultRetryDelaySeconds = 30;

    /// <summary>
    /// Default maximum number of concurrent handlers.
    /// </summary>
    public static readonly int DefaultMaxConcurrentHandlers = Environment.ProcessorCount;

    /// <summary>
    /// Message header keys for internal use.
    /// </summary>
    public static class MessageHeaders
    {
        /// <summary>
        /// Header key for correlation ID.
        /// </summary>
        public const string CorrelationId = "X-Correlation-Id";

        /// <summary>
        /// Header key for source system.
        /// </summary>
        public const string Source = "X-Source";

        /// <summary>
        /// Header key for handler name.
        /// </summary>
        public const string Handler = "X-Handler";

        /// <summary>
        /// Header key for processing attempt number.
        /// </summary>
        public const string AttemptNumber = "X-Attempt-Number";

        /// <summary>
        /// Header key for message scope.
        /// </summary>
        public const string Scope = "X-Scope";

        /// <summary>
        /// Header key for timestamp.
        /// </summary>
        public const string Timestamp = "X-Timestamp";
    }

    /// <summary>
    /// Standard event names for internal system events.
    /// </summary>
    public static class SystemEvents
    {
        /// <summary>
        /// Fired when a message is published.
        /// </summary>
        public const string MessagePublished = "system.message.published";

        /// <summary>
        /// Fired when publishing fails.
        /// </summary>
        public const string MessagePublishFailed = "system.message.publish.failed";

        /// <summary>
        /// Fired when a message is moved to dead letter.
        /// </summary>
        public const string MessageDeadLettered = "system.message.deadlettered";

        /// <summary>
        /// Fired when a dead letter entry is reprocessed.
        /// </summary>
        public const string DeadLetterReprocessed = "system.deadletter.reprocessed";

        /// <summary>
        /// Fired when a handler fails.
        /// </summary>
        public const string HandlerFailed = "system.handler.failed";

        /// <summary>
        /// Fired when a handler times out.
        /// </summary>
        public const string HandlerTimedOut = "system.handler.timedout";
    }

    /// <summary>
    /// Response status codes for request/reply operations.
    /// </summary>
    public enum ResponseStatusCode
    {
        /// <summary>
        /// Request processed successfully.
        /// </summary>
        Success = 200,

        /// <summary>
        /// Request processing failed.
        /// </summary>
        Failure = 400,

        /// <summary>
        /// Request timed out.
        /// </summary>
        Timeout = 408,

        /// <summary>
        /// No handler found for the request.
        /// </summary>
        NoHandler = 404,

        /// <summary>
        /// Handler threw an exception.
        /// </summary>
        Exception = 500
    }
}

/// <summary>
/// Handler execution modes.
/// </summary>
public enum HandlerExecutionMode
{
    /// <summary>
    /// Handler is executed synchronously.
    /// </summary>
    Synchronous = 0,

    /// <summary>
    /// Handler is executed asynchronously.
    /// </summary>
    Asynchronous = 1,

    /// <summary>
    /// Handler is executed fire-and-forget (no await).
    /// </summary>
    FireAndForget = 2
}

/// <summary>
/// Priority levels for handler execution ordering.
/// </summary>
public enum HandlerPriority
{
    /// <summary>
    /// Lowest priority.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority.
    /// </summary>
    Normal = 5,

    /// <summary>
    /// Medium priority. Alias of <see cref="Normal"/> for readability at call sites.
    /// </summary>
    Medium = 5,

    /// <summary>
    /// High priority.
    /// </summary>
    High = 10,

    /// <summary>
    /// Highest priority (executes first).
    /// </summary>
    Critical = 20
}
