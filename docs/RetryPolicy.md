# RetryPolicy

A `RetryPolicy` in `dotnet-event-bus` provides configurable retry behavior for asynchronous operations, supporting exponential backoff, linear backoff, immediate retries, and customizable jitter. It encapsulates retry logic for transient failures, allowing fine-grained control over delay strategies, retry limits, and exception filtering.

## API

### `WithMaxRetries`
Configures the maximum number of retry attempts (excluding the initial attempt). The default is `3`.

- **Parameters**
  - `maxRetries` (int): The maximum number of retry attempts. Must be non-negative.
- **Return Value**
  - Returns a new `RetryPolicy` instance with the updated retry limit.
- **Throws**
  - `ArgumentOutOfRangeException`: If `maxRetries` is negative.

### `WithInitialDelay`
Sets the initial delay duration before the first retry attempt.

- **Parameters**
  - `initialDelay` (TimeSpan): The initial delay duration. Must be non-negative.
- **Return Value**
  - Returns a new `RetryPolicy` instance with the updated initial delay.
- **Throws**
  - `ArgumentOutOfRangeException`: If `initialDelay` is negative.

### `WithBackoffMultiplier`
Adjusts the multiplier applied to the delay between retry attempts in exponential backoff strategies.

- **Parameters**
  - `multiplier` (double): The multiplier value. Must be greater than or equal to `1.0`.
- **Return Value**
  - Returns a new `RetryPolicy` instance with the updated multiplier.
- **Throws**
  - `ArgumentOutOfRangeException`: If `multiplier` is less than `1.0`.

### `WithMaxDelay`
Defines the maximum delay duration that any retry attempt will wait, regardless of backoff calculations.

- **Parameters**
  - `maxDelay` (TimeSpan): The maximum delay duration. Must be non-negative.
- **Return Value**
  - Returns a new `RetryPolicy` instance with the updated maximum delay.
- **Throws**
  - `ArgumentOutOfRangeException`: If `maxDelay` is negative.

### `WithJitter`
Enables or disables random jitter in delay calculations to avoid thundering herds.

- **Parameters**
  - `enabled` (bool): `true` to enable jitter; `false` to disable.
- **Return Value**
  - Returns a new `RetryPolicy` instance with the updated jitter setting.

### `WithRetryableExceptionFilter`
Specifies a predicate to determine which exceptions should trigger a retry.

- **Parameters**
  - `filter` (Func<Exception, bool>): A function that returns `true` if the exception is retryable.
- **Return Value**
  - Returns a new `RetryPolicy` instance with the updated filter.
- **Throws**
  - `ArgumentNullException`: If `filter` is `null`.

### `ExecuteAsync<T>`
Executes an asynchronous operation with retry logic and returns a result of type `T`.

- **Parameters**
  - `action` (Func<Task<T>>): The asynchronous operation to execute.
- **Return Value**
  - Returns a `Task<T>` that completes with the result of the operation or throws after all retries are exhausted.
- **Throws**
  - `ArgumentNullException`: If `action` is `null`.
  - `RetryFailedException`: If all retry attempts are exhausted without success.

### `ExecuteAsync`
Executes an asynchronous operation with retry logic without returning a result.

- **Parameters**
  - `action` (Func<Task>): The asynchronous operation to execute.
- **Return Value**
  - Returns a `Task` that completes when the operation succeeds or throws after all retries are exhausted.
- **Throws**
  - `ArgumentNullException`: If `action` is `null`.
  - `RetryFailedException`: If all retry attempts are exhausted without success.

### `CreateDefault`
Creates a `RetryPolicy` with default settings: exponential backoff with a maximum of 3 retries, an initial delay of 1 second, a backoff multiplier of 2, a maximum delay of 30 seconds, and jitter enabled.

- **Return Value**
  - Returns a new `RetryPolicy` instance configured with default values.

### `CreateExponentialBackoff`
Creates a `RetryPolicy` using an exponential backoff strategy with customizable parameters.

- **Parameters**
  - `maxRetries` (int): Maximum number of retry attempts.
  - `initialDelay` (TimeSpan): Initial delay duration.
  - `multiplier` (double): Backoff multiplier.
  - `maxDelay` (TimeSpan): Maximum delay duration.
  - `enableJitter` (bool): Whether to enable jitter.
- **Return Value**
  - Returns a new `RetryPolicy` instance configured with exponential backoff.
- **Throws**
  - `ArgumentOutOfRangeException`: If `maxRetries` is negative, `initialDelay` or `maxDelay` is negative, or `multiplier` is less than `1.0`.

### `CreateLinearBackoff`
Creates a `RetryPolicy` using a linear backoff strategy with customizable parameters.

- **Parameters**
  - `maxRetries` (int): Maximum number of retry attempts.
  - `initialDelay` (TimeSpan): Initial delay duration.
  - `delayIncrement` (TimeSpan): Fixed increment added to the delay after each retry.
  - `maxDelay` (TimeSpan): Maximum delay duration.
  - `enableJitter` (bool): Whether to enable jitter.
- **Return Value**
  - Returns a new `RetryPolicy` instance configured with linear backoff.
- **Throws**
  - `ArgumentOutOfRangeException`: If `maxRetries` is negative, `initialDelay`, `delayIncrement`, or `maxDelay` is negative.

### `CreateImmediate`
Creates a `RetryPolicy` that retries immediately without any delay between attempts.

- **Parameters**
  - `maxRetries` (int): Maximum number of retry attempts.
- **Return Value**
  - Returns a new `RetryPolicy` instance configured for immediate retries.
- **Throws**
  - `ArgumentOutOfRangeException`: If `maxRetries` is negative.

## Usage

### Example 1: Exponential Backoff with Custom Settings
