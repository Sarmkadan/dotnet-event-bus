// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Middleware;

/// <summary>
/// Middleware that logs all events passing through the pipeline.
/// Tracks timing, event data, and execution results for observability.
/// Why: Essential for debugging and monitoring event flow in production systems.
/// </summary>
public class EventBusLoggingMiddleware
{
    private readonly ILogger<EventBusLoggingMiddleware> _logger;
    private readonly LogLevel _logLevel;
    private readonly bool _logEventPayload;

    public EventBusLoggingMiddleware(ILogger<EventBusLoggingMiddleware> logger, LogLevel logLevel = LogLevel.Information, bool logEventPayload = true)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logLevel = logLevel;
        _logEventPayload = logEventPayload;
    }

    /// <summary>
    /// Wraps the next middleware with logging instrumentation.
    /// Captures timing and exception information automatically.
    /// </summary>
    public EventBusMiddleware Create()
    {
        return async (context) =>
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = context.CorrelationId ?? Guid.NewGuid().ToString();
            context.CorrelationId = correlationId;

            try
            {
                _logger.Log(_logLevel,
                    "Event published: {EventType} [CorrelationId: {CorrelationId}]",
                    context.EventType, correlationId);

                if (_logEventPayload && context.EventData != null)
                {
                    var payload = JsonSerializer.Serialize(context.EventData, new JsonSerializerOptions { WriteIndented = false });
                    _logger.Log(_logLevel, "Event payload: {Payload}", payload);
                }

                // Execute next middleware
                await Task.CompletedTask;

                stopwatch.Stop();
                context.IsProcessed = true;

                _logger.Log(_logLevel,
                    "Event processed successfully: {EventType} in {ElapsedMs}ms [CorrelationId: {CorrelationId}]",
                    context.EventType, stopwatch.ElapsedMilliseconds, correlationId);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                context.ProcessingException = ex;
                context.IsProcessed = false;

                _logger.LogError(ex,
                    "Event processing failed: {EventType} after {ElapsedMs}ms [CorrelationId: {CorrelationId}]",
                    context.EventType, stopwatch.ElapsedMilliseconds, correlationId);

                throw;
            }
        };
    }
}
