#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetEventBus.Configuration;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Services;

/// <summary>
/// Handles retry logic with exponential backoff and jitter for failed event handler invocations.
/// </summary>
public sealed class RetryHandler
{
    private readonly ILogger<RetryHandler> _logger;
    private readonly EventBusOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger for retry diagnostics.</param>
    /// <param name="options">Event bus options containing retry configuration.</param>
    public RetryHandler(ILogger<RetryHandler> logger, EventBusOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Executes an async action with retry logic. On exhausted retries, returns the last exception.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="handlerName">Name of the handler for logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Null on success, or the last exception if all retries were exhausted.</returns>
    public async Task<Exception?> ExecuteWithRetryAsync(
        Func<CancellationToken, Task> action,
        string handlerName,
        CancellationToken cancellationToken = default)
    {
        var maxAttempts = _options.MaxRetryAttempts > 0
            ? _options.MaxRetryAttempts
            : EventBusConstants.DefaultMaxRetryAttempts;

        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await action(cancellationToken).ConfigureAwait(false);
                if (attempt > 1)
                {
                    _logger.LogInformation(
                        "Handler {HandlerName} succeeded on attempt {Attempt}/{Max}",
                        handlerName, attempt, maxAttempts);
                }
                return null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(
                    ex,
                    "Handler {HandlerName} failed on attempt {Attempt}/{Max}",
                    handlerName, attempt, maxAttempts);

                if (attempt < maxAttempts)
                {
                    var delay = ComputeBackoffDelay(attempt);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        _logger.LogError(
            lastException,
            "Handler {HandlerName} exhausted all {Max} retry attempts",
            handlerName, maxAttempts);

        return lastException;
    }

    /// <summary>
    /// Computes the backoff delay for a given attempt using exponential backoff with jitter.
    /// </summary>
    /// <param name="attempt">The current attempt number (1-based).</param>
    /// <returns>The delay to wait before retrying.</returns>
    public TimeSpan ComputeBackoffDelay(int attempt)
    {
        var baseDelay = _options.RetryDelay.TotalMilliseconds > 0
            ? _options.RetryDelay.TotalMilliseconds
            : EventBusConstants.DefaultRetryDelayMilliseconds;

        var multiplier = _options.RetryDelayMultiplier > 0
            ? _options.RetryDelayMultiplier
            : EventBusConstants.DefaultRetryDelayMultiplier;

        var delayMs = baseDelay * Math.Pow(multiplier, attempt - 1);
        var maxDelayMs = EventBusConstants.MaxDefaultRetryDelaySeconds * 1000.0;
        delayMs = Math.Min(delayMs, maxDelayMs);

        // Add jitter: +/- 25%
        var jitter = Random.Shared.NextDouble() * 0.5 + 0.75;
        delayMs *= jitter;

        return TimeSpan.FromMilliseconds(delayMs);
    }

    /// <summary>
    /// Determines whether a given exception type should be retried.
    /// Infrastructure exceptions (e.g. timeout, IO) are retried; logic errors are not.
    /// </summary>
    /// <param name="exception">The exception to evaluate.</param>
    /// <returns>True if the operation should be retried.</returns>
    public static bool ShouldRetry(Exception exception)
    {
        return exception is TimeoutException
            or IOException
            or HttpRequestException
            or InvalidOperationException;
    }
}
