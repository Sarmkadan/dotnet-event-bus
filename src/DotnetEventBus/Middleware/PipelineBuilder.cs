#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace DotnetEventBus.Middleware;

/// <summary>
/// Builds and manages the middleware pipeline for event processing.
/// Uses a chain-of-responsibility pattern to compose middleware components.
/// </summary>
public sealed class PipelineBuilder
{
    private readonly List<Func<EventBusMiddleware, EventBusMiddleware>> _middlewares = [];

    /// <summary>
    /// Registers a middleware component in the pipeline.
    /// Middleware is executed in the order it was registered.
    /// </summary>
    public PipelineBuilder Use(Func<EventBusMiddleware, EventBusMiddleware> middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        _middlewares.Add(middleware);
        return this;
    }

    /// <summary>
    /// Builds the complete pipeline by composing all registered middleware.
    /// Returns the composed middleware that executes all components.
    /// </summary>
    public EventBusMiddleware Build()
    {
        // Base middleware that actually publishes the event
        EventBusMiddleware pipeline = async (context) =>
        {
            // Base implementation - would be replaced in DI
            await Task.CompletedTask;
        };

        // Apply middleware in reverse order so first registered executes first
        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var currentPipeline = pipeline;
            pipeline = (context) => middleware(currentPipeline)(context);
        }

        return pipeline;
    }

    /// <summary>
    /// Clears all registered middleware from the pipeline.
    /// </summary>
    public void Clear() => _middlewares.Clear();
}

/// <summary>
/// Delegate representing the middleware pipeline execution.
/// Each middleware receives the next middleware in the chain.
/// </summary>
public delegate Task EventBusMiddleware(EventContext context);

/// <summary>
/// Context passed through the middleware pipeline.
/// Contains the event data and metadata for processing.
/// </summary>
public sealed class EventContext
{
    public required string EventType { get; set; }
    public required object EventData { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
    public bool IsProcessed { get; set; }
    public Exception? ProcessingException { get; set; }
}
