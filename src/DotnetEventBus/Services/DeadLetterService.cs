#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetEventBus.Models;
using DotnetEventBus.Repositories;
using Microsoft.Extensions.DependencyInjection;
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
    /// Reprocesses all pending dead letter entries with the given event type.
    /// Processing continues even if individual entries fail.
    /// </summary>
    /// <param name="eventType">Event type to reprocess.</param>
    /// <param name="maxEntries">Optional upper limit on how many entries to reprocess in one call.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<BatchReprocessResult> ReprocessByEventTypeAsync(
        string eventType,
        int? maxEntries = null,
        CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Adds a new dead letter entry, typically for messages that failed deserialization.
    /// </summary>
    Task AddDeadLetterEntryAsync(
        string eventType,
        string rawPayload,
        Exception exception,
        string? correlationId = null,
        string? handlerName = null,
        CancellationToken cancellationToken = default);
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
/// Result of a batch reprocess operation.
/// </summary>
public sealed class BatchReprocessResult
{
    /// <summary>Number of entries that were successfully reprocessed.</summary>
    public int SucceededCount { get; set; }

    /// <summary>Number of entries that failed reprocessing.</summary>
    public int FailedCount { get; set; }

    /// <summary>Total entries attempted.</summary>
    public int TotalAttempted => SucceededCount + FailedCount;

    /// <summary>IDs of entries that failed reprocessing.</summary>
    public List<string> FailedEntryIds { get; set; } = [];

    public bool AllSucceeded => FailedCount == 0;
}

/// <summary>
/// Default implementation of the dead letter service.
/// </summary>
public sealed class DeadLetterService : IDeadLetterService
{
    private readonly IDeadLetterRepository _repository;
    private readonly IEventBus? _eventBusInstance;
    private readonly IServiceProvider? _serviceProvider;
    private readonly ILogger<DeadLetterService>? _logger;

    // Resolved lazily (never at construction time): DeadLetterService and EventBus are
    // mutually dependent when wired through DI (AddEventBus registers EventBus with a
    // factory that resolves IDeadLetterService, which historically resolved IEventBus back
    // via its constructor). Requesting IEventBus eagerly here re-enters the same singleton
    // resolution that is still in progress and deadlocks inside the container's singleton
    // lock. Resolving through IServiceProvider on first use instead defers the lookup until
    // the EventBus singleton has already finished constructing.
    private IEventBus? _eventBus => _eventBusInstance ?? _serviceProvider?.GetService<IEventBus>();

    public DeadLetterService(
        IDeadLetterRepository repository,
        IEventBus? eventBus = null,
        ILogger<DeadLetterService>? logger = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventBusInstance = eventBus;
        _logger = logger;
    }

    public DeadLetterService(
        IDeadLetterRepository repository,
        IServiceProvider serviceProvider,
        ILogger<DeadLetterService>? logger = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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

    public async Task<BatchReprocessResult> ReprocessByEventTypeAsync(
        string eventType,
        int? maxEntries = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty", nameof(eventType));

        var entries = await _repository.GetByEventTypeAsync(eventType, cancellationToken);
        var pending = entries
            .Where(e => e.Status == DeadLetterStatus.Pending)
            .ToList();

        if (maxEntries.HasValue)
            pending = pending.Take(maxEntries.Value).ToList();

        var batchResult = new BatchReprocessResult();

        _logger?.LogInformation(
            "Starting batch reprocess for event type {EventType}: {Count} entries",
            eventType, pending.Count);

        foreach (var entry in pending)
        {
            var succeeded = await ReprocessEntryAsync(entry.Id, cancellationToken);
            if (succeeded)
                batchResult.SucceededCount++;
            else
            {
                batchResult.FailedCount++;
                batchResult.FailedEntryIds.Add(entry.Id);
            }
        }

        _logger?.LogInformation(
            "Batch reprocess for {EventType} complete: {Succeeded} succeeded, {Failed} failed",
            eventType, batchResult.SucceededCount, batchResult.FailedCount);

        return batchResult;
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
        var pendingCount = await _repository.CountByStatusAsync(DeadLetterStatus.Pending, cancellationToken);
        var reviewedCount = await _repository.CountByStatusAsync(DeadLetterStatus.ReviewedNotProcessed, cancellationToken);
        var reprocessedCount = await _repository.CountByStatusAsync(DeadLetterStatus.Reprocessed, cancellationToken);
        var reprocessFailedCount = await _repository.CountByStatusAsync(DeadLetterStatus.ReprocessFailed, cancellationToken);
        var archivedCount = await _repository.CountByStatusAsync(DeadLetterStatus.Archived, cancellationToken);

        var stats = new DeadLetterStatistics
        {
            TotalEntries = pendingCount + reviewedCount + reprocessedCount + reprocessFailedCount + archivedCount,
            PendingEntries = pendingCount,
            ReviewedNotProcessedEntries = reviewedCount,
            ReprocessedEntries = reprocessedCount,
            ReprocessFailedEntries = reprocessFailedCount,
            ArchivedEntries = archivedCount,
            EntriesByEventType = await _repository.GetCountsByEventTypeAsync(cancellationToken),
            EntriesByHandler = await _repository.GetCountsByHandlerAsync(cancellationToken)
        };

        return stats;
    }

    public async Task PurgeAsync(CancellationToken cancellationToken = default)
    {
        await _repository.ClearAsync(cancellationToken);
        _logger?.LogWarning("Purged all dead letter entries");
    }

    public async Task AddDeadLetterEntryAsync(
        string eventType,
        string rawPayload,
        Exception exception,
        string? correlationId = null,
        string? handlerName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty", nameof(eventType));
        if (string.IsNullOrWhiteSpace(rawPayload))
            throw new ArgumentException("Raw payload cannot be empty", nameof(rawPayload));
        if (exception is null)
            throw new ArgumentNullException(nameof(exception));

        var message = new EventMessage(eventType, rawPayload)
        {
            CorrelationId = correlationId,
            Scope = MessageScope.Distributed, // Mark as distributed as it's typically from external source
            CreatedAtUtc = DateTime.UtcNow
        };

        var deadLetterEntry = new DeadLetterEntry(
            message,
            handlerName ?? "DeserializationFailed", // Use a generic name for deserialization failures
            exception,
            0 // No retries for deserialization failures as they are structural
        );

        await _repository.AddAsync(deadLetterEntry, cancellationToken);

        _logger?.LogError(
            exception,
            "Raw distributed event of type {EventType} failed deserialization and was added to dead letter queue. CorrelationId: {CorrelationId}",
            eventType,
            correlationId);
    }
}
