#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using DotnetEventBus.Middleware;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Configuration;

/// <summary>
/// Extension methods for fluent pipeline configuration.
/// Simplifies middleware registration and pipeline setup.
/// Why: Provides clean, discoverable API for configuring the event bus pipeline.
/// </summary>
public static class PipelineBuilderExtensions
{
    /// <summary>
    /// Adds logging middleware to the pipeline.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="logLevel">The log level for middleware events.</param>
    /// <param name="logPayloads">Whether to log event payloads.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> or <paramref name="loggerFactory"/> is null.</exception>
    public static PipelineBuilder AddLogging(
        this PipelineBuilder builder,
        ILoggerFactory loggerFactory,
        LogLevel logLevel = LogLevel.Information,
        bool logPayloads = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var logger = loggerFactory.CreateLogger<EventBusLoggingMiddleware>();
        var middleware = new EventBusLoggingMiddleware(logger, logLevel, logPayloads);

        builder.Use(next => middleware.Create(next));
        return builder;
    }

    /// <summary>
    /// Adds error handling middleware to the pipeline.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="maxRetries">Maximum number of retry attempts. Must be non-negative.</param>
    /// <param name="retryDelay">Delay between retry attempts. Defaults to 1 second.</param>
    /// <param name="errorHandler">Optional custom error handler.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> or <paramref name="loggerFactory"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxRetries"/> is negative.</exception>
    public static PipelineBuilder AddErrorHandling(
        this PipelineBuilder builder,
        ILoggerFactory loggerFactory,
        int maxRetries = 3,
        TimeSpan? retryDelay = null,
        Func<EventContext, Exception, Task<bool>>? errorHandler = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        var logger = loggerFactory.CreateLogger<ErrorHandlingMiddleware>();
        var middleware = new ErrorHandlingMiddleware(logger, maxRetries, retryDelay, errorHandler);

        builder.Use(next => middleware.Create(next));
        return builder;
    }

    /// <summary>
    /// Adds rate limiting middleware to the pipeline.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="requestsPerWindow">Maximum requests per time window. Must be positive.</param>
    /// <param name="timeWindow">Time window for rate limiting. Defaults to 60 seconds.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> or <paramref name="loggerFactory"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="requestsPerWindow"/> is not positive.</exception>
    public static PipelineBuilder AddRateLimiting(
        this PipelineBuilder builder,
        ILoggerFactory loggerFactory,
        int requestsPerWindow = 1000,
        TimeSpan? timeWindow = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(requestsPerWindow, 0);

        var logger = loggerFactory.CreateLogger<RateLimitingMiddleware>();
        var middleware = new RateLimitingMiddleware(logger, requestsPerWindow, timeWindow);

        builder.Use(next => middleware.Create(next));
        return builder;
    }

    /// <summary>
    /// Adds custom middleware to the pipeline.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="middleware">The middleware delegate to add.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> or <paramref name="middleware"/> is null.</exception>
    public static PipelineBuilder UseMiddleware(
        this PipelineBuilder builder,
        Func<EventBusMiddleware, EventBusMiddleware> middleware)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(middleware);

        builder.Use(middleware);
        return builder;
    }

    /// <summary>
    /// Creates a standard pipeline with common middleware.
    /// Includes logging, rate limiting, and error handling.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>The configured pipeline builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> or <paramref name="loggerFactory"/> is null.</exception>
    public static PipelineBuilder CreateStandardPipeline(
        this PipelineBuilder builder,
        ILoggerFactory loggerFactory)
        => builder
            .AddLogging(loggerFactory)
            .AddRateLimiting(loggerFactory)
            .AddErrorHandling(loggerFactory);

    /// <summary>
    /// Creates a high-performance pipeline with minimal overhead.
    /// Optimized for production with reduced logging and conservative rate limits.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>The configured pipeline builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> or <paramref name="loggerFactory"/> is null.</exception>
    public static PipelineBuilder CreateHighPerformancePipeline(
        this PipelineBuilder builder,
        ILoggerFactory loggerFactory)
        => builder
            .AddErrorHandling(loggerFactory, maxRetries: 2)
            .AddRateLimiting(loggerFactory, requestsPerWindow: 10000);

    /// <summary>
    /// Creates a development pipeline with comprehensive logging.
    /// Includes debug-level logging, payload logging, and extended retry attempts.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>The configured pipeline builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> or <paramref name="loggerFactory"/> is null.</exception>
    public static PipelineBuilder CreateDevelopmentPipeline(
        this PipelineBuilder builder,
        ILoggerFactory loggerFactory)
        => builder
            .AddLogging(loggerFactory, LogLevel.Debug, logPayloads: true)
            .AddErrorHandling(loggerFactory, maxRetries: 5)
            .AddRateLimiting(loggerFactory);
}