#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetEventBus.Repositories;

/// <summary>
/// Extension methods for <see cref="InMemoryRepository{T}"/> providing additional convenience operations.
/// </summary>
public static class InMemoryRepositoryExtensions
{
    /// <summary>
    /// Gets the first entity matching the specified predicate, or null if not found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <param name="predicate">The predicate to match entities.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The first matching entity or null.</returns>
    public static async Task<T?> FirstOrDefaultAsync<T>(this IRepository<T> repository,
        Func<T, bool> predicate,
        CancellationToken cancellationToken = default) where T : class
    {
        if (predicate is null)
            throw new ArgumentNullException(nameof(predicate));

        var allItems = await repository.GetAllAsync(cancellationToken);
        return allItems.FirstOrDefault(predicate);
    }

    /// <summary>
    /// Gets all entities matching the specified predicate.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <param name="predicate">The predicate to match entities.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Collection of matching entities.</returns>
    public static async Task<IEnumerable<T>> WhereAsync<T>(this IRepository<T> repository,
        Func<T, bool> predicate,
        CancellationToken cancellationToken = default) where T : class
    {
        if (predicate is null)
            throw new ArgumentNullException(nameof(predicate));

        var allItems = await repository.GetAllAsync(cancellationToken);
        return allItems.Where(predicate).ToList();
    }

    /// <summary>
    /// Determines whether any entity matches the specified predicate.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <param name="predicate">The predicate to match entities.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if any entity matches; otherwise false.</returns>
    public static async Task<bool> AnyAsync<T>(this IRepository<T> repository,
        Func<T, bool> predicate,
        CancellationToken cancellationToken = default) where T : class
    {
        if (predicate is null)
            throw new ArgumentNullException(nameof(predicate));

        var allItems = await repository.GetAllAsync(cancellationToken);
        return allItems.Any(predicate);
    }

    /// <summary>
    /// Gets entities with pagination and applies a filter predicate.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="predicate">Optional filter predicate to apply before pagination.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Paginated result with filtered items.</returns>
    public static async Task<PaginatedResult<T>> GetPagedAsync<T>(this IRepository<T> repository,
        int pageNumber,
        int pageSize,
        Func<T, bool>? predicate = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (pageNumber < 1)
            throw new ArgumentException("Page number must be at least 1", nameof(pageNumber));
        if (pageSize < 1)
            throw new ArgumentException("Page size must be at least 1", nameof(pageSize));

        var allItems = await repository.GetAllAsync(cancellationToken);
        IEnumerable<T> filteredItems = allItems;

        if (predicate is not null)
        {
            filteredItems = allItems.Where(predicate);
        }

        var total = filteredItems.Count();
        var items = filteredItems
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedResult<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = total
        };
    }
}