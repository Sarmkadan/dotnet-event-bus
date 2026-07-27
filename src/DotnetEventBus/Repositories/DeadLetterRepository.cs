#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotnetEventBus.Models;

namespace DotnetEventBus.Repositories;

/// <summary>
/// Repository for managing dead letter queue entries with specialized queries.
/// </summary>
public interface IDeadLetterRepository : IRepository<DeadLetterEntry>
{
    /// <summary>
    /// Gets all pending dead letter entries.
    /// </summary>
    Task<IEnumerable<DeadLetterEntry>> GetPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dead letter entries for a specific handler.
    /// </summary>
    Task<IEnumerable<DeadLetterEntry>> GetByHandlerAsync(string handlerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dead letter entries for a specific event type.
    /// </summary>
    Task<IEnumerable<DeadLetterEntry>> GetByEventTypeAsync(string eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dead letter entries with a specific status.
    /// </summary>
    Task<IEnumerable<DeadLetterEntry>> GetByStatusAsync(DeadLetterStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dead letter entries created within a time range.
    /// </summary>
    Task<IEnumerable<DeadLetterEntry>> GetByTimeRangeAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of entries with a specific status.
    /// </summary>
    Task<int> CountByStatusAsync(DeadLetterStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entry counts grouped by event type without loading full entry objects.
    /// </summary>
    Task<Dictionary<string, int>> GetCountsByEventTypeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entry counts grouped by handler name without loading full entry objects.
    /// </summary>
    Task<Dictionary<string, int>> GetCountsByHandlerAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives old dead letter entries.
    /// </summary>
    Task<int> ArchiveOldEntriesAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of the dead letter repository. Behaves as a bounded ring
/// buffer: once <see cref="_maxEntries"/> entries are stored, adding a new one evicts the
/// oldest entry (by insertion order) to make room, so a runaway stream of failing events
/// cannot grow the store without bound.
/// </summary>
public sealed class InMemoryDeadLetterRepository : InMemoryRepository<DeadLetterEntry>, IDeadLetterRepository
{
    private readonly int _maxEntries;
    private readonly ConcurrentQueue<string> _insertionOrder = new();
    private readonly SemaphoreSlim _evictionLock = new(1, 1);

    /// <summary>
    /// Initializes a new in-memory dead letter repository.
    /// </summary>
    /// <param name="maxEntries">
    /// Maximum number of entries retained before the oldest entry is evicted to make room
    /// for a new one. Must be at least 1.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxEntries"/> is less than 1.</exception>
    public InMemoryDeadLetterRepository(int maxEntries = 1000)
    {
        if (maxEntries < 1)
            throw new ArgumentOutOfRangeException(nameof(maxEntries), maxEntries, "maxEntries must be at least 1");

        _maxEntries = maxEntries;
    }

    /// <summary>
    /// Adds a dead letter entry, evicting the oldest stored entry first if the store is
    /// already at capacity.
    /// </summary>
    /// <param name="entity">The dead letter entry to add.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The added entry.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is null.</exception>
    public override async Task<DeadLetterEntry> AddAsync(DeadLetterEntry entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await _evictionLock.WaitAsync(cancellationToken);
        try
        {
            if (await CountAsync(cancellationToken) >= _maxEntries && _insertionOrder.TryDequeue(out var oldestId))
                await DeleteAsync(oldestId, cancellationToken);

            var added = await base.AddAsync(entity, cancellationToken);
            _insertionOrder.Enqueue(entity.Id);
            return added;
        }
        finally
        {
            _evictionLock.Release();
        }
    }

    public async Task<IEnumerable<DeadLetterEntry>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await GetByStatusAsync(DeadLetterStatus.Pending, cancellationToken);
    }

    public async Task<IEnumerable<DeadLetterEntry>> GetByHandlerAsync(string handlerName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(handlerName))
            throw new ArgumentException("Handler name cannot be empty", nameof(handlerName));

        var entries = await GetAllAsync(cancellationToken);
        return entries.Where(e => e.FailedHandlerName == handlerName).ToList();
    }

    public async Task<IEnumerable<DeadLetterEntry>> GetByEventTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty", nameof(eventType));

        var entries = await GetAllAsync(cancellationToken);
        return entries.Where(e => e.Message.EventType == eventType).ToList();
    }

    public async Task<IEnumerable<DeadLetterEntry>> GetByStatusAsync(DeadLetterStatus status, CancellationToken cancellationToken = default)
    {
        var entries = await GetAllAsync(cancellationToken);
        return entries.Where(e => e.Status == status).ToList();
    }

    public async Task<IEnumerable<DeadLetterEntry>> GetByTimeRangeAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        if (endUtc < startUtc)
            throw new ArgumentException("End time must be after start time");

        var entries = await GetAllAsync(cancellationToken);
        return entries
            .Where(e => e.CreatedAtUtc >= startUtc && e.CreatedAtUtc <= endUtc)
            .ToList();
    }

    public async Task<int> CountByStatusAsync(DeadLetterStatus status, CancellationToken cancellationToken = default)
    {
        var entries = await GetByStatusAsync(status, cancellationToken);
        return entries.Count();
    }

    public async Task<Dictionary<string, int>> GetCountsByEventTypeAsync(CancellationToken cancellationToken = default)
    {
        var entries = await GetAllAsync(cancellationToken);
        return entries
            .GroupBy(e => e.Message.EventType)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<Dictionary<string, int>> GetCountsByHandlerAsync(CancellationToken cancellationToken = default)
    {
        var entries = await GetAllAsync(cancellationToken);
        return entries
            .GroupBy(e => e.FailedHandlerName)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<int> ArchiveOldEntriesAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
    {
        if (retentionPeriod <= TimeSpan.Zero)
            throw new ArgumentException("Retention period must be greater than zero", nameof(retentionPeriod));

        var cutoffTime = DateTime.UtcNow.Subtract(retentionPeriod);
        var allEntries = await GetAllAsync(cancellationToken);

        var entriesToArchive = allEntries
            .Where(e => e.CreatedAtUtc < cutoffTime && e.Status != DeadLetterStatus.Archived)
            .ToList();

        int archivedCount = 0;
        foreach (var entry in entriesToArchive)
        {
            if (entry.Status != DeadLetterStatus.Archived)
            {
                entry.Status = DeadLetterStatus.Archived;
                entry.StatusReason = "Auto-archived due to retention period";
                await UpdateAsync(entry, cancellationToken);
                archivedCount++;
            }
        }

        return archivedCount;
    }
}
