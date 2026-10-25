#nullable enable

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
public sealed class InMemorySubscriptionRepository : InMemoryRepository<Subscription>, ISubscriptionRepository
{
    /// <summary>
    /// Gets all subscriptions for a specific event type.
    /// </summary>
    /// <param name="eventType">The event type name to match. Cannot be null or empty.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Subscriptions whose event type equals <paramref name="eventType"/>, active or not.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="eventType"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="eventType"/> is empty.</exception>
    public async Task<IEnumerable<Subscription>> GetByEventTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventType);

        var subscriptions = await GetAllAsync(cancellationToken);
        return subscriptions.Where(s => s.EventType == eventType).ToList();
    }

    /// <summary>
    /// Gets all active subscriptions for a specific event type.
    /// </summary>
    /// <param name="eventType">The event type name to match. Cannot be null or empty.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Active subscriptions whose event type equals <paramref name="eventType"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="eventType"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="eventType"/> is empty.</exception>
    public async Task<IEnumerable<Subscription>> GetActiveByEventTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventType);

        var subscriptions = await GetAllAsync(cancellationToken);
        return subscriptions.Where(s => s.EventType == eventType && s.IsActive).ToList();
    }

    /// <summary>
    /// Gets subscriptions by handler name.
    /// </summary>
    /// <param name="handlerName">The handler name to match. Cannot be null or empty.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Subscriptions whose handler name equals <paramref name="handlerName"/>, active or not.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="handlerName"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="handlerName"/> is empty.</exception>
    public async Task<IEnumerable<Subscription>> GetByHandlerNameAsync(string handlerName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(handlerName);

        var subscriptions = await GetAllAsync(cancellationToken);
        return subscriptions.Where(s => s.HandlerName == handlerName).ToList();
    }

    /// <summary>
    /// Gets all active subscriptions.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>All subscriptions whose <see cref="Subscription.IsActive"/> flag is set.</returns>
    public async Task<IEnumerable<Subscription>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var subscriptions = await GetAllAsync(cancellationToken);
        return subscriptions.Where(s => s.IsActive).ToList();
    }

    /// <summary>
    /// Gets all inactive/disabled subscriptions.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>All subscriptions whose <see cref="Subscription.IsActive"/> flag is not set.</returns>
    public async Task<IEnumerable<Subscription>> GetAllInactiveAsync(CancellationToken cancellationToken = default)
    {
        var subscriptions = await GetAllAsync(cancellationToken);
        return subscriptions.Where(s => !s.IsActive).ToList();
    }

    /// <summary>
    /// Gets subscriptions ordered by priority (highest first) for a given event type.
    /// </summary>
    /// <param name="eventType">The event type name to match. Cannot be null or empty.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Subscriptions for <paramref name="eventType"/> sorted by descending priority.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="eventType"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="eventType"/> is empty.</exception>
    public async Task<IEnumerable<Subscription>> GetByEventTypeOrderedByPriorityAsync(
        string eventType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventType);

        var subscriptions = await GetByEventTypeAsync(eventType, cancellationToken);
        return subscriptions.OrderByDescending(s => s.Priority).ToList();
    }

    /// <summary>
    /// Counts subscriptions for a specific event type.
    /// </summary>
    /// <param name="eventType">The event type name to match. Cannot be null or empty.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of subscriptions for <paramref name="eventType"/>, active or not.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="eventType"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="eventType"/> is empty.</exception>
    public async Task<int> CountByEventTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventType);

        var subscriptions = await GetByEventTypeAsync(eventType, cancellationToken);
        return subscriptions.Count();
    }

    /// <summary>
    /// Disables all subscriptions for a specific handler.
    /// </summary>
    /// <param name="handlerName">The handler name whose subscriptions should be disabled. Cannot be null or empty.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes once every matching subscription has been disabled and updated.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="handlerName"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="handlerName"/> is empty.</exception>
    public async Task DisableHandlerAsync(string handlerName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(handlerName);

        var subscriptions = await GetByHandlerNameAsync(handlerName, cancellationToken);
        foreach (var sub in subscriptions)
        {
            sub.Disable();
            await UpdateAsync(sub, cancellationToken);
        }
    }

    /// <summary>
    /// Enables all subscriptions for a specific handler.
    /// </summary>
    /// <param name="handlerName">The handler name whose subscriptions should be enabled. Cannot be null, empty, or whitespace.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes once every matching subscription has been enabled and updated.</returns>
    /// <exception cref="ArgumentException"><paramref name="handlerName"/> is null, empty, or consists only of white-space characters.</exception>
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
