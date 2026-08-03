using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotnetEventBus.Repositories;
using Xunit;

namespace DotnetEventBus.Tests;

public class InMemoryRepositoryExtensionsTests
{
    // ------------------------------------------------------------------------
    // Simple in‑memory repository implementation used only for these tests.
    // ------------------------------------------------------------------------
    private sealed class InMemoryRepository<T> : IRepository<T> where T : class
    {
        private readonly List<T> _items = new();

        public Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<T?>(null);

        public Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<T>>(_items.ToArray());

        public Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            _items.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
            => Task.FromResult(entity);

        public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            _items.Remove(entity);
            return Task.FromResult(true);
        }

        public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_items.Count);

        public Task<PaginatedResult<T>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult(new PaginatedResult<T>
            {
                Items = _items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = _items.Count
            });

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            _items.Clear();
            return Task.CompletedTask;
        }
    }

    // ------------------------------------------------------------------------
    // FirstOrDefaultAsync tests
    // ------------------------------------------------------------------------
    [Fact]
    public async Task FirstOrDefaultAsync_ReturnsFirstMatchingItem()
    {
        var repo = new InMemoryRepository<string>();
        await repo.AddAsync("alpha");
        await repo.AddAsync("beta");
        await repo.AddAsync("gamma");

        var result = await repo.FirstOrDefaultAsync(s => s == "beta");

        Assert.Equal("beta", result);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ReturnsNullWhenNoMatch()
    {
        var repo = new InMemoryRepository<int>();
        await repo.AddAsync(1);
        await repo.AddAsync(2);

        var result = await repo.FirstOrDefaultAsync(i => i == 99);

        Assert.Null(result);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ThrowsArgumentNullException_WhenRepositoryIsNull()
    {
        IRepository<string>? repo = null;
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await repo!.FirstOrDefaultAsync(s => s.Length > 0));
    }

    // ------------------------------------------------------------------------
    // WhereAsync tests
    // ------------------------------------------------------------------------
    [Fact]
    public async Task WhereAsync_ReturnsAllMatchingItems()
    {
        var repo = new InMemoryRepository<int>();
        foreach (var i in Enumerable.Range(1, 5))
            await repo.AddAsync(i);

        var result = await repo.WhereAsync(i => i % 2 == 0);

        Assert.Equal(new[] { 2, 4 }, result);
    }

    [Fact]
    public async Task WhereAsync_ReturnsEmptyWhenNoItemsMatch()
    {
        var repo = new InMemoryRepository<string>();
        await repo.AddAsync("one");
        await repo.AddAsync("two");

        var result = await repo.WhereAsync(s => s.StartsWith("z"));

        Assert.Empty(result);
    }

    // ------------------------------------------------------------------------
    // AnyAsync tests
    // ------------------------------------------------------------------------
    [Fact]
    public async Task AnyAsync_ReturnsTrueWhenAnyMatch()
    {
        var repo = new InMemoryRepository<string>();
        await repo.AddAsync("apple");
        await repo.AddAsync("banana");

        var result = await repo.AnyAsync(s => s.Contains("app"));

        Assert.True(result);
    }

    [Fact]
    public async Task AnyAsync_ReturnsFalseWhenNoMatch()
    {
        var repo = new InMemoryRepository<string>();
        await repo.AddAsync("cat");
        await repo.AddAsync("dog");

        var result = await repo.AnyAsync(s => s.Contains("z"));

        Assert.False(result);
    }

    [Fact]
    public async Task AnyAsync_ThrowsArgumentNullException_WhenRepositoryIsNull()
    {
        IRepository<int>? repo = null;
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await repo!.AnyAsync(i => i > 0));
    }

    // ------------------------------------------------------------------------
    // GetPagedAsync tests (extension method)
    // ------------------------------------------------------------------------
    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectPageWithoutPredicate()
    {
        var repo = new InMemoryRepository<int>();
        foreach (var i in Enumerable.Range(1, 10))
            await repo.AddAsync(i);

        var page = await repo.GetPagedAsync(pageNumber: 2, pageSize: 3);

        Assert.Equal(2, page.PageNumber);
        Assert.Equal(3, page.PageSize);
        Assert.Equal(10, page.TotalCount);
        Assert.Equal(new[] { 4, 5, 6 }, page.Items);
    }

    [Fact]
    public async Task GetPagedAsync_AppliesPredicateBeforePaging()
    {
        var repo = new InMemoryRepository<int>();
        foreach (var i in Enumerable.Range(1, 10))
            await repo.AddAsync(i);

        var page = await repo.GetPagedAsync(
            pageNumber: 1,
            pageSize: 3,
            predicate: i => i > 5);

        Assert.Equal(1, page.PageNumber);
        Assert.Equal(3, page.PageSize);
        Assert.Equal(5, page.TotalCount); // items 6..10
        Assert.Equal(new[] { 6, 7, 8 }, page.Items);
    }

    [Fact]
    public async Task GetPagedAsync_ThrowsArgumentException_WhenPageNumberIsLessThanOne()
    {
        var repo = new InMemoryRepository<int>();
        await repo.AddAsync(1);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await repo.GetPagedAsync(pageNumber: 0, pageSize: 5));
    }

    [Fact]
    public async Task GetPagedAsync_ThrowsArgumentException_WhenPageSizeIsLessThanOne()
    {
        var repo = new InMemoryRepository<int>();
        await repo.AddAsync(1);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await repo.GetPagedAsync(pageNumber: 1, pageSize: 0));
    }
}
