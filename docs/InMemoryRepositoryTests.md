# InMemoryRepositoryTests

Unit tests for the `InMemoryRepository` class, verifying in-memory storage and retrieval operations for event bus message handling. Tests cover CRUD operations, filtering by status, pagination, and message-specific queries.

## API

### `AddAsync_WithValidEntity_ShouldAddAndRetrieve`
Adds an entity to the repository and verifies it can be retrieved by ID. Ensures the added entity matches the input data.

- **Parameters**:
  - `entity`: The entity to add.
- **Return value**: `Task` completing when the operation finishes.
- **Throws**: `ArgumentNullException` if `entity` is `null`.

### `UpdateAsync_WithExistingEntity_ShouldUpdate`
Updates an existing entity and verifies the changes are persisted. Confirms the updated entity reflects the new values.

- **Parameters**:
  - `entity`: The entity with updated values.
- **Return value**: `Task` completing when the operation finishes.
- **Throws**: `ArgumentNullException` if `entity` is `null`; `KeyNotFoundException` if the entity does not exist.

### `DeleteAsync_WithValidId_ShouldDelete`
Deletes an entity by ID and verifies it is no longer retrievable. Ensures the repository state reflects the deletion.

- **Parameters**:
  - `id`: The ID of the entity to delete.
- **Return value**: `Task` completing when the operation finishes.
- **Throws**: `ArgumentException` if `id` is `null` or empty.

### `ExistsAsync_ShouldReturnCorrectStatus`
Checks whether an entity with the given ID exists in the repository. Validates the correct boolean result is returned.

- **Parameters**:
  - `id`: The ID to check.
- **Return value**: `Task<bool>` indicating existence.
- **Throws**: `ArgumentException` if `id` is `null` or empty.

### `CountAsync_ShouldReturnCorrectCount`
Returns the total number of entities in the repository. Confirms the count matches the expected value.

- **Return value**: `Task<int>` representing the entity count.

### `GetPagedAsync_ShouldReturnPaginatedResults`
Retrieves a paginated subset of entities based on page index and size. Verifies the returned slice matches the requested range.

- **Parameters**:
  - `pageIndex`: Zero-based page index.
  - `pageSize`: Number of items per page.
- **Return value**: `Task<IReadOnlyList<T>>` containing the paginated results.
- **Throws**: `ArgumentOutOfRangeException` if `pageIndex` or `pageSize` are invalid.

### `ClearAsync_ShouldRemoveAllEntities`
Removes all entities from the repository. Verifies the repository is empty afterward.

- **Return value**: `Task` completing when the operation finishes.

### `GetByEventTypeAsync_ShouldReturnMatchingMessages`
Filters entities by event type and returns matching messages. Ensures only entities with the specified type are returned.

- **Parameters**:
  - `eventType`: The event type to filter by.
- **Return value**: `Task<IReadOnlyList<T>>` containing matching entities.
- **Throws**: `ArgumentException` if `eventType` is `null` or empty.

### `GetByCorrelationIdAsync_ShouldReturnRelatedMessages`
Filters entities by correlation ID and returns related messages. Validates the correct subset is returned.

- **Parameters**:
  - `correlationId`: The correlation ID to filter by.
- **Return value**: `Task<IReadOnlyList<T>>` containing matching entities.
- **Throws**: `ArgumentException` if `correlationId` is `null` or empty.

### `DeleteOldMessagesAsync_ShouldRemoveOldMessages`
Removes entities older than a specified threshold. Confirms the repository no longer contains outdated entities.

- **Parameters**:
  - `threshold`: The age threshold for deletion.
- **Return value**: `Task<int>` indicating the number of entities deleted.
- **Throws**: `ArgumentOutOfRangeException` if `threshold` is invalid.

### `GetPendingAsync_ShouldReturnOnlyPendingEntries`
Filters entities by pending status and returns only those awaiting processing. Verifies the correct subset is returned.

- **Return value**: `Task<IReadOnlyList<T>>` containing pending entities.

### `GetByStatusAsync_ShouldFilterByStatus`
Filters entities by a specific status and returns matching entries. Ensures only entities with the given status are included.

- **Parameters**:
  - `status`: The status to filter by.
- **Return value**: `Task<IReadOnlyList<T>>` containing matching entities.
- **Throws**: `ArgumentException` if `status` is `null`.

### `CountByStatusAsync_ShouldReturnCorrectCount`
Returns the number of entities matching a given status. Validates the count reflects the filtered set.

- **Parameters**:
  - `status`: The status to count.
- **Return value**: `Task<int>` representing the count.
- **Throws**: `ArgumentException` if `status` is `null`.

### `Id`
Gets the unique identifier for the in-memory repository instance. Used to distinguish between multiple repositories in tests.

- **Return value**: `string` representing the repository ID.

### `Name`
Gets the display name for the repository. Provides a human-readable label for logging and debugging.

- **Return value**: `string` representing the repository name.

## Usage

### Example 1: Testing CRUD Operations
