# RetryPolicyTests

Unit tests for the `RetryPolicy` class, verifying retry behavior, delay strategies, exception handling, and configuration validation in asynchronous and void operation scenarios.

## API

### `ExecuteAsync_WithSuccessfulOperation_ShouldReturnResultWithoutRetry`
Verifies that a successful operation completes without any retry attempts. The test asserts that the operation result is returned immediately and no retry logic is invoked.

### `ExecuteAsync_WithTransientFailureThenSuccess_ShouldRetryAndSucceed`
Ensures that a transient failure followed by success triggers the expected number of retry attempts before succeeding. The test validates that the operation eventually returns the expected result after retries.

### `ExecuteAsync_ExceedingMaxRetries_ShouldThrowOriginalException`
Confirms that when the maximum retry count is exceeded, the original exception is propagated to the caller. The test asserts that the exception thrown matches the original failure and no additional retries occur beyond the configured limit.

### `ExecuteAsync_WithExponentialBackoff_ShouldIncreaseDelayPerAttempt`
Validates that the retry delay increases exponentially with each attempt. The test checks that the delay between retries follows the expected backoff pattern based on the configured multiplier.

### `ExecuteAsync_WithMaxDelay_ShouldCapDelayAtMaximum`
Ensures that the retry delay does not exceed the specified maximum delay, even if the exponential backoff calculation would otherwise exceed it. The test verifies that delays are capped at the configured maximum value.

### `ExecuteAsync_WithRetryableExceptionFilter_ShouldRetryOnlyRetryableExceptions`
Tests that only exceptions matching the retryable filter are retried. The test asserts that non-retryable exceptions are not retried and propagate immediately.

### `ExecuteAsync_WithRetryableExceptionFilter_ShouldRetryRetryableExceptions`
Ensures that exceptions explicitly marked as retryable are retried according to the policy. The test validates that retryable exceptions trigger the expected retry behavior.

### `ExecuteAsync_WithJitterDisabled_DelaysShouldBeConsistent`
Confirms that when jitter is disabled, retry delays are consistent and deterministic across retries. The test asserts that the delay between retries matches the calculated backoff value without variation.

### `WithMaxRetries_WithNegativeValue_ShouldThrowArgumentException`
Validates that configuring a negative maximum retry count throws an `ArgumentException`. The test asserts that the exception message is meaningful and the configuration is rejected.

### `WithInitialDelay_WithNegativeValue_ShouldThrowArgumentException`
Ensures that setting a negative initial delay throws an `ArgumentException`. The test verifies that the configuration is rejected with an appropriate error message.

### `WithBackoffMultiplier_WithValueLessThanOrEqualToOne_ShouldThrowArgumentException`
Confirms that a backoff multiplier less than or equal to one throws an `ArgumentException`. The test asserts that invalid multiplier values are rejected during policy configuration.

### `WithMaxDelay_WithNonPositiveValue_ShouldThrowArgumentException`
Validates that configuring a non-positive maximum delay throws an `ArgumentException`. The test ensures that invalid delay values are rejected with a descriptive error message.

### `ExecuteAsync_VoidOperation_WithSuccess_ShouldCompleteSuccessfully`
Tests that a successful void operation completes without retry attempts. The test asserts that the operation finishes successfully and no retry logic is triggered.

### `ExecuteAsync_VoidOperation_WithFailure_ShouldRetryAndThrow`
Ensures that a failing void operation retries according to the policy before propagating the exception. The test verifies that the operation is retried the expected number of times before the final exception is thrown.

## Usage

### Example 1: Basic Retry with Exponential Backoff
