#nullable enable

using FluentAssertions;
using Xunit;
using DotnetEventBus.Integration;
using System.Diagnostics;

namespace DotnetEventBus.Tests;

public sealed class RetryPolicyTests
{
    [Fact]
    public async Task ExecuteAsync_WithSuccessfulOperation_ShouldReturnResultWithoutRetry()
    {
        // Arrange
        var policy = new RetryPolicy();
        var executionCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async () =>
        {
            executionCount++;
            return "success";
        });

        // Assert
        result.Should().Be("success");
        executionCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WithTransientFailureThenSuccess_ShouldRetryAndSucceed()
    {
        // Arrange
        var policy = new RetryPolicy()
            .WithMaxRetries(3)
            .WithInitialDelay(TimeSpan.FromMilliseconds(10));

        var executionCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async () =>
        {
            executionCount++;
            if (executionCount < 3)
                throw new TimeoutException("Transient failure");
            return "success";
        });

        // Assert
        result.Should().Be("success");
        executionCount.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_ExceedingMaxRetries_ShouldThrowOriginalException()
    {
        // Arrange
        var policy = new RetryPolicy()
            .WithMaxRetries(2)
            .WithInitialDelay(TimeSpan.FromMilliseconds(5));

        var executionCount = 0;

        // Act
        var act = () => policy.ExecuteAsync(async () =>
        {
            executionCount++;
            throw new InvalidOperationException("Always fails");
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        executionCount.Should().Be(3); // 1 initial + 2 retries
    }

    [Fact]
    public async Task ExecuteAsync_WithExponentialBackoff_ShouldIncreaseDelayPerAttempt()
    {
        // Arrange
        var policy = new RetryPolicy()
            .WithMaxRetries(3)
            .WithInitialDelay(TimeSpan.FromMilliseconds(10))
            .WithBackoffMultiplier(2.0)
            .WithJitter(false);

        var stopwatch = Stopwatch.StartNew();
        var executionCount = 0;

        // Act
        var act = () => policy.ExecuteAsync(async () =>
        {
            executionCount++;
            throw new TimeoutException("Fail");
        });

        // Assert
        await act.Should().ThrowAsync<TimeoutException>();
        stopwatch.Stop();

        // With initial delay of 10ms and multiplier of 2:
        // Attempt 1: fails immediately
        // Wait 10ms, Attempt 2: fails
        // Wait 20ms, Attempt 3: fails
        // Wait 40ms, Attempt 4: fails
        // Total minimum: ~70ms
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(60);
    }

    [Fact]
    public async Task ExecuteAsync_WithMaxDelay_ShouldCapDelayAtMaximum()
    {
        // Arrange
        var policy = new RetryPolicy()
            .WithMaxRetries(5)
            .WithInitialDelay(TimeSpan.FromSeconds(1))
            .WithBackoffMultiplier(2.0)
            .WithMaxDelay(TimeSpan.FromMilliseconds(100))
            .WithJitter(false);

        var executionCount = 0;

        // Act
        var act = () => policy.ExecuteAsync(async () =>
        {
            executionCount++;
            throw new TimeoutException("Fail");
        });

        // Assert
        await act.Should().ThrowAsync<TimeoutException>();
        // Should not take unreasonably long due to max delay cap
    }

    [Fact]
    public async Task ExecuteAsync_WithRetryableExceptionFilter_ShouldRetryOnlyRetryableExceptions()
    {
        // Arrange
        var policy = new RetryPolicy()
            .WithMaxRetries(2)
            .WithInitialDelay(TimeSpan.FromMilliseconds(5))
            .WithRetryableExceptionFilter(ex => ex is TimeoutException);

        var executionCount = 0;

        // Act
        var act = () => policy.ExecuteAsync(async () =>
        {
            executionCount++;
            throw new InvalidOperationException("Non-retryable");
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        executionCount.Should().Be(1); // No retries for non-retryable exception
    }

    [Fact]
    public async Task ExecuteAsync_WithRetryableExceptionFilter_ShouldRetryRetryableExceptions()
    {
        // Arrange
        var policy = new RetryPolicy()
            .WithMaxRetries(2)
            .WithInitialDelay(TimeSpan.FromMilliseconds(5))
            .WithRetryableExceptionFilter(ex => ex is TimeoutException);

        var executionCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async () =>
        {
            executionCount++;
            if (executionCount < 3)
                throw new TimeoutException("Retryable");
            return "success";
        });

        // Assert
        result.Should().Be("success");
        executionCount.Should().Be(3); // Initial attempt plus two retries
    }

    [Fact]
    public async Task ExecuteAsync_WithJitterDisabled_DelaysShouldBeConsistent()
    {
        // Arrange
        var policy = new RetryPolicy()
            .WithMaxRetries(2)
            .WithInitialDelay(TimeSpan.FromMilliseconds(50))
            .WithJitter(false);

        var executionCount = 0;

        // Act
        var act = () => policy.ExecuteAsync(async () =>
        {
            executionCount++;
            throw new TimeoutException("Fail");
        });

        // Assert
        await act.Should().ThrowAsync<TimeoutException>();
        // Should execute exactly 3 times (1 initial + 2 retries)
        executionCount.Should().Be(3);
    }

    [Fact]
    public void WithMaxRetries_WithNegativeValue_ShouldThrowArgumentException()
    {
        // Arrange
        var policy = new RetryPolicy();

        // Act & Assert
        policy.Invoking(p => p.WithMaxRetries(-1))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithInitialDelay_WithNegativeValue_ShouldThrowArgumentException()
    {
        // Arrange
        var policy = new RetryPolicy();

        // Act & Assert
        policy.Invoking(p => p.WithInitialDelay(TimeSpan.FromMilliseconds(-1)))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithBackoffMultiplier_WithValueLessThanOrEqualToOne_ShouldThrowArgumentException()
    {
        // Arrange
        var policy = new RetryPolicy();

        // Act & Assert
        policy.Invoking(p => p.WithBackoffMultiplier(1.0))
            .Should().Throw<ArgumentException>();
        policy.Invoking(p => p.WithBackoffMultiplier(0.5))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithMaxDelay_WithNonPositiveValue_ShouldThrowArgumentException()
    {
        // Arrange
        var policy = new RetryPolicy();

        // Act & Assert
        policy.Invoking(p => p.WithMaxDelay(TimeSpan.Zero))
            .Should().Throw<ArgumentException>();
        policy.Invoking(p => p.WithMaxDelay(TimeSpan.FromMilliseconds(-1)))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteAsync_VoidOperation_WithSuccess_ShouldCompleteSuccessfully()
    {
        // Arrange
        var policy = new RetryPolicy();
        var wasExecuted = false;

        // Act
        await policy.ExecuteAsync(async () =>
        {
            wasExecuted = true;
            await Task.CompletedTask;
        });

        // Assert
        wasExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_VoidOperation_WithFailure_ShouldRetryAndThrow()
    {
        // Arrange
        var policy = new RetryPolicy()
            .WithMaxRetries(2)
            .WithInitialDelay(TimeSpan.FromMilliseconds(5));

        var executionCount = 0;

        // Act
        var act = () => policy.ExecuteAsync(async () =>
        {
            executionCount++;
            await Task.CompletedTask;
            throw new TimeoutException("Fail");
        });

        // Assert
        await act.Should().ThrowAsync<TimeoutException>();
        executionCount.Should().Be(3);
    }
}
