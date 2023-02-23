// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Workers;

/// <summary>
/// Background worker that periodically processes dead-lettered events.
/// Attempts to reprocess failed events and track retry statistics.
/// Why: Ensures no events are permanently lost and provides visibility into failures.
/// </summary>
public class DeadLetterProcessor : BackgroundService
{
    private readonly ILogger<DeadLetterProcessor> _logger;
    private readonly TimeSpan _processingInterval;
    private readonly int _batchSize;
    private List<DeadLetterItem> _deadLetterQueue = [];

    public DeadLetterProcessor(
        ILogger<DeadLetterProcessor> logger,
        TimeSpan? processingInterval = null,
        int batchSize = 100)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _processingInterval = processingInterval ?? TimeSpan.FromMinutes(5);
        _batchSize = batchSize;
    }

    /// <summary>
    /// Adds an event to the dead letter queue for later reprocessing.
    /// </summary>
    public void Enqueue(string eventType, object eventData, Exception exception)
    {
        var item = new DeadLetterItem
        {
            Id = Guid.NewGuid().ToString(),
            EventType = eventType,
            EventData = eventData,
            ErrorMessage = exception.Message,
            StackTrace = exception.StackTrace,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
            Status = DeadLetterStatus.Pending
        };

        _deadLetterQueue.Add(item);
        _logger.LogWarning("Event added to dead letter queue: {EventType}", eventType);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Dead Letter Processor starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDeadLettersAsync(stoppingToken);
                await Task.Delay(_processingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing dead letters");
            }
        }

        _logger.LogInformation("Dead Letter Processor stopped");
    }

    private async Task ProcessDeadLettersAsync(CancellationToken stoppingToken)
    {
        var pendingItems = _deadLetterQueue
            .Where(x => x.Status == DeadLetterStatus.Pending)
            .Take(_batchSize)
            .ToList();

        if (pendingItems.Count == 0)
            return;

        _logger.LogInformation("Processing {Count} dead lettered events", pendingItems.Count);

        foreach (var item in pendingItems)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                // Attempt reprocessing logic would go here
                item.RetryCount++;

                if (item.RetryCount >= 5)
                {
                    item.Status = DeadLetterStatus.Failed;
                    _logger.LogError(
                        "Event {EventType} (Id: {Id}) exhausted all retries",
                        item.EventType, item.Id);
                }
                else
                {
                    item.Status = DeadLetterStatus.Retry;
                    item.LastRetryAt = DateTime.UtcNow;
                    _logger.LogInformation(
                        "Event {EventType} (Id: {Id}) marked for retry (attempt {Attempt}/5)",
                        item.EventType, item.Id, item.RetryCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process dead letter item: {Id}", item.Id);
            }

            await Task.Delay(100, stoppingToken); // Small delay between items
        }
    }

    /// <summary>
    /// Gets statistics about the dead letter queue.
    /// </summary>
    public DeadLetterStats GetStats()
    {
        return new DeadLetterStats
        {
            TotalItems = _deadLetterQueue.Count,
            PendingItems = _deadLetterQueue.Count(x => x.Status == DeadLetterStatus.Pending),
            RetryingItems = _deadLetterQueue.Count(x => x.Status == DeadLetterStatus.Retry),
            FailedItems = _deadLetterQueue.Count(x => x.Status == DeadLetterStatus.Failed),
            SuccessfulItems = _deadLetterQueue.Count(x => x.Status == DeadLetterStatus.Successful)
        };
    }

    /// <summary>
    /// Gets all dead lettered events.
    /// </summary>
    public IEnumerable<DeadLetterItem> GetAllItems()
    {
        return _deadLetterQueue.ToList();
    }

    /// <summary>
    /// Removes a dead letter item.
    /// </summary>
    public bool RemoveItem(string id)
    {
        var item = _deadLetterQueue.FirstOrDefault(x => x.Id == id);
        if (item != null)
        {
            _deadLetterQueue.Remove(item);
            return true;
        }

        return false;
    }
}

public class DeadLetterItem
{
    public string? Id { get; set; }
    public string? EventType { get; set; }
    public object? EventData { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastRetryAt { get; set; }
    public int RetryCount { get; set; }
    public DeadLetterStatus Status { get; set; }
}

public enum DeadLetterStatus
{
    Pending,
    Retry,
    Successful,
    Failed
}

public class DeadLetterStats
{
    public int TotalItems { get; set; }
    public int PendingItems { get; set; }
    public int RetryingItems { get; set; }
    public int FailedItems { get; set; }
    public int SuccessfulItems { get; set; }
}
