#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Middleware;

/// <summary>
/// Middleware that handles errors and exceptions in the event pipeline.
/// Provides centralized exception handling, recovery, and dead-letter routing.
/// Why: Prevents cascading failures and ensures failed events are captured for analysis.
/// </summary>
public sealed class ErrorHandlingMiddleware
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly int _maxRetries;
    private readonly TimeSpan _retryDelay;
    private readonly Func<EventContext, Exception, Task<bool>>? _errorHandler;

    public ErrorHandlingMiddleware(
        ILogger<ErrorHandlingMiddleware> logger,
        int maxRetries = 3,
        TimeSpan? retryDelay = null,
        Func<EventContext, Exception, Task<bool>>? errorHandler = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxRetries = maxRetries;
        _retryDelay = retryDelay ?? TimeSpan.FromSeconds(1);
        _errorHandler = errorHandler;
    }

    public EventBusMiddleware Create(EventBusMiddleware next)
    {
        return async (context) =>
        {
            int attemptCount = 0;
            Exception? lastException = null;

            while (attemptCount < _maxRetries)
            {
                try
                {
                    attemptCount++;
                    await next(context);
                    context.IsProcessed = true;
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    context.ProcessingException = ex;
                    context.Metadata["attempt"] = attemptCount;

                    _logger.LogWarning(
                        "Event processing attempt {Attempt}/{MaxRetries} failed: {Exception}",
                        attemptCount, _maxRetries, ex.Message);

                    if (attemptCount < _maxRetries)
                    {
                        await Task.Delay(_retryDelay);
                    }
                }
            }

            // All retries exhausted - invoke custom error handler if provided
            if (_errorHandler is not null && lastException is not null)
            {
                bool handled = await _errorHandler(context, lastException);
                if (handled)
                {
                    context.IsProcessed = true;
                    context.Metadata["recoveredByHandler"] = true;
                    return;
                }
            }

            // Could not recover - rethrow or mark as failed
            context.IsProcessed = false;
            if (lastException is not null)
            {
                throw new EventProcessingException(
                    $"Event {context.EventType} failed after {_maxRetries} retries",
                    lastException);
            }
        };
    }
}

/// <summary>
/// Exception thrown when event processing fails after all retry attempts.
/// </summary>
public sealed class EventProcessingException : Exception
{
    public EventProcessingException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
