#nullable enable

using FluentAssertions;
using Xunit;
using DotnetEventBus.Integration;

namespace DotnetEventBus.Tests;

/// <summary>
/// Contains unit tests for the <see cref="CircuitBreaker"/> class.
/// Tests circuit breaker behavior including state transitions, failure handling,
/// and recovery mechanisms.
/// </summary>
public sealed class CircuitBreakerTests
{
    /// <summary>
    /// Tests that a successful operation completes normally and circuit remains closed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsync_WithSuccessfulOperation_ShouldReturnResult()
    {
        // Arrange
        var breaker = new CircuitBreaker(failureThreshold: 5);

        // Act
        var result = await breaker.ExecuteAsync(async () =>
        {
            return "success";
        });

        // Assert
        result.Should().Be("success");
        breaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    /// <summary>
    /// Tests that exceptions below the failure threshold don't open the circuit.
    /// The breaker should continue processing operations normally.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsync_WithExceptionsBelowThreshold_ShouldContinueProcessing()
    {
        // Arrange
        var breaker = new CircuitBreaker(failureThreshold: 5);
        var executionCount = 0;

        // Act
        for (int i = 0; i < 3; i++)
        {
            var act = () => breaker.ExecuteAsync(async () =>
            {
                executionCount++;
                throw new TimeoutException("Transient failure");
            });
            await act.Should().ThrowAsync<TimeoutException>();
        }

        // Assert
        breaker.State.Should().Be(CircuitBreakerState.Closed);
        executionCount.Should().Be(3);
    }

    /// <summary>
    /// Tests that exceeding the failure threshold transitions the circuit to the Open state.
    /// Once the threshold is exceeded, the breaker should stop processing operations.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsync_WhenExceptionsExceedThreshold_ShouldTransitionToOpen()
    {
        // Arrange
        var breaker = new CircuitBreaker(failureThreshold: 3, timeout: TimeSpan.FromSeconds(10));
        var executionCount = 0;

        // Act - Exceed failure threshold
        for (int i = 0; i < 3; i++)
        {
            var act = () => breaker.ExecuteAsync(async () =>
            {
                executionCount++;
                throw new TimeoutException("Failure");
            });
            await act.Should().ThrowAsync<TimeoutException>();
        }

        // Assert
        breaker.State.Should().Be(CircuitBreakerState.Open);
    }

    /// <summary>
    /// Tests that attempting to execute when the circuit is Open throws <see cref="CircuitBreakerOpenException"/>.
    /// This verifies that the breaker prevents operations when in the Open state.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsync_WhenCircuitIsOpen_ShouldThrowCircuitBreakerOpenException()
    {
        // Arrange
        var breaker = new CircuitBreaker(failureThreshold: 2, timeout: TimeSpan.FromSeconds(10));

        // Act - Open the circuit
        for (int i = 0; i < 2; i++)
        {
            var act = () => breaker.ExecuteAsync(async () =>
            {
                throw new TimeoutException("Failure");
            });
            await act.Should().ThrowAsync<TimeoutException>();
        }

        // Try to execute when open
        var openAct = () => breaker.ExecuteAsync(async () =>
        {
            return "should not execute";
        });

        // Assert
        await openAct.Should().ThrowAsync<CircuitBreakerOpenException>();
    }

    /// <summary>
    /// Tests that after the timeout expires, the circuit transitions to HalfOpen state.
    /// A successful operation in HalfOpen state should close the circuit again.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsync_AfterTimeoutExpires_ShouldTransitionToHalfOpen()
    {
        // Arrange
        var breaker = new CircuitBreaker(failureThreshold: 2, timeout: TimeSpan.FromMilliseconds(100));

        // Act - Open the circuit
        for (int i = 0; i < 2; i++)
        {
            var act = () => breaker.ExecuteAsync(async () =>
            {
                throw new TimeoutException("Failure");
            });
            await act.Should().ThrowAsync<TimeoutException>();
        }

        // Wait for timeout to expire
        await Task.Delay(150);

        // Try to execute again
        var result = await breaker.ExecuteAsync(async () =>
        {
            return "recovery attempt";
        });

        // Assert
        result.Should().Be("recovery attempt");
        breaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    /// <summary>
    /// Tests that a void operation (no return value) completes successfully when the circuit is closed.
    /// Verifies that both void and non-void operations are handled correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsync_VoidOperation_WithSuccess_ShouldCompleteSuccessfully()
    {
        // Arrange
        var breaker = new CircuitBreaker(failureThreshold: 5);
        var wasExecuted = false;

        // Act
        await breaker.ExecuteAsync(async () =>
        {
            wasExecuted = true;
            await Task.CompletedTask;
        });

        // Assert
        wasExecuted.Should().BeTrue();
        breaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    /// <summary>
    /// Tests that a void operation throws <see cref="CircuitBreakerOpenException"/> when the circuit is Open.
    /// Verifies that both void and non-void operations are protected by the circuit breaker.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsync_VoidOperation_WhenCircuitIsOpen_ShouldThrow()
    {
        // Arrange
        var breaker = new CircuitBreaker(failureThreshold: 2, timeout: TimeSpan.FromSeconds(10));

        // Act - Open the circuit
        for (int i = 0; i < 2; i++)
        {
            var act = () => breaker.ExecuteAsync(async () =>
            {
                throw new TimeoutException("Failure");
            });
            await act.Should().ThrowAsync<TimeoutException>();
        }

        // Try to execute void operation when open
        var openAct = () => breaker.ExecuteAsync(async () =>
        {
            await Task.CompletedTask;
        });

        // Assert
        await openAct.Should().ThrowAsync<CircuitBreakerOpenException>();
    }

    /// <summary>
    /// Tests that the constructor throws <see cref="ArgumentException"/> when an invalid failure threshold is provided.
    /// Validates that the failure threshold must be a positive integer.
    /// </summary>
    [Fact]
    public void Constructor_WithInvalidFailureThreshold_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => new CircuitBreaker(failureThreshold: 0);
        act.Should().Throw<ArgumentException>();

        var act2 = () => new CircuitBreaker(failureThreshold: -1);
        act2.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that passing a null operation to ExecuteAsync throws <see cref="ArgumentNullException"/>.
    /// Validates proper null checking for the operation parameter.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsync_WithNullOperation_ShouldThrowArgumentNullException()
    {
        // Arrange
        var breaker = new CircuitBreaker();

        // Act & Assert
        var act = () => breaker.ExecuteAsync((Func<Task<string>>?)null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that passing a null void operation to ExecuteAsync throws <see cref="ArgumentNullException"/>.
    /// Validates proper null checking for the void operation parameter.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsync_VoidWithNullOperation_ShouldThrowArgumentNullException()
    {
        // Arrange
        var breaker = new CircuitBreaker();

        // Act & Assert
        var act = () => breaker.ExecuteAsync((Func<Task>?)null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that a successful operation after some failures resets the circuit to Closed state.
    /// This verifies the circuit breaker's recovery mechanism works correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsync_SuccessfulOperationAfterFailures_ShouldResetCircuit()
    {
        // Arrange
        var breaker = new CircuitBreaker(failureThreshold: 3, timeout: TimeSpan.FromSeconds(10));
        var executionCount = 0;

        // Act - Cause two failures (below threshold)
        for (int i = 0; i < 2; i++)
        {
            var act = () => breaker.ExecuteAsync(async () =>
            {
                executionCount++;
                throw new TimeoutException("Failure");
            });
            await act.Should().ThrowAsync<TimeoutException>();
        }

        // Execute successful operation
        var result = await breaker.ExecuteAsync(async () =>
        {
            executionCount++;
            return "recovery";
        });

        // Assert
        result.Should().Be("recovery");
        breaker.State.Should().Be(CircuitBreakerState.Closed);
        executionCount.Should().Be(3);
    }

    /// <summary>
    /// Tests that a failure during HalfOpen state transitions the circuit back to Open state.
    /// This verifies the circuit breaker properly handles failures during the recovery attempt.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsync_HalfOpenToOpenTransition_OnFailure()
    {
        // Arrange
        var breaker = new CircuitBreaker(failureThreshold: 2, timeout: TimeSpan.FromMilliseconds(100));

        // Act - Open the circuit
        for (int i = 0; i < 2; i++)
        {
            var act = () => breaker.ExecuteAsync(async () =>
            {
                throw new TimeoutException("Failure");
            });
            await act.Should().ThrowAsync<TimeoutException>();
        }

        breaker.State.Should().Be(CircuitBreakerState.Open);

        // Wait for timeout and try to execute (should transition to half-open and succeed)
        await Task.Delay(150);

        var act2 = () => breaker.ExecuteAsync(async () =>
        {
            throw new TimeoutException("Still failing");
        });

        // Assert
        await act2.Should().ThrowAsync<TimeoutException>();
        // Circuit should reopen after failure in half-open state
        breaker.State.Should().Be(CircuitBreakerState.Open);
    }
}
