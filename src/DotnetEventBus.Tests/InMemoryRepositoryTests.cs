using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotnetEventBus.Repositories;
using Xunit;

namespace DotnetEventBus.Tests.Repositories;

public class InMemoryRepositoryTests
{
    private sealed class TestEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? Name { get; set; }
    }

    private InMemoryRepository<TestEntity> CreateRepository() => new InMemoryRepository<TestEntity>();

    [Fact]
    public async Task AddAsync_ShouldAddEntityAndAllowRetrieval()
    {
        await using var repo = CreateRepository();
        var entity = new TestEntity { Name = "Alice" };

        var added = await repo.AddAsync(entity);
        Assert.Same(entity, added);

        var fetched = await repo.GetByIdAsync(entity.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Alice", fetched!.Name);
    }

    [Fact]
    public async Task AddAsync_ShouldThrow_WhenEntityIsNull()
    {
        await using var repo = CreateRepository();
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.AddAsync(null!));
    }

    [Fact]
    public async Task AddAsync_ShouldThrow_WhenDuplicateId()
    {
        await using var repo = CreateRepository();
        var entity = new TestEntity { Name = "Bob" };
        await repo.AddAsync(entity);

        var duplicate = new TestEntity { Id = entity.Id, Name = "Bob2" };
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.AddAsync(duplicate));
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyExistingEntity()
    {
        await using var repo = CreateRepository();
        var entity = new TestEntity { Name = "Charlie" };
        await repo.AddAsync(entity);

        entity.Name = "Charlie Updated";
        var updated = await repo.UpdateAsync(entity);
        Assert.Equal("Charlie Updated", updated.Name);

        var fetched = await repo.GetByIdAsync(entity.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Charlie Updated", fetched!.Name);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenEntityDoesNotExist()
    {
        await using var repo = CreateRepository();
        var entity = new TestEntity { Name = "NonExistent" };
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await repo.UpdateAsync(entity));
    }

    [Fact]
    public async Task DeleteAsync_ById_ShouldReturnTrueWhenEntityExists()
    {
        await using var repo = CreateRepository();
        var entity = new TestEntity { Name = "Dave" };
        await repo.AddAsync(entity);

        var result = await repo.DeleteAsync(entity.Id);
        Assert.True(result);
        Assert.Null(await repo.GetByIdAsync(entity.Id));
    }

    [Fact]
    public async Task DeleteAsync_ById_ShouldReturnFalseWhenEntityDoesNotExist()
    {
        await using var repo = CreateRepository();
        var result = await repo.DeleteAsync("non-existent-id");
        Assert.False(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllAddedEntities()
    {
        await using var repo = CreateRepository();
        var entities = new[]
        {
            new TestEntity { Name = "E1" },
            new TestEntity { Name = "E2" },
            new TestEntity { Name = "E3" }
        };

        foreach (var e in entities)
            await repo.AddAsync(e);

        var all = await repo.GetAllAsync();
        Assert.Equal(3, all.Count());
        Assert.All(entities, e => Assert.Contains(all, x => x.Id == e.Id));
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnCorrectPageAndMetadata()
    {
        await using var repo = CreateRepository();
        for (int i = 1; i <= 10; i++)
        {
            await repo.AddAsync(new TestEntity { Name = $"Item{i}" });
        }

        var page = await repo.GetPagedAsync(pageNumber: 2, pageSize: 3);
        Assert.Equal(2, page.PageNumber);
        Assert.Equal(3, page.PageSize);
        Assert.Equal(10, page.TotalCount);
        Assert.Equal(3, page.Items.Count);
        // Items should be 4th,5th,6th inserted (zero‑based index 3..5)
        var expectedNames = new[] { "Item4", "Item5", "Item6" };
        Assert.Equal(expectedNames, page.Items.Select(i => i.Name));
    }

    [Fact]
    public async Task ClearAsync_ShouldRemoveAllEntities()
    {
        await using var repo = CreateRepository();
        await repo.AddAsync(new TestEntity { Name = "ToBeCleared" });
        await repo.ClearAsync();

        var count = await repo.CountAsync();
        Assert.Equal(0, count);
        var all = await repo.GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReflectPresenceOfEntity()
    {
        await using var repo = CreateRepository();
        var entity = new TestEntity { Name = "Existence" };
        await repo.AddAsync(entity);

        Assert.True(await repo.ExistsAsync(entity.Id));
        Assert.False(await repo.ExistsAsync("unknown-id"));
    }

    [Fact]
    public async Task CountAsync_ShouldReturnNumberOfStoredEntities()
    {
        await using var repo = CreateRepository();
        Assert.Equal(0, await repo.CountAsync());

        await repo.AddAsync(new TestEntity { Name = "One" });
        await repo.AddAsync(new TestEntity { Name = "Two" });

        Assert.Equal(2, await repo.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrow_WhenIdIsNullOrWhiteSpace()
    {
        await using var repo = CreateRepository();
        await Assert.ThrowsAsync<ArgumentException>(async () => await repo.GetByIdAsync(string.Empty));
        await Assert.ThrowsAsync<ArgumentException>(async () => await repo.GetByIdAsync("   "));
    }

    [Fact]
    public async Task DeleteAsync_ByEntity_ShouldThrow_WhenEntityIsNull()
    {
        await using var repo = CreateRepository();
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.DeleteAsync(null!));
    }
}
