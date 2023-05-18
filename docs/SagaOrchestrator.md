# SagaOrchestrator

`SagaOrchestrator` is a stateful coordinator for executing a sequence of steps (a saga) in a reliable, compensatable workflow. It tracks each step's status, handles failures by triggering compensation actions, and provides visibility into the overall execution state. This type is typically used to implement long-running, distributed transactions where atomicity is achieved through compensating actions rather than traditional rollback mechanisms.

## API

### `SagaOrchestrator<TContext>`
The generic type `SagaOrchestrator<TContext>` represents a saga orchestrator with a custom context type `TContext`. The context is passed between steps and can be used to share state or data across the saga's execution.

### `public SagaOrchestrator<TContext> AddStep(Func<TContext, Task> action, Func<TContext, Task>? compensationAction = null)`
Adds a step to the saga with an optional compensation action.

**Parameters:**
- `action` (`Func<TContext, Task>`): The primary action to execute for this step. Must not be `null`.
- `compensationAction` (`Func<TContext, Task>?`): The optional compensation action to execute if this step or a subsequent step fails. Can be `null` if no compensation is required.

**Returns:**
- The `SagaOrchestrator<TContext>` instance to allow method chaining.

**Throws:**
- `ArgumentNullException`: Thrown if `action` is `null`.

---

### `public async Task<SagaExecutionResult> ExecuteAsync(TContext context)`
Executes the saga steps in sequence. If any step fails, it triggers the compensation actions for all previously executed steps in reverse order.

**Parameters:**
- `context` (`TContext`): The context object passed to each step's action and compensation action.

**Returns:**
- A `SagaExecutionResult` containing the outcome of the execution, including success status, error details, and step statuses.

**Throws:**
- `InvalidOperationException`: Thrown if no steps have been added to the orchestrator.
- Exceptions thrown by individual step actions or compensation actions are captured and included in the result.

---

### `public IEnumerable<SagaStep<TContext>> GetStepStatus()`
Retrieves the status of all steps in the saga, including their execution state, error messages (if any), and timestamps.

**Returns:**
- An enumerable of `SagaStep<TContext>` objects, each representing a step's current state.

---

### `public required string Name`
The name of the saga orchestrator. This is used for logging, diagnostics, and identification purposes.

**Throws:**
- `InvalidOperationException`: Thrown if `Name` is not set before execution or status retrieval.

---

### `public required Func<TContext, Task> Action`
The primary action of a step. This is set when adding a step via `AddStep`.

---

### `public Func<TContext, Task>? CompensationAction`
The optional compensation action of a step. This is set when adding a step via `AddStep` and can be `null`.

---

### `public SagaStepStatus Status`
The current status of a step (`NotStarted`, `Executing`, `Completed`, `Compensating`, `Compensated`, `Failed`).

---

### `public string? ErrorMessage`
The error message associated with a step's failure, if applicable. `null` if the step succeeded or has not yet executed.

---

### `public string? SagaId`
A unique identifier for the saga execution instance. This is generated during execution and remains `null` until `ExecuteAsync` is called.

---

### `public bool Success`
Indicates whether the saga execution completed successfully. `true` if all steps executed without errors; `false` otherwise.

---

### `public string? FailedStep`
The name or identifier of the step that caused the saga to fail. `null` if the saga succeeded or has not yet executed.

---

### `public string? Error`
The error message or exception details associated with the saga's failure. `null` if the saga succeeded or has not yet executed.

---

### `public DateTime ExecutedAt`
The timestamp of when the saga execution started. Defaults to `DateTime.MinValue` if the saga has not yet executed.

## Usage

### Example 1: Basic Saga with Compensation
