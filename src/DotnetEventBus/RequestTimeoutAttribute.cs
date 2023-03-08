#nullable enable

using System;

namespace DotnetEventBus;

/// <summary>
/// Specifies a timeout for a request/reply operation.
/// Can be applied to event types to override the global `EventBusOptions.RequestTimeout`.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class RequestTimeoutAttribute : Attribute
{
    /// <summary>
    /// The timeout in milliseconds.
    /// </summary>
    public int TimeoutMilliseconds { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestTimeoutAttribute"/> class.
    /// </summary>
    /// <param name="timeoutMilliseconds">The timeout in milliseconds.</param>
    public RequestTimeoutAttribute(int timeoutMilliseconds)
    {
        if (timeoutMilliseconds <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(timeoutMilliseconds),
                "Timeout must be greater than zero.");

        TimeoutMilliseconds = timeoutMilliseconds;
    }
}
