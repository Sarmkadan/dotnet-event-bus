#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetEventBus.Repositories;

/// <summary>
/// In-memory repository implementation using a thread-safe dictionary.
/// Suitable for testing and single-process deployments.
/// </summary>
public sealed class InMemoryRepository<T> : IRepository<T> where T : class
{
    private readonly Dictionary<string, T> _store = new();
    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    /// Gets an entity by its ID.
    /// </summary>
    public async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID cannot be empty", nameof(id));

        _lock.EnterReadLock();
        try
        {
            return _store.TryGetValue(id, out var entity) ? entity : null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets all entities.
    /// </summary>
    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            return _store.Values.ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Adds a new entity.
    /// </summary>
    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        var id = GetEntityId(entity);
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Entity must have a valid ID property", nameof(entity));

        _lock.EnterWriteLock();
        try
        {
            if (_store.ContainsKey(id))
                throw new InvalidOperationException($"Entity with ID '{id}' already exists");

            _store[id] = entity;
            return entity;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    public async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        var id = GetEntityId(entity);
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Entity must have a valid ID property", nameof(entity));

        _lock.EnterWriteLock();
        try
        {
            if (!_store.ContainsKey(id))
                throw new InvalidOperationException($"Entity with ID '{id}' not found");

            _store[id] = entity;
            return entity;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Deletes an entity by its ID.
    /// </summary>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID cannot be empty", nameof(id));

        _lock.EnterWriteLock();
        try
        {
            return _store.Remove(id);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Deletes an entity.
    /// </summary>
    public async Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        var id = GetEntityId(entity);
        return await DeleteAsync(id, cancellationToken);
    }

    /// <summary>
    /// Checks if an entity exists.
    /// </summary>
    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID cannot be empty", nameof(id));

        _lock.EnterReadLock();
        try
        {
            return _store.ContainsKey(id);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets the total count of entities.
    /// </summary>
    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            return _store.Count;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets entities with pagination.
    /// </summary>
    public async Task<PaginatedResult<T>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
            throw new ArgumentException("Page number must be at least 1", nameof(pageNumber));
        if (pageSize < 1)
            throw new ArgumentException("Page size must be at least 1", nameof(pageSize));

        _lock.EnterReadLock();
        try
        {
            var total = _store.Count;
            var items = _store.Values
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
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Clears all entities.
    /// </summary>
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            _store.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Extracts the ID from an entity (looks for Id or Id property).
    /// </summary>
    private static string? GetEntityId(T entity)
    {
        var idProperty = typeof(T).GetProperty("Id",
            System.Reflection.BindingFlags.IgnoreCase |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance);

        return idProperty?.GetValue(entity)?.ToString();
    }

    /// <summary>
    /// Disposes the lock resources.
    /// </summary>
    public void Dispose()
    {
        _lock?.Dispose();
    }
}
