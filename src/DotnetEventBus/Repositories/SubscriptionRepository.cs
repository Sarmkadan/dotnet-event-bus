// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetEventBus.Models;

namespace DotnetEventBus.Repositories;

/// <summary>
/// Repository for managing event subscriptions with specialized queries.
/// </summary>
public interface ISubscriptionRepository : IRepository<Subscription>
{
    /// <summary>
    /// Gets all subscriptions for a specific event type.
    /// </summary>
    Task<IEnumerable<Subscription>> GetByEventTypeAsync(string eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active subscriptions for a specific event type.
    /// </summary>
    Task<IEnumerable<Subscription>> GetActiveByEventTypeAsync(string eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets subscriptions by handler name.
    /// </summary>
    Task<IEnumerable<Subscription>> GetByHandlerNameAsync(string handlerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active subscriptions.
    /// </summary>
    Task<IEnumerable<Subscription>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all inactive/disabled subscriptions.
    /// </summary>
    Task<IEnumerable<Subscription>> GetAllInactiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets subscriptions ordered by priority (highest first) for a given event type.
    /// </summary>
    Task<IEnumerable<Subscription>> GetByEventTypeOrderedByPriorityAsync(string eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts subscriptions for a specific event type.
    /// </summary>
    Task<int> CountByEventTypeAsync(string eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables all subscriptions for a specific handler.
    /// </summary>
    Task DisableHandlerAsync(string handlerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables all subscriptions for a specific handler.
    /// </summary>
    Task EnableHandlerAsync(string handlerName, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of the subscription repository.
/// </summary>
public class InMemorySubscriptionRepository : InMemoryRepository<Subscription>, ISubscriptionRepository
{
    public async Task<IEnumerable<Subscription>> GetByEventTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty", nameof(eventType));

        var subscriptions = await GetAllAsync(cancellationToken);
        return subscriptions.Where(s => s.EventType == eventType).ToList();
    }

    public async Task<IEnumerable<Subscription>> GetActiveByEventTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty", nameof(eventType));

        var subscriptions = await GetAllAsync(cancellationToken);
        return subscriptions.Where(s => s.EventType == eventType && s.IsActive).ToList();
    }

    public async Task<IEnumerable<Subscription>> GetByHandlerNameAsync(string handlerName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(handlerName))
            throw new ArgumentException("Handler name cannot be empty", nameof(handlerName));

        var subscriptions = await GetAllAsync(cancellationToken);
        return subscriptions.Where(s => s.HandlerName == handlerName).ToList();
    }

    public async Task<IEnumerable<Subscription>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var subscriptions = await GetAllAsync(cancellationToken);
        return subscriptions.Where(s => s.IsActive).ToList();
    }

    public async Task<IEnumerable<Subscription>> GetAllInactiveAsync(CancellationToken cancellationToken = default)
    {
        var subscriptions = await GetAllAsync(cancellationToken);
        return subscriptions.Where(s => !s.IsActive).ToList();
    }

    public async Task<IEnumerable<Subscription>> GetByEventTypeOrderedByPriorityAsync(
        string eventType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty", nameof(eventType));

        var subscriptions = await GetByEventTypeAsync(eventType, cancellationToken);
        return subscriptions.OrderByDescending(s => s.Priority).ToList();
    }

    public async Task<int> CountByEventTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty", nameof(eventType));

        var subscriptions = await GetByEventTypeAsync(eventType, cancellationToken);
        return subscriptions.Count();
    }

    public async Task DisableHandlerAsync(string handlerName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(handlerName))
            throw new ArgumentException("Handler name cannot be empty", nameof(handlerName));

        var subscriptions = await GetByHandlerNameAsync(handlerName, cancellationToken);
        foreach (var sub in subscriptions)
        {
            sub.Disable();
            await UpdateAsync(sub, cancellationToken);
        }
    }

    public async Task EnableHandlerAsync(string handlerName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(handlerName))
            throw new ArgumentException("Handler name cannot be empty", nameof(handlerName));

        var subscriptions = await GetByHandlerNameAsync(handlerName, cancellationToken);
        foreach (var sub in subscriptions)
        {
            sub.Enable();
            await UpdateAsync(sub, cancellationToken);
        }
    }
}
