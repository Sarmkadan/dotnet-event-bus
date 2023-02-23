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

        builder.Use(next => middleware.Create());
        return builder;
    }

    /// <summary>
    /// Adds error handling middleware to the pipeline.
    /// </summary>
    public static PipelineBuilder AddErrorHandling(
        this PipelineBuilder builder,
        ILoggerFactory loggerFactory,
        int maxRetries = 3,
        TimeSpan? retryDelay = null,
        Func<EventContext, Exception, Task<bool>>? errorHandler = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var logger = loggerFactory.CreateLogger<ErrorHandlingMiddleware>();
        var middleware = new ErrorHandlingMiddleware(logger, maxRetries, retryDelay, errorHandler);

        builder.Use(next => middleware.Create());
        return builder;
    }

    /// <summary>
    /// Adds rate limiting middleware to the pipeline.
    /// </summary>
    public static PipelineBuilder AddRateLimiting(
        this PipelineBuilder builder,
        ILoggerFactory loggerFactory,
        int requestsPerWindow = 1000,
        TimeSpan? timeWindow = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var logger = loggerFactory.CreateLogger<RateLimitingMiddleware>();
        var middleware = new RateLimitingMiddleware(logger, requestsPerWindow, timeWindow);

        builder.Use(next => middleware.Create());
        return builder;
    }

    /// <summary>
    /// Adds custom middleware to the pipeline.
    /// </summary>
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
    /// </summary>
    public static PipelineBuilder CreateStandardPipeline(
        this PipelineBuilder builder,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        return builder
            .AddLogging(loggerFactory)
            .AddRateLimiting(loggerFactory)
            .AddErrorHandling(loggerFactory);
    }

    /// <summary>
    /// Creates a high-performance pipeline with minimal overhead.
    /// </summary>
    public static PipelineBuilder CreateHighPerformancePipeline(
        this PipelineBuilder builder,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        return builder
            .AddErrorHandling(loggerFactory, maxRetries: 2)
            .AddRateLimiting(loggerFactory, requestsPerWindow: 10000);
    }

    /// <summary>
    /// Creates a development pipeline with comprehensive logging.
    /// </summary>
    public static PipelineBuilder CreateDevelopmentPipeline(
        this PipelineBuilder builder,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        return builder
            .AddLogging(loggerFactory, LogLevel.Debug, logPayloads: true)
            .AddErrorHandling(loggerFactory, maxRetries: 5)
            .AddRateLimiting(loggerFactory);
    }
}
