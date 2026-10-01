#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetEventBus.Models;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Services;

/// <summary>
/// Handles serialization and deserialization of event messages using System.Text.Json.
/// Provides a centralized, consistent serialization strategy for the event bus.
/// </summary>
public sealed class EventSerializer
{
    private readonly ILogger<EventSerializer> _logger;
    private readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSerializer"/> class.
    /// </summary>
    /// <param name="logger">Logger for serialization diagnostics.</param>
    /// <param name="serializerOptions">Optional custom JSON serializer options.</param>
    public EventSerializer(ILogger<EventSerializer> logger, JsonSerializerOptions? serializerOptions = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializerOptions = serializerOptions ?? CreateDefaultOptions();
    }

    /// <summary>
    /// Serializes an event object to a JSON string.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="event">The event to serialize.</param>
    /// <returns>The JSON representation of the event.</returns>
    /// <exception cref="InvalidOperationException">Thrown when serialization fails.</exception>
    public string Serialize<TEvent>(TEvent @event) where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(@event);
        try
        {
            var json = JsonSerializer.Serialize(@event, _serializerOptions);
            _logger.LogDebug("Serialized {EventType} ({Length} chars)", typeof(TEvent).Name, json.Length);
            return json;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize event of type {EventType}", typeof(TEvent).Name);
            throw new InvalidOperationException($"Failed to serialize event of type {typeof(TEvent).Name}", ex);
        }
    }

    /// <summary>
    /// Deserializes a JSON string to the specified event type.
    /// </summary>
    /// <typeparam name="TEvent">The target event type.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized event object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails or produces null.</exception>
    public TEvent Deserialize<TEvent>(string json) where TEvent : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        try
        {
            var result = JsonSerializer.Deserialize<TEvent>(json, _serializerOptions);
            if (result is null)
            {
                throw new InvalidOperationException($"Deserialization of {typeof(TEvent).Name} produced null");
            }
            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize JSON to {EventType}", typeof(TEvent).Name);
            throw new InvalidOperationException($"Failed to deserialize JSON to {typeof(TEvent).Name}", ex);
        }
    }

    /// <summary>
    /// Deserializes a JSON string to the specified runtime type.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="eventType">The target type.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails or produces null.</exception>
    public object Deserialize(string json, Type eventType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ArgumentNullException.ThrowIfNull(eventType);
        try
        {
            var result = JsonSerializer.Deserialize(json, eventType, _serializerOptions);
            if (result is null)
            {
                throw new InvalidOperationException($"Deserialization of {eventType.Name} produced null");
            }
            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize JSON to {EventType}", eventType.Name);
            throw new InvalidOperationException($"Failed to deserialize JSON to {eventType.Name}", ex);
        }
    }

    /// <summary>
    /// Wraps an event in an <see cref="EventEnvelope"/> and serializes the entire envelope.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="event">The event to wrap.</param>
    /// <param name="correlationId">Optional correlation ID for tracing.</param>
    /// <returns>The JSON representation of the envelope.</returns>
    public string SerializeEnvelope<TEvent>(TEvent @event, string? correlationId = null) where TEvent : class
    {
        var envelope = new EventEnvelope
        {
            EventType = typeof(TEvent).FullName ?? typeof(TEvent).Name,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString(),
            Payload = (object)Serialize(@event)
        };
        return JsonSerializer.Serialize(envelope, _serializerOptions);
    }

    private static JsonSerializerOptions CreateDefaultOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };
    }
}
