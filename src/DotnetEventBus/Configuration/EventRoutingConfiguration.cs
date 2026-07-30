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
public sealed class EventRoutingConfiguration : IEquatable<EventRoutingConfiguration>
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

    #region Equality members

    public bool Equals(EventRoutingConfiguration? other)
    {
        if (ReferenceEquals(this, other))
            return true;
        if (other is null)
            return false;

        if (_routes.Count != other._routes.Count)
            return false;

        // Compare each event type and its routing rules
        foreach (var kvp in _routes)
        {
            if (!other._routes.TryGetValue(kvp.Key, out var otherRules))
                return false;

            var thisRules = kvp.Value;
            if (thisRules.Count != otherRules.Count)
                return false;

            // Compare rules in order; order is significant for priority handling
            for (int i = 0; i < thisRules.Count; i++)
            {
                var a = thisRules[i];
                var b = otherRules[i];

                if (!RoutingRuleEquals(a, b))
                    return false;
            }
        }

        return true;
    }

    private static bool RoutingRuleEquals(RoutingRule a, RoutingRule b)
    {
        if (ReferenceEquals(a, b))
            return true;
        if (a is null || b is null)
            return false;

        return string.Equals(a.TargetHandler, b.TargetHandler, StringComparison.Ordinal) &&
               Equals(a.Condition, b.Condition) && // delegate equality (reference)
               a.Priority == b.Priority &&
               a.ContinueEvaluation == b.ContinueEvaluation;
    }

    public override bool Equals(object? obj) => Equals(obj as EventRoutingConfiguration);

    public override int GetHashCode()
    {
        var hash = new HashCode();

        // Ensure deterministic order
        foreach (var kvp in _routes.OrderBy(k => k.Key))
        {
            hash.Add(kvp.Key);
            foreach (var rule in kvp.Value)
            {
                hash.Add(rule.TargetHandler);
                hash.Add(rule.Condition);
                hash.Add(rule.Priority);
                hash.Add(rule.ContinueEvaluation);
            }
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(EventRoutingConfiguration? left, EventRoutingConfiguration? right)
        => Equals(left, right);

    public static bool operator !=(EventRoutingConfiguration? left, EventRoutingConfiguration? right)
        => !Equals(left, right);

    #endregion
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
