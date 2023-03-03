#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetEventBus.Models;

/// <summary>
/// Represents a message that failed processing and was moved to the dead letter queue.
/// </summary>
public sealed class DeadLetterEntry
{
    /// <summary>
    /// Unique identifier for this dead letter entry.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The original message that failed.
    /// </summary>
    public EventMessage Message { get; set; }

    /// <summary>
    /// The handler that failed to process this message.
    /// </summary>
    public string FailedHandlerName { get; set; }

    /// <summary>
    /// The exception that occurred during processing.
    /// </summary>
    public string ExceptionMessage { get; set; }

    /// <summary>
    /// Full exception stack trace for debugging.
    /// </summary>
    public string? ExceptionStackTrace { get; set; }

    /// <summary>
    /// Timestamp when the message was moved to dead letter.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// The maximum number of retry attempts made before moving to dead letter.
    /// </summary>
    public int MaxRetryAttempts { get; set; }

    /// <summary>
    /// Current status of the dead letter entry.
    /// </summary>
    public DeadLetterStatus Status { get; set; }

    /// <summary>
    /// Optional reason for the dead letter status.
    /// </summary>
    public string? StatusReason { get; set; }

    /// <summary>
    /// Initializes a new instance of the DeadLetterEntry class.
    /// </summary>
    public DeadLetterEntry(
        EventMessage message,
        string failedHandlerName,
        Exception exception,
        int maxRetryAttempts = 3)
    {
        Id = Guid.NewGuid().ToString();
        Message = message ?? throw new ArgumentNullException(nameof(message));
        FailedHandlerName = failedHandlerName ?? throw new ArgumentNullException(nameof(failedHandlerName));
        ExceptionMessage = exception?.Message ?? "Unknown exception";
        // Hotfix: Capture full exception details including stack trace and inner exceptions
        // using ToString() instead of just StackTrace property to preserve complete exception chain
        ExceptionStackTrace = exception?.ToString();
        CreatedAtUtc = DateTime.UtcNow;
        MaxRetryAttempts = maxRetryAttempts;
        Status = DeadLetterStatus.Pending;
    }

    /// <summary>
    /// Marks this entry as reviewed but not reprocessed.
    /// </summary>
    public void MarkAsReviewed(string? reason = null)
    {
        Status = DeadLetterStatus.ReviewedNotProcessed;
        StatusReason = reason;
    }

    /// <summary>
    /// Marks this entry as successfully reprocessed.
    /// </summary>
    public void MarkAsReprocessed()
    {
        Status = DeadLetterStatus.Reprocessed;
        StatusReason = "Successfully reprocessed";
    }

    /// <summary>
    /// Marks this entry as failed to reprocess.
    /// </summary>
    public void MarkAsReprocessFailed(string reason)
    {
        Status = DeadLetterStatus.ReprocessFailed;
        StatusReason = reason;
    }

    /// <summary>
    /// Gets a summary of this dead letter entry.
    /// </summary>
    public string GetSummary()
    {
        return $"Dead Letter [{Id}]: {FailedHandlerName} failed to process {Message.EventType}" +
               $" at {CreatedAtUtc:O}. Error: {ExceptionMessage}";
    }
}

/// <summary>
/// Defines the status of a dead letter entry.
/// </summary>
public enum DeadLetterStatus
{
    /// <summary>
    /// Entry is pending review/processing.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Entry was reviewed but not reprocessed.
    /// </summary>
    ReviewedNotProcessed = 1,

    /// <summary>
    /// Entry was successfully reprocessed.
    /// </summary>
    Reprocessed = 2,

    /// <summary>
    /// Reprocessing attempt failed.
    /// </summary>
    ReprocessFailed = 3,

    /// <summary>
    /// Entry was permanently failed and archived.
    /// </summary>
    Archived = 4
}
