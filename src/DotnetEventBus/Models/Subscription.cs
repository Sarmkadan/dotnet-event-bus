// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetEventBus.Models;

/// <summary>
/// Represents a subscription between an event type and its handlers.
/// </summary>
public class Subscription
{
    /// <summary>
    /// Unique identifier for this subscription.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The event type being subscribed to.
    /// </summary>
    public string EventType { get; set; }

    /// <summary>
    /// The handler method or action that processes the event.
    /// </summary>
    public Delegate Handler { get; set; }

    /// <summary>
    /// Display name of the handler for logging and debugging.
    /// </summary>
    public string HandlerName { get; set; }

    /// <summary>
    /// Whether this subscription is active or disabled.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Priority for handler execution (higher priority runs first).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Whether this handler should process events asynchronously.
    /// </summary>
    public bool IsAsync { get; set; }

    /// <summary>
    /// Maximum time to wait for this handler to complete.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Whether the handler can process messages concurrently.
    /// </summary>
    public bool AllowConcurrent { get; set; }

    /// <summary>
    /// Whether to send this subscription to dead letter on failure.
    /// </summary>
    public bool SendToDeadLetterOnFailure { get; set; }

    /// <summary>
    /// Timestamp when this subscription was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Initializes a new instance of the Subscription class.
    /// </summary>
    public Subscription(
        string eventType,
        Delegate handler,
        string handlerName,
        int priority = 0)
    {
        Id = Guid.NewGuid().ToString();
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        HandlerName = handlerName ?? throw new ArgumentNullException(nameof(handlerName));
        IsActive = true;
        Priority = priority;
        IsAsync = IsAsyncDelegate(handler);
        AllowConcurrent = true;
        SendToDeadLetterOnFailure = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Determines if a delegate is asynchronous.
    /// </summary>
    private static bool IsAsyncDelegate(Delegate handler)
    {
        var method = handler.Method;
        return method.ReturnType == typeof(Task) ||
               (method.ReturnType.IsGenericType &&
                method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));
    }

    /// <summary>
    /// Disables this subscription.
    /// </summary>
    public void Disable()
    {
        IsActive = false;
    }

    /// <summary>
    /// Enables this subscription.
    /// </summary>
    public void Enable()
    {
        IsActive = true;
    }

    /// <summary>
    /// Sets the timeout for handler execution.
    /// </summary>
    public void SetTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentException("Timeout must be greater than zero", nameof(timeout));

        Timeout = timeout;
    }
}
