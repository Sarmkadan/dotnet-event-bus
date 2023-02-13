// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetEventBus.Models;

namespace DotnetEventBus.Repositories;

/// <summary>
/// Repository for storing and querying event messages with additional filtering capabilities.
/// </summary>
public interface IEventMessageRepository : IRepository<EventMessage>
{
    /// <summary>
    /// Gets all messages of a specific event type.
    /// </summary>
    Task<IEnumerable<EventMessage>> GetByEventTypeAsync(string eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets messages created within a time range.
    /// </summary>
    Task<IEnumerable<EventMessage>> GetByTimeRangeAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets messages with a specific correlation ID.
    /// </summary>
    Task<IEnumerable<EventMessage>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets messages from a specific source.
    /// </summary>
    Task<IEnumerable<EventMessage>> GetBySourceAsync(string source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets messages with failed processing attempts.
    /// </summary>
    Task<IEnumerable<EventMessage>> GetFailedMessagesAsync(int minAttempts = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old messages based on retention period.
    /// </summary>
    Task<int> DeleteOldMessagesAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of the event message repository.
/// </summary>
public class InMemoryEventMessageRepository : InMemoryRepository<EventMessage>, IEventMessageRepository
{
    public async Task<IEnumerable<EventMessage>> GetByEventTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty", nameof(eventType));

        var messages = await GetAllAsync(cancellationToken);
        return messages.Where(m => m.EventType == eventType).ToList();
    }

    public async Task<IEnumerable<EventMessage>> GetByTimeRangeAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        if (endUtc < startUtc)
            throw new ArgumentException("End time must be after start time");

        var messages = await GetAllAsync(cancellationToken);
        return messages
            .Where(m => m.CreatedAtUtc >= startUtc && m.CreatedAtUtc <= endUtc)
            .ToList();
    }

    public async Task<IEnumerable<EventMessage>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("Correlation ID cannot be empty", nameof(correlationId));

        var messages = await GetAllAsync(cancellationToken);
        return messages.Where(m => m.CorrelationId == correlationId).ToList();
    }

    public async Task<IEnumerable<EventMessage>> GetBySourceAsync(string source, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be empty", nameof(source));

        var messages = await GetAllAsync(cancellationToken);
        return messages.Where(m => m.Source == source).ToList();
    }

    public async Task<IEnumerable<EventMessage>> GetFailedMessagesAsync(int minAttempts = 1, CancellationToken cancellationToken = default)
    {
        if (minAttempts < 1)
            throw new ArgumentException("Minimum attempts must be at least 1", nameof(minAttempts));

        var messages = await GetAllAsync(cancellationToken);
        return messages.Where(m => m.ProcessingAttempts >= minAttempts).ToList();
    }

    public async Task<int> DeleteOldMessagesAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
    {
        if (retentionPeriod <= TimeSpan.Zero)
            throw new ArgumentException("Retention period must be greater than zero", nameof(retentionPeriod));

        var cutoffTime = DateTime.UtcNow.Subtract(retentionPeriod);
        var allMessages = await GetAllAsync(cancellationToken);

        var messagesToDelete = allMessages.Where(m => m.CreatedAtUtc < cutoffTime).ToList();

        int deletedCount = 0;
        foreach (var message in messagesToDelete)
        {
            if (await DeleteAsync(message, cancellationToken))
                deletedCount++;
        }

        return deletedCount;
    }
}
