#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetEventBus.Configuration;

/// <summary>
/// Configures event routing rules for conditional event delivery.
/// Allows events to be routed to different handlers based on content or metadata.
/// Why: Enables sophisticated event routing without handler modifications.
/// </summary>
public sealed class EventRoutingConfiguration
{
    private readonly Dictionary<string, List<RoutingRule>> _routes = [];

    /// <summary>
    /// Adds a routing rule for an event type.
    /// </summary>
    public void AddRoute(string eventType, RoutingRule rule)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(rule);

        if (!_routes.ContainsKey(eventType))
        {
            _routes[eventType] = [];
        }

        _routes[eventType].Add(rule);
    }

    /// <summary>
    /// Gets routes for a specific event type.
    /// </summary>
    public IEnumerable<RoutingRule> GetRoutes(string eventType)
    {
        if (_routes.TryGetValue(eventType, out var routes))
        {
            return routes;
        }

        return Enumerable.Empty<RoutingRule>();
    }

    /// <summary>
    /// Determines if an event should be routed to a handler.
    /// </summary>
    public bool ShouldRoute(string eventType, string handlerName, Dictionary<string, object>? metadata = null)
    {
        var routes = GetRoutes(eventType);
        if (!routes.Any())
            return true; // No routes defined, deliver to all

        var targetRoute = routes.FirstOrDefault(r => r.TargetHandler == handlerName);
        if (targetRoute is null)
            return false;

        // Check route conditions
        if (targetRoute.Condition is not null && metadata is not null)
        {
            return targetRoute.Condition(metadata);
        }

        return true;
    }

    /// <summary>
    /// Gets all configured event types.
    /// </summary>
    public IEnumerable<string> GetConfiguredEventTypes()
    {
        return _routes.Keys;
    }

    /// <summary>
    /// Clears all routes.
    /// </summary>
    public void Clear()
    {
        _routes.Clear();
    }
}

/// <summary>
/// Represents a routing rule for event delivery.
/// </summary>
public sealed class RoutingRule
{
    /// <summary>
    /// The target handler name.
    /// </summary>
    public required string TargetHandler { get; set; }

    /// <summary>
    /// Optional condition for routing (based on metadata).
    /// </summary>
    public Func<Dictionary<string, object>, bool>? Condition { get; set; }

    /// <summary>
    /// Priority of this rule (higher = evaluated first).
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Whether to continue evaluating rules after this one matches.
    /// </summary>
    public bool ContinueEvaluation { get; set; } = false;
}

/// <summary>
/// Fluent builder for event routing configuration.
/// </summary>
public sealed class EventRoutingBuilder
{
    private readonly EventRoutingConfiguration _configuration = new();

    /// <summary>
    /// Routes an event type to a handler unconditionally.
    /// </summary>
    public EventRoutingBuilder RouteEvent(string eventType, string handlerName)
    {
        var rule = new RoutingRule { TargetHandler = handlerName };
        _configuration.AddRoute(eventType, rule);
        return this;
    }

    /// <summary>
    /// Routes an event type to a handler based on a condition.
    /// </summary>
    public EventRoutingBuilder RouteEventIf(
        string eventType,
        string handlerName,
        Func<Dictionary<string, object>, bool> condition,
        int priority = 0)
    {
        var rule = new RoutingRule
        {
            TargetHandler = handlerName,
            Condition = condition,
            Priority = priority
        };

        _configuration.AddRoute(eventType, rule);
        return this;
    }

    /// <summary>
    /// Routes an event based on metadata value.
    /// </summary>
    public EventRoutingBuilder RouteByMetadata(
        string eventType,
        string handlerName,
        string metadataKey,
        object expectedValue)
    {
        return RouteEventIf(eventType, handlerName, metadata =>
        {
            if (metadata.TryGetValue(metadataKey, out var value))
            {
                return Equals(value, expectedValue);
            }

            return false;
        });
    }

    /// <summary>
    /// Builds the configuration.
    /// </summary>
    public EventRoutingConfiguration Build()
    {
        return _configuration;
    }
}
