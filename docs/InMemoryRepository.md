# InMemoryRepository

A generic in-memory repository implementation for quick prototyping or testing scenarios where persistence is not required. It provides basic CRUD operations and pagination support for any entity type `T` constrained to `class`.

## API

### `public async Task<T?> GetByIdAsync(TId id)`

Retrieves an entity by its identifier asynchronously.

- **Parameters**
  - `id`: The identifier of the entity to retrieve.
- **Return value**
  - The entity if found; otherwise, `null`.
- **Exceptions**
  - Throws `ArgumentNullException` if `id` is `null`.

### `public async Task<IEnumerable<T>> GetAllAsync()`

Retrieves all entities stored in the repository asynchronously.

- **Return value**
  - An `IEnumerable<T>` containing all entities.

### `public async Task<T> AddAsync(T entity)`

Adds a new entity to the repository asynchronously.

- **Parameters**
  - `entity`: The entity to add.
- **Return value**
  - The added entity.
- **Exceptions**
  - Throws `ArgumentNullException` if `entity` is `null`.
  - Throws `InvalidOperationException` if an entity with the same identifier already exists.

### `public async Task<T> UpdateAsync(T entity)`

Updates an existing entity in the repository asynchronously.

- **Parameters**
  - `entity`: The entity to update.
- **Return value**
  - The updated entity.
- **Exceptions**
  - Throws `ArgumentNullException` if `entity` is `null`.
  - Throws `KeyNotFoundException` if no entity with the same identifier exists.

### `public async Task<bool> DeleteAsync(TId id)`

Deletes an entity by its identifier asynchronously.

- **Parameters**
  - `id`: The identifier of the entity to delete.
- **Return value**
  - `true` if the entity was found and deleted; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `id` is `null`.

### `public async Task<bool> ExistsAsync(TId id)`

Checks whether an entity with the given identifier exists asynchronously.

- **Parameters**
  - `id`: The identifier to check.
- **Return value**
  - `true` if the entity exists; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `id` is `null`.

### `public async Task<int> CountAsync()`

Gets the total number of entities stored in the repository asynchronously.

- **Return value**
  - The count of entities.

### `public async Task<PaginatedResult<T>> GetPagedAsync(int pageNumber, int pageSize)`

Retrieves a paginated subset of entities asynchronously.

- **Parameters**
  - `pageNumber`: The zero-based page number to retrieve.
  - `pageSize`: The maximum number of items per page.
- **Return value**
  - A `PaginatedResult<T>` containing the subset of entities and metadata.
- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `pageNumber` or `pageSize` is negative.
  - Throws `ArgumentException` if `pageSize` is zero.

### `public async Task ClearAsync()`

Removes all entities from the repository asynchronously.

### `public void Dispose()`

Releases all resources used by the repository.

## Usage

### Example 1: Basic CRUD Operations
