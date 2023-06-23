# PipelineBuilderTests

Unit tests for `PipelineBuilder<T>` that verify middleware registration, pipeline construction, execution order, context modification, short-circuiting, exception handling, and builder chaining.

## API

### `public void Use_WithNullMiddleware_ShouldThrowArgumentNullException()`

Verifies that registering a `null` middleware via `Use` throws an `ArgumentNullException`.

- **Parameters**: None.
- **Return value**: None.
- **Throws**: `ArgumentNullException` if the middleware argument is `null`.

---

### `public void Build_WithoutMiddleware_ShouldReturnValidPipeline()`

Ensures that building a pipeline with no middleware registered produces a valid, executable pipeline.

- **Parameters**: None.
- **Return value**: None.
- **Throws**: None.

---

### `public async Task Build_WithSingleMiddleware_ShouldExecuteMiddleware()`

Validates that a single middleware registered via `Use` is invoked exactly once during pipeline execution.

- **Parameters**: None.
- **Return value**: `Task` representing the asynchronous test.
- **Throws**: Propagates any exceptions thrown by the middleware.

---

### `public async Task Build_WithMultipleMiddleware_ShouldExecuteInOrder()`

Confirms that multiple middleware are executed in the order they were registered.

- **Parameters**: None.
- **Return value**: `Task` representing the asynchronous test.
- **Throws**: Propagates any exceptions thrown by the middleware.

---

### `public async Task Build_MiddlewareCanModifyContext()`

Demonstrates that middleware can read and modify the `EventContext` during execution.

- **Parameters**: None.
- **Return value**: `Task` representing the asynchronous test.
- **Throws**: Propagates any exceptions thrown by the middleware.

---
### `public async Task Build_MiddlewareCanShortCircuit()`

Tests that middleware can prevent subsequent middleware from executing by not invoking the continuation delegate.

- **Parameters**: None.
- **Return value**: `Task` representing the asynchronous test.
- **Throws**: Propagates any exceptions thrown by the middleware.

---
### `public async Task Build_MiddlewareCanHandleExceptions()`

Ensures that middleware can catch and handle exceptions thrown by downstream middleware or itself.

- **Parameters**: None.
- **Return value**: `Task` representing the asynchronous test.
- **Throws**: Propagates unhandled exceptions.

---
### `public void Clear_ShouldRemoveAllMiddleware()`

Verifies that calling `Clear` on the builder removes all previously registered middleware.

- **Parameters**: None.
- **Return value**: None.
- **Throws**: None.

---
### `public void Use_ShouldReturnBuilderForChaining()`

Checks that `Use` returns the builder instance to enable method chaining.

- **Parameters**: None.
- **Return value**: None.
- **Throws**: None.

---
### `public async Task Build_WithAsyncMiddleware_ShouldAwaitProperly()`

Validates that asynchronous middleware are awaited correctly during pipeline execution.

- **Parameters**: None.
- **Return value**: `Task` representing the asynchronous test.
- **Throws**: Propagates any exceptions thrown by the middleware.

---
### `public async Task Build_ComplexPipeline_WithLoggingAndErrorHandling()`

Tests a realistic pipeline with logging and error-handling middleware in sequence.

- **Parameters**: None.
- **Return value**: `Task` representing the asynchronous test.
- **Throws**: Propagates unhandled exceptions.

---
### `public async Task EventContext_ShouldHaveProperDefaults()`

Ensures that `EventContext` is initialized with expected default values when the pipeline starts.

- **Parameters**: None.
- **Return value**: `Task` representing the asynchronous test.
- **Throws**: Propagates any exceptions thrown during context initialization.

## Usage

### Example 1: Basic Pipeline with Logging and Retry
