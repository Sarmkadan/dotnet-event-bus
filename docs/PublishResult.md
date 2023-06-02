# PublishResult

A lightweight result object returned by the `IEventBus.PublishAsync` method that captures the outcome of a single publish operation, including success/failure state, timing, and per-handler results.

## API

### Properties

- **`MessageId`** `string`
  The unique identifier of the message being published. This value is set once during construction and never modified.

- **`Success`** `bool`
  Indicates whether the entire publish operation succeeded. `true` only if no handlers failed and no exceptions were thrown; otherwise `false`.

- **`HandlersInvoked`** `int`
  The total number of handlers that were attempted during the publish. This count is incremented each time a handler is invoked, regardless of success or failure.

- **`FailedHandlers`** `int`
  The number of handlers that failed during the publish. This value is derived from the length of `FailedHandlerNames`.

- **`ErrorMessage`** `string?`
  A human-readable error message summarizing the reason for failure. `null` when `Success` is `true`.

- **`Exception`** `Exception?`
  The exception thrown by a handler, if any. `null` when `Success` is `true`.

- **`ElapsedTime`** `TimeSpan`
  The wall-clock duration from the start of the publish operation until completion (success or failure).

- **`SuccessfulHandlers`** `List<string>`
  The names of all handlers that completed successfully. Populated by calls to `AddSuccessfulHandler`.

- **`FailedHandlerNames`** `List<string>`
  The names of all handlers that threw an exception or otherwise failed. Populated by calls to `AddFailedHandler`.

### Constructors

- **`PublishResult`** `()`
  Initializes a new `PublishResult` with default values: `Success = false`, `HandlersInvoked = 0`, `FailedHandlers = 0`, empty lists, `ElapsedTime = TimeSpan.Zero`, and `null` for `ErrorMessage` and `Exception`.

### Methods

- **`MarkSuccess`** `void`
  Sets `Success = true`, clears any existing `ErrorMessage` and `Exception`, and ensures `ElapsedTime` reflects the total duration. Should be called only once per instance when the operation completes without failures.

- **`AddFailedHandler`** `(string handlerName)`
  Increments `HandlersInvoked`, increments `FailedHandlers`, adds `handlerName` to `FailedHandlerNames`, and captures the thrown exception (if any) into `Exception` and `ErrorMessage`. Throws `ArgumentNullException` if `handlerName` is `null`.

- **`AddSuccessfulHandler`** `(string handlerName)`
  Increments `HandlersInvoked`, adds `handlerName` to `SuccessfulHandlers`, and updates `Success` to `true` only if no failures have been recorded so far. Throws `ArgumentNullException` if `handlerName` is `null`.

- **`GetSummary`** `string`
  Returns a concise, multi-line string containing: `MessageId`, `Success`, `ElapsedTime`, `HandlersInvoked`, `FailedHandlers`, `ErrorMessage` (if present), and the counts of `SuccessfulHandlers` and `FailedHandlerNames`. Never throws.

- **`CreateFailed`** `static PublishResult (string messageId, Exception exception, string errorMessage)`
  Constructs and returns a `PublishResult` representing a failed publish. Sets `Success = false`, `ErrorMessage = errorMessage`, `Exception = exception`, and `MessageId = messageId`. The `ElapsedTime` is initialized to `TimeSpan.Zero`; it should be set by the caller after timing the operation.

- **`CreateSuccess`** `static PublishResult (string messageId)`
  Constructs and returns a `PublishResult` representing a successful publish. Sets `Success = true`, `MessageId = messageId`, and initializes all other fields to their default or empty states. The `ElapsedTime` is initialized to `TimeSpan.Zero`; it should be set by the caller after timing the operation.

## Usage
