// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Xunit;
using DotnetEventBus.Models;
using DotnetEventBus.Repositories;

namespace DotnetEventBus.Tests;

/// <summary>
/// Unit tests for repository implementations.
/// </summary>
public class InMemoryRepositoryTests
{
    [Fact]
    public async Task AddAsync_WithValidEntity_ShouldAddAndRetrieve()
    {
        // Arrange
        var repository = new InMemoryRepository<TestEntity>();
        var entity = new TestEntity { Id = "1", Name = "Test" };

        // Act
        var result = await repository.AddAsync(entity);
        var retrieved = await repository.GetByIdAsync("1");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(retrieved);
        Assert.Equal("Test", retrieved.Name);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingEntity_ShouldUpdate()
    {
        // Arrange
        var repository = new InMemoryRepository<TestEntity>();
        var entity = new TestEntity { Id = "1", Name = "Original" };
        await repository.AddAsync(entity);

        // Act
        entity.Name = "Updated";
        await repository.UpdateAsync(entity);
        var retrieved = await repository.GetByIdAsync("1");

        // Assert
        Assert.Equal("Updated", retrieved?.Name);
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldDelete()
    {
        // Arrange
        var repository = new InMemoryRepository<TestEntity>();
        var entity = new TestEntity { Id = "1", Name = "Test" };
        await repository.AddAsync(entity);

        // Act
        var result = await repository.DeleteAsync("1");
        var retrieved = await repository.GetByIdAsync("1");

        // Assert
        Assert.True(result);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnCorrectStatus()
    {
        // Arrange
        var repository = new InMemoryRepository<TestEntity>();
        var entity = new TestEntity { Id = "1", Name = "Test" };
        await repository.AddAsync(entity);

        // Act & Assert
        Assert.True(await repository.ExistsAsync("1"));
        Assert.False(await repository.ExistsAsync("nonexistent"));
    }

    [Fact]
    public async Task CountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var repository = new InMemoryRepository<TestEntity>();
        await repository.AddAsync(new TestEntity { Id = "1", Name = "Test1" });
        await repository.AddAsync(new TestEntity { Id = "2", Name = "Test2" });

        // Act
        var count = await repository.CountAsync();

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPaginatedResults()
    {
        // Arrange
        var repository = new InMemoryRepository<TestEntity>();
        for (int i = 1; i <= 5; i++)
        {
            await repository.AddAsync(new TestEntity { Id = i.ToString(), Name = $"Test{i}" });
        }

        // Act
        var page1 = await repository.GetPagedAsync(1, 2);
        var page2 = await repository.GetPagedAsync(2, 2);
        var page3 = await repository.GetPagedAsync(3, 2);

        // Assert
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(2, page2.Items.Count);
        Assert.Equal(1, page3.Items.Count);
        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(3, page1.TotalPages);
        Assert.True(page1.HasNextPage);
        Assert.True(page2.HasNextPage);
        Assert.False(page3.HasNextPage);
    }

    [Fact]
    public async Task ClearAsync_ShouldRemoveAllEntities()
    {
        // Arrange
        var repository = new InMemoryRepository<TestEntity>();
        await repository.AddAsync(new TestEntity { Id = "1", Name = "Test1" });
        await repository.AddAsync(new TestEntity { Id = "2", Name = "Test2" });

        // Act
        await repository.ClearAsync();
        var count = await repository.CountAsync();

        // Assert
        Assert.Equal(0, count);
    }
}

/// <summary>
/// Unit tests for event message repository.
/// </summary>
public class EventMessageRepositoryTests
{
    private readonly IEventMessageRepository _repository = new InMemoryEventMessageRepository();

    [Fact]
    public async Task GetByEventTypeAsync_ShouldReturnMatchingMessages()
    {
        // Arrange
        var msg1 = new EventMessage("Event1", "payload1");
        var msg2 = new EventMessage("Event1", "payload2");
        var msg3 = new EventMessage("Event2", "payload3");

        await _repository.AddAsync(msg1);
        await _repository.AddAsync(msg2);
        await _repository.AddAsync(msg3);

        // Act
        var results = await _repository.GetByEventTypeAsync("Event1");

        // Assert
        Assert.Equal(2, results.Count());
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_ShouldReturnRelatedMessages()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var msg1 = new EventMessage("Event1", "payload1") { CorrelationId = correlationId };
        var msg2 = new EventMessage("Event1", "payload2") { CorrelationId = Guid.NewGuid().ToString() };

        await _repository.AddAsync(msg1);
        await _repository.AddAsync(msg2);

        // Act
        var results = await _repository.GetByCorrelationIdAsync(correlationId);

        // Assert
        Assert.Single(results);
        Assert.Equal(correlationId, results.First().CorrelationId);
    }

    [Fact]
    public async Task DeleteOldMessagesAsync_ShouldRemoveOldMessages()
    {
        // Arrange
        var oldMsg = new EventMessage("Event1", "payload1");
        oldMsg.CreatedAtUtc = DateTime.UtcNow.AddDays(-10);
        var newMsg = new EventMessage("Event1", "payload2");

        await _repository.AddAsync(oldMsg);
        await _repository.AddAsync(newMsg);

        // Act
        var deletedCount = await _repository.DeleteOldMessagesAsync(TimeSpan.FromDays(7));

        // Assert
        Assert.Equal(1, deletedCount);
        Assert.Null(await _repository.GetByIdAsync(oldMsg.MessageId));
        Assert.NotNull(await _repository.GetByIdAsync(newMsg.MessageId));
    }
}

/// <summary>
/// Unit tests for dead letter repository.
/// </summary>
public class DeadLetterRepositoryTests
{
    private readonly IDeadLetterRepository _repository = new InMemoryDeadLetterRepository();

    [Fact]
    public async Task GetPendingAsync_ShouldReturnOnlyPendingEntries()
    {
        // Arrange
        var msg = new EventMessage("Event1", "payload");
        var entry1 = new DeadLetterEntry(msg, "Handler1", new Exception("Test"));
        var entry2 = new DeadLetterEntry(msg, "Handler2", new Exception("Test"));
        entry2.MarkAsReprocessed();

        await _repository.AddAsync(entry1);
        await _repository.AddAsync(entry2);

        // Act
        var pending = await _repository.GetPendingAsync();

        // Assert
        Assert.Single(pending);
        Assert.Equal(entry1.Id, pending.First().Id);
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldFilterByStatus()
    {
        // Arrange
        var msg = new EventMessage("Event1", "payload");
        var entry = new DeadLetterEntry(msg, "Handler1", new Exception("Test"));
        await _repository.AddAsync(entry);

        // Act
        var pending = await _repository.GetByStatusAsync(DeadLetterStatus.Pending);
        var reviewed = await _repository.GetByStatusAsync(DeadLetterStatus.ReviewedNotProcessed);

        // Assert
        Assert.Single(pending);
        Assert.Empty(reviewed);
    }

    [Fact]
    public async Task CountByStatusAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var msg = new EventMessage("Event1", "payload");
        var entry1 = new DeadLetterEntry(msg, "Handler1", new Exception("Test"));
        var entry2 = new DeadLetterEntry(msg, "Handler2", new Exception("Test"));

        await _repository.AddAsync(entry1);
        await _repository.AddAsync(entry2);

        // Act
        var count = await _repository.CountByStatusAsync(DeadLetterStatus.Pending);

        // Assert
        Assert.Equal(2, count);
    }
}

/// <summary>
/// Test entity for repository testing.
/// </summary>
public class TestEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
}
