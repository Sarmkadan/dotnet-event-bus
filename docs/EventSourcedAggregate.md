# EventSourcedAggregate

A base class for event-sourced aggregates that provides infrastructure for loading, committing, and snapshotting aggregate state from an event store. It tracks uncommitted events, supports snapshot-based recovery, and enforces versioning and schema validation during replay.

## API

### `public string? Id`
The unique identifier of the aggregate. May be `null` if the aggregate has not been persisted.

### `public void LoadFromHistory(IEnumerable<IDomainEvent> events)`
Loads the aggregate state by replaying a sequence of domain events.
- **Parameters**: `events` – An enumerable of domain events to replay in order.
- **Throws**: `ArgumentNullException` if `events` is `null`.
- **Throws**: `InvalidOperationException` if the aggregate has already been loaded or if events are replayed out of order.

### `public void CommitChanges()`
Commits all uncommitted events to the event store and clears the uncommitted event list.
- **Throws**: `InvalidOperationException` if there are no uncommitted events or if the aggregate is not in a valid state for commit.

### `public void LoadSnapshot(AggregateSnapshot snapshot)`
Restores the aggregate state from a previously created snapshot.
- **Parameters**: `snapshot` – The snapshot containing aggregate state and metadata.
- **Throws**: `ArgumentNullException` if `snapshot` is `null`.
- **Throws**: `InvalidOperationException` if the snapshot’s `AggregateId` or `AggregateType` does not match the current aggregate.

### `public AggregateSnapshot CreateSnapshot()`
Creates a snapshot of the current aggregate state.
- **Returns**: An `AggregateSnapshot` containing the current state, version, and metadata.
- **Throws**: `InvalidOperationException` if the aggregate has not been loaded or if required state is missing.

### `public string? AggregateId`
Gets the aggregate’s unique identifier. May be `null` if the aggregate is transient.

### `public string? AggregateType`
Gets the type identifier of the aggregate. Used for routing and deserialization.

### `public int Version`
Gets the current version of the aggregate, representing the number of events applied.

### `public DateTime CreatedAt`
Gets the timestamp when the aggregate was first created.

### `public Dictionary<string, object?> State`
Gets the mutable state dictionary of the aggregate. Keys are state property names; values are the current state values.

### `public int SnapshotInterval`
Gets or sets the number of events after which a snapshot is automatically created. A value of `0` disables automatic snapshotting.

### `public bool EnableAutoSnapshots`
Gets or sets whether automatic snapshots are enabled. When `true`, snapshots are created every `SnapshotInterval` events.

### `public int MaxEventsToReplay`
Gets or sets the maximum number of events to replay during `LoadFromHistory`. Prevents unbounded event replay in large aggregates.

### `public bool ValidateEventSchema`
Gets or sets whether to validate event schemas during replay. When `true`, enforces schema compatibility for each event.

## Usage

### Example 1: Loading and Committing an Aggregate
