#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetEventBus.Repositories;

/// <summary>
/// Extension methods for <see cref="IRepository{T}"/> providing additional convenience operations.
/// </summary>
public static class InMemoryRepositoryExtensions
{
    /// <summary>
    /// Gets the first entity matching the specified predicate, or null if not found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="repository">The repository instance. Cannot be null.</param>
    /// <param name="predicate">The predicate to match entities. Cannot be null.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The first matching entity or null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> or <paramref name="predicate"/> is null.</exception>
    public static async Task<T?> FirstOrDefaultAsync<T>(this IRepository<T> repository,
        Func<T, bool> predicate,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(predicate);

        var allItems = await repository.GetAllAsync(cancellationToken);
        return allItems.FirstOrDefault(predicate);
    }

    /// <summary>
    /// Gets all entities matching the specified predicate.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="repository">The repository instance. Cannot be null.</param>
    /// <param name="predicate">The predicate to match entities. Cannot be null.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Collection of matching entities.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> or <paramref name="predicate"/> is null.</exception>
    public static async Task<IEnumerable<T>> WhereAsync<T>(this IRepository<T> repository,
        Func<T, bool> predicate,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(predicate);

        var allItems = await repository.GetAllAsync(cancellationToken);
        return allItems.Where(predicate).ToList();
    }

    /// <summary>
    /// Determines whether any entity matches the specified predicate.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="repository">The repository instance. Cannot be null.</param>
    /// <param name="predicate">The predicate to match entities. Cannot be null.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if any entity matches; otherwise false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> or <paramref name="predicate"/> is null.</exception>
    public static async Task<bool> AnyAsync<T>(this IRepository<T> repository,
        Func<T, bool> predicate,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(predicate);

        var allItems = await repository.GetAllAsync(cancellationToken);
        return allItems.Any(predicate);
    }

    /// <summary>
    /// Gets entities with pagination and applies a filter predicate.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="repository">The repository instance. Cannot be null.</param>
    /// <param name="pageNumber">The page number (1-based). Must be greater than 0.</param>
    /// <param name="pageSize">The number of items per page. Must be greater than 0.</param>
    /// <param name="predicate">Optional filter predicate to apply before pagination.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Paginated result with filtered items.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="pageNumber"/> or <paramref name="pageSize"/> is less than 1.</exception>
    public static async Task<PaginatedResult<T>> GetPagedAsync<T>(this IRepository<T> repository,
        int pageNumber,
        int pageSize,
        Func<T, bool>? predicate = null,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(repository);

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