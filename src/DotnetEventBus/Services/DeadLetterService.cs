#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetEventBus.Models;
using DotnetEventBus.Repositories;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Services;

/// <summary>
/// Service for managing dead letter queue operations.
/// </summary>
public interface IDeadLetterService
{
    /// <summary>
    /// Gets all pending dead letter entries.
    /// </summary>
    Task<IEnumerable<DeadLetterEntry>> GetPendingEntriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dead letter entries for a specific event type.
    /// </summary>
    Task<IEnumerable<DeadLetterEntry>> GetEntriesByEventTypeAsync(string eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dead letter entries for a specific handler.
    /// </summary>
    Task<IEnumerable<DeadLetterEntry>> GetEntriesByHandlerAsync(string handlerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to reprocess a dead letter entry.
    /// </summary>
    Task<bool> ReprocessEntryAsync(string entryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a dead letter entry as reviewed but not reprocessed.
    /// </summary>
    Task MarkAsReviewedAsync(string entryId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives old dead letter entries.
    /// </summary>
    Task<int> ArchiveOldEntriesAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about the dead letter queue.
    /// </summary>
    Task<DeadLetterStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Purges all dead letter entries.
    /// </summary>
    Task PurgeAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics about the dead letter queue.
/// </summary>
public sealed class DeadLetterStatistics
{
    public int TotalEntries { get; set; }
    public int PendingEntries { get; set; }
    public int ReviewedNotProcessedEntries { get; set; }
    public int ReprocessedEntries { get; set; }
    public int ReprocessFailedEntries { get; set; }
    public int ArchivedEntries { get; set; }
    public Dictionary<string, int> EntriesByEventType { get; set; } = new();
    public Dictionary<string, int> EntriesByHandler { get; set; } = new();
}

/// <summary>
/// Default implementation of the dead letter service.
/// </summary>
public sealed class DeadLetterService : IDeadLetterService
{
    private readonly IDeadLetterRepository _repository;
    private readonly IEventBus? _eventBus;
    private readonly ILogger<DeadLetterService>? _logger;

    public DeadLetterService(
        IDeadLetterRepository repository,
        IEventBus? eventBus = null,
        ILogger<DeadLetterService>? logger = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<IEnumerable<DeadLetterEntry>> GetPendingEntriesAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetPendingAsync(cancellationToken);
    }

    public async Task<IEnumerable<DeadLetterEntry>> GetEntriesByEventTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty", nameof(eventType));

        return await _repository.GetByEventTypeAsync(eventType, cancellationToken);
    }

    public async Task<IEnumerable<DeadLetterEntry>> GetEntriesByHandlerAsync(string handlerName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(handlerName))
            throw new ArgumentException("Handler name cannot be empty", nameof(handlerName));

        return await _repository.GetByHandlerAsync(handlerName, cancellationToken);
    }

    public async Task<bool> ReprocessEntryAsync(string entryId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entryId))
            throw new ArgumentException("Entry ID cannot be empty", nameof(entryId));

        var entry = await _repository.GetByIdAsync(entryId, cancellationToken);
        if (entry is null)
        {
            _logger?.LogWarning("Dead letter entry {EntryId} not found", entryId);
            return false;
        }

        if (_eventBus is null)
        {
            _logger?.LogError("Event bus not available for reprocessing dead letter entry {EntryId}", entryId);
            entry.MarkAsReprocessFailed("Event bus not available");
            await _repository.UpdateAsync(entry, cancellationToken);
            return false;
        }

        try
        {
            var result = await _eventBus.PublishAsync(
                entry.Message.Payload,
                Type.GetType(entry.Message.EventType) ?? typeof(object),
                entry.Message.CorrelationId,
                cancellationToken);

            if (result.Success)
            {
                entry.MarkAsReprocessed();
                _logger?.LogInformation("Successfully reprocessed dead letter entry {EntryId}", entryId);
            }
            else
            {
                entry.MarkAsReprocessFailed($"Reprocessing failed with {result.FailedHandlers} handler failures");
                _logger?.LogWarning("Failed to reprocess dead letter entry {EntryId}", entryId);
            }

            await _repository.UpdateAsync(entry, cancellationToken);
            return result.Success;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception reprocessing dead letter entry {EntryId}", entryId);
            entry.MarkAsReprocessFailed(ex.Message);
            await _repository.UpdateAsync(entry, cancellationToken);
            return false;
        }
    }

    public async Task MarkAsReviewedAsync(string entryId, string? reason = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entryId))
            throw new ArgumentException("Entry ID cannot be empty", nameof(entryId));

        var entry = await _repository.GetByIdAsync(entryId, cancellationToken);
        if (entry is null)
            throw new InvalidOperationException($"Dead letter entry '{entryId}' not found");

        entry.MarkAsReviewed(reason);
        await _repository.UpdateAsync(entry, cancellationToken);

        _logger?.LogInformation(
            "Marked dead letter entry {EntryId} as reviewed. Reason: {Reason}",
            entryId,
            reason ?? "None provided");
    }

    public async Task<int> ArchiveOldEntriesAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
    {
        if (retentionPeriod <= TimeSpan.Zero)
            throw new ArgumentException("Retention period must be greater than zero", nameof(retentionPeriod));

        var archivedCount = await _repository.ArchiveOldEntriesAsync(retentionPeriod, cancellationToken);

        _logger?.LogInformation(
            "Archived {ArchivedCount} dead letter entries older than {RetentionPeriodDays} days",
            archivedCount,
            retentionPeriod.TotalDays);

        return archivedCount;
    }

    public async Task<DeadLetterStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var allEntries = await _repository.GetAllAsync(cancellationToken);
        var entriesList = allEntries.ToList();

        var stats = new DeadLetterStatistics
        {
            TotalEntries = entriesList.Count,
            PendingEntries = await _repository.CountByStatusAsync(DeadLetterStatus.Pending, cancellationToken),
            ReviewedNotProcessedEntries = await _repository.CountByStatusAsync(DeadLetterStatus.ReviewedNotProcessed, cancellationToken),
            ReprocessedEntries = await _repository.CountByStatusAsync(DeadLetterStatus.Reprocessed, cancellationToken),
            ReprocessFailedEntries = await _repository.CountByStatusAsync(DeadLetterStatus.ReprocessFailed, cancellationToken),
            ArchivedEntries = await _repository.CountByStatusAsync(DeadLetterStatus.Archived, cancellationToken)
        };

        foreach (var entry in entriesList)
        {
            var eventType = entry.Message.EventType;
            if (!stats.EntriesByEventType.ContainsKey(eventType))
                stats.EntriesByEventType[eventType] = 0;
            stats.EntriesByEventType[eventType]++;

            var handler = entry.FailedHandlerName;
            if (!stats.EntriesByHandler.ContainsKey(handler))
                stats.EntriesByHandler[handler] = 0;
            stats.EntriesByHandler[handler]++;
        }

        return stats;
    }

    public async Task PurgeAsync(CancellationToken cancellationToken = default)
    {
        await _repository.ClearAsync(cancellationToken);
        _logger?.LogWarning("Purged all dead letter entries");
    }
}
