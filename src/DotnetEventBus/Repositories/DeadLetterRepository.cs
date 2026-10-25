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

    /// <summary>
    /// Gets all pending dead letter entries.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The entries whose status is <see cref="DeadLetterStatus.Pending"/>.</returns>
    public async Task<IEnumerable<DeadLetterEntry>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await GetByStatusAsync(DeadLetterStatus.Pending, cancellationToken);
    }

    /// <summary>
    /// Gets dead letter entries for a specific handler.
    /// </summary>
    /// <param name="handlerName">The name of the handler whose failed entries should be returned.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The entries whose failed handler name matches <paramref name="handlerName"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handlerName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="handlerName"/> is empty.</exception>
    public async Task<IEnumerable<DeadLetterEntry>> GetByHandlerAsync(string handlerName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(handlerName);

        var entries = await GetAllAsync(cancellationToken);
        return entries.Where(e => e.FailedHandlerName == handlerName).ToList();
    }

    /// <summary>
    /// Gets dead letter entries for a specific event type.
    /// </summary>
    /// <param name="eventType">The event type name to match against the entry's message.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The entries whose message event type matches <paramref name="eventType"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="eventType"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="eventType"/> is empty.</exception>
    public async Task<IEnumerable<DeadLetterEntry>> GetByEventTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventType);

        var entries = await GetAllAsync(cancellationToken);
        return entries.Where(e => e.Message.EventType == eventType).ToList();
    }

    /// <summary>
    /// Gets dead letter entries with a specific status.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The entries whose status equals <paramref name="status"/>.</returns>
    public async Task<IEnumerable<DeadLetterEntry>> GetByStatusAsync(DeadLetterStatus status, CancellationToken cancellationToken = default)
    {
        var entries = await GetAllAsync(cancellationToken);
        return entries.Where(e => e.Status == status).ToList();
    }

    /// <summary>
    /// Gets dead letter entries created within a time range.
    /// </summary>
    /// <param name="startUtc">The inclusive start of the range, in UTC.</param>
    /// <param name="endUtc">The inclusive end of the range, in UTC.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The entries whose creation time falls within the range.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="endUtc"/> is earlier than <paramref name="startUtc"/>.</exception>
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

    /// <summary>
    /// Gets the count of entries with a specific status.
    /// </summary>
    /// <param name="status">The status to count.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The number of entries whose status equals <paramref name="status"/>.</returns>
    public async Task<int> CountByStatusAsync(DeadLetterStatus status, CancellationToken cancellationToken = default)
    {
        var entries = await GetByStatusAsync(status, cancellationToken);
        return entries.Count();
    }

    /// <summary>
    /// Gets entry counts grouped by event type without loading full entry objects.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A dictionary mapping each event type to the number of entries with that type.</returns>
    public async Task<Dictionary<string, int>> GetCountsByEventTypeAsync(CancellationToken cancellationToken = default)
    {
        var entries = await GetAllAsync(cancellationToken);
        return entries
            .GroupBy(e => e.Message.EventType)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Gets entry counts grouped by handler name without loading full entry objects.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A dictionary mapping each failed handler name to the number of entries for that handler.</returns>
    public async Task<Dictionary<string, int>> GetCountsByHandlerAsync(CancellationToken cancellationToken = default)
    {
        var entries = await GetAllAsync(cancellationToken);
        return entries
            .GroupBy(e => e.FailedHandlerName)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Archives old dead letter entries.
    /// </summary>
    /// <param name="retentionPeriod">Entries created longer ago than this period are archived. Must be greater than zero.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The number of entries that were newly archived.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="retentionPeriod"/> is zero or negative.</exception>
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
