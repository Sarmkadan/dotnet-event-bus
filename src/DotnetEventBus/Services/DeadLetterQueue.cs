#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotnetEventBus.Models;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Services;

/// <summary>
/// In-memory dead letter queue that stores failed event messages for later inspection and reprocessing.
/// </summary>
public sealed class DeadLetterQueue : IDisposable
{
    private readonly ConcurrentQueue<DeadLetterEntry> _queue = new();
    private readonly ConcurrentDictionary<string, DeadLetterEntry> _index = new();
    private readonly ILogger<DeadLetterQueue> _logger;
    private readonly int _maxSize;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeadLetterQueue"/> class.
    /// </summary>
    /// <param name="logger">Logger for dead letter operations.</param>
    /// <param name="maxSize">Maximum number of entries to retain. Oldest entries are evicted when exceeded.</param>
    public DeadLetterQueue(ILogger<DeadLetterQueue> logger, int maxSize = 10_000)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxSize = maxSize > 0 ? maxSize : throw new ArgumentOutOfRangeException(nameof(maxSize));
    }

    /// <summary>
    /// Gets the current number of entries in the dead letter queue.
    /// </summary>
    public int Count => _index.Count;

    /// <summary>
    /// Enqueues a failed event message into the dead letter queue.
    /// </summary>
    /// <param name="message">The event message that failed processing.</param>
    /// <param name="handlerName">The name of the handler that failed.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>The created dead letter entry.</returns>
    public DeadLetterEntry Enqueue(EventMessage message, string handlerName, Exception exception)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(message);

        var entry = new DeadLetterEntry(message, handlerName, exception);

        _queue.Enqueue(entry);
        _index[entry.Id] = entry;

        // Evict oldest if over capacity
        while (_index.Count > _maxSize && _queue.TryDequeue(out var evicted))
        {
            _index.TryRemove(evicted.Id, out _);
            _logger.LogWarning("Dead letter queue capacity exceeded; evicted entry {EntryId}", evicted.Id);
        }

        _logger.LogInformation(
            "Dead-lettered message {MessageId} from handler {HandlerName}: {Reason}",
            message.MessageId, handlerName, exception.Message);

        return entry;
    }

    /// <summary>
    /// Retrieves a dead letter entry by its identifier.
    /// </summary>
    /// <param name="entryId">The entry identifier.</param>
    /// <returns>The entry if found; null otherwise.</returns>
    public DeadLetterEntry? GetById(string entryId)
    {
        _index.TryGetValue(entryId, out var entry);
        return entry;
    }

    /// <summary>
    /// Returns all dead letter entries matching the given event type.
    /// </summary>
    /// <param name="eventType">The event type to filter by.</param>
    /// <returns>Matching dead letter entries.</returns>
    public IReadOnlyList<DeadLetterEntry> GetByEventType(string eventType)
    {
        return _index.Values
            .Where(e => string.Equals(e.Message?.EventType, eventType, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Removes a dead letter entry after successful reprocessing.
    /// </summary>
    /// <param name="entryId">The entry identifier to remove.</param>
    /// <returns>True if the entry was found and removed.</returns>
    public bool Remove(string entryId)
    {
        var removed = _index.TryRemove(entryId, out _);
        if (removed)
        {
            _logger.LogDebug("Removed dead letter entry {EntryId}", entryId);
        }
        return removed;
    }

    /// <summary>
    /// Returns a snapshot of all current dead letter entries.
    /// </summary>
    /// <returns>All dead letter entries.</returns>
    public IReadOnlyList<DeadLetterEntry> GetAll()
    {
        return _index.Values.ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _index.Clear();
    }
}
