// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetEventBus.Models;
using DotnetEventBus.Repositories;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Services;

/// <summary>
/// Service for managing subscriptions with monitoring and control capabilities.
/// </summary>
public interface ISubscriptionManager
{
    /// <summary>
    /// Gets all subscriptions for an event type.
    /// </summary>
    Task<IEnumerable<SubscriptionInfo>> GetSubscriptionsAsync(string eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all subscriptions.
    /// </summary>
    Task<IEnumerable<SubscriptionInfo>> GetAllSubscriptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets subscription count for an event type.
    /// </summary>
    Task<int> GetSubscriptionCountAsync(string eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables all handlers of a specific type.
    /// </summary>
    Task DisableHandlerAsync(string handlerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables all handlers of a specific type.
    /// </summary>
    Task EnableHandlerAsync(string handlerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about subscriptions.
    /// </summary>
    Task<SubscriptionStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about a subscription.
/// </summary>
public class SubscriptionInfo
{
    public string Id { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string HandlerName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int Priority { get; set; }
    public bool IsAsync { get; set; }
    public TimeSpan? Timeout { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Statistics about subscriptions.
/// </summary>
public class SubscriptionStatistics
{
    public int TotalSubscriptions { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int InactiveSubscriptions { get; set; }
    public int UniqueEventTypes { get; set; }
    public int UniqueHandlers { get; set; }
    public Dictionary<string, int> SubscriptionsByEventType { get; set; } = new();
    public Dictionary<string, int> SubscriptionsByHandler { get; set; } = new();
    public Dictionary<string, int> ActiveSubscriptionsByEventType { get; set; } = new();
}

/// <summary>
/// Default implementation of subscription management.
/// </summary>
public class SubscriptionManager : ISubscriptionManager
{
    private readonly ISubscriptionRepository _repository;
    private readonly ILogger<SubscriptionManager>? _logger;

    public SubscriptionManager(
        ISubscriptionRepository repository,
        ILogger<SubscriptionManager>? logger = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger;
    }

    public async Task<IEnumerable<SubscriptionInfo>> GetSubscriptionsAsync(
        string eventType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty", nameof(eventType));

        var subscriptions = await _repository.GetByEventTypeOrderedByPriorityAsync(eventType, cancellationToken);

        return subscriptions.Select(s => new SubscriptionInfo
        {
            Id = s.Id,
            EventType = s.EventType,
            HandlerName = s.HandlerName,
            IsActive = s.IsActive,
            Priority = s.Priority,
            IsAsync = s.IsAsync,
            Timeout = s.Timeout,
            CreatedAtUtc = s.CreatedAtUtc
        }).ToList();
    }

    public async Task<IEnumerable<SubscriptionInfo>> GetAllSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        var subscriptions = await _repository.GetAllAsync(cancellationToken);

        return subscriptions.Select(s => new SubscriptionInfo
        {
            Id = s.Id,
            EventType = s.EventType,
            HandlerName = s.HandlerName,
            IsActive = s.IsActive,
            Priority = s.Priority,
            IsAsync = s.IsAsync,
            Timeout = s.Timeout,
            CreatedAtUtc = s.CreatedAtUtc
        }).ToList();
    }

    public async Task<int> GetSubscriptionCountAsync(string eventType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty", nameof(eventType));

        return await _repository.CountByEventTypeAsync(eventType, cancellationToken);
    }

    public async Task DisableHandlerAsync(string handlerName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(handlerName))
            throw new ArgumentException("Handler name cannot be empty", nameof(handlerName));

        await _repository.DisableHandlerAsync(handlerName, cancellationToken);

        _logger?.LogInformation("Disabled handler {HandlerName}", handlerName);
    }

    public async Task EnableHandlerAsync(string handlerName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(handlerName))
            throw new ArgumentException("Handler name cannot be empty", nameof(handlerName));

        await _repository.EnableHandlerAsync(handlerName, cancellationToken);

        _logger?.LogInformation("Enabled handler {HandlerName}", handlerName);
    }

    public async Task<SubscriptionStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var allSubscriptions = await _repository.GetAllAsync(cancellationToken);
        var subscriptionList = allSubscriptions.ToList();

        var activeSubscriptions = await _repository.GetAllActiveAsync(cancellationToken);
        var activeList = activeSubscriptions.ToList();

        var stats = new SubscriptionStatistics
        {
            TotalSubscriptions = subscriptionList.Count,
            ActiveSubscriptions = activeList.Count,
            InactiveSubscriptions = subscriptionList.Count - activeList.Count,
            UniqueEventTypes = subscriptionList.Select(s => s.EventType).Distinct().Count(),
            UniqueHandlers = subscriptionList.Select(s => s.HandlerName).Distinct().Count()
        };

        // Count by event type
        foreach (var subscription in subscriptionList)
        {
            if (!stats.SubscriptionsByEventType.ContainsKey(subscription.EventType))
                stats.SubscriptionsByEventType[subscription.EventType] = 0;
            stats.SubscriptionsByEventType[subscription.EventType]++;

            if (subscription.IsActive)
            {
                if (!stats.ActiveSubscriptionsByEventType.ContainsKey(subscription.EventType))
                    stats.ActiveSubscriptionsByEventType[subscription.EventType] = 0;
                stats.ActiveSubscriptionsByEventType[subscription.EventType]++;
            }
        }

        // Count by handler
        foreach (var subscription in subscriptionList)
        {
            if (!stats.SubscriptionsByHandler.ContainsKey(subscription.HandlerName))
                stats.SubscriptionsByHandler[subscription.HandlerName] = 0;
            stats.SubscriptionsByHandler[subscription.HandlerName]++;
        }

        return stats;
    }
}
