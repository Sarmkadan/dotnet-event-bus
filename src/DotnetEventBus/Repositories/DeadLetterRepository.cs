// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    /// Archives old dead letter entries.
    /// </summary>
    Task<int> ArchiveOldEntriesAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of the dead letter repository.
/// </summary>
public class InMemoryDeadLetterRepository : InMemoryRepository<DeadLetterEntry>, IDeadLetterRepository
{
    private readonly object _queryLock = new();

    public async Task<IEnumerable<DeadLetterEntry>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await GetByStatusAsync(DeadLetterStatus.Pending, cancellationToken);
    }

    public async Task<IEnumerable<DeadLetterEntry>> GetByHandlerAsync(string handlerName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(handlerName))
            throw new ArgumentException("Handler name cannot be empty", nameof(handlerName));

        lock (_queryLock)
        {
            var entries = await GetAllAsync(cancellationToken);
            return entries.Where(e => e.FailedHandlerName == handlerName).ToList();
        }
    }

    public async Task<IEnumerable<DeadLetterEntry>> GetByEventTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty", nameof(eventType));

        lock (_queryLock)
        {
            var entries = await GetAllAsync(cancellationToken);
            return entries.Where(e => e.Message.EventType == eventType).ToList();
        }
    }

    public async Task<IEnumerable<DeadLetterEntry>> GetByStatusAsync(DeadLetterStatus status, CancellationToken cancellationToken = default)
    {
        lock (_queryLock)
        {
            var entries = await GetAllAsync(cancellationToken);
            return entries.Where(e => e.Status == status).ToList();
        }
    }

    public async Task<IEnumerable<DeadLetterEntry>> GetByTimeRangeAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        if (endUtc < startUtc)
            throw new ArgumentException("End time must be after start time");

        lock (_queryLock)
        {
            var entries = await GetAllAsync(cancellationToken);
            return entries
                .Where(e => e.CreatedAtUtc >= startUtc && e.CreatedAtUtc <= endUtc)
                .ToList();
        }
    }

    public async Task<int> CountByStatusAsync(DeadLetterStatus status, CancellationToken cancellationToken = default)
    {
        var entries = await GetByStatusAsync(status, cancellationToken);
        return entries.Count();
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
