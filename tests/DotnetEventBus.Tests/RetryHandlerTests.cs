using System;
using DotnetEventBus.Configuration;
using DotnetEventBus.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotnetEventBus.Tests;

public class RetryHandlerTests
{
    private static RetryHandler Create(EventBusOptions options)
        => new(NullLogger<RetryHandler>.Instance, options);

    [Fact]
    public void Constructor_WithDefaultOptions_DoesNotThrow()
    {
        var exception = Record.Exception(() => Create(new EventBusOptions()));
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RetryHandler(null!, new EventBusOptions()));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Create(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_MaxRetryAttemptsBelowOne_ThrowsArgumentOutOfRangeException(int attempts)
    {
        var options = new EventBusOptions { MaxRetryAttempts = attempts };

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Create(options));

        Assert.Contains("MaxRetryAttempts", ex.Message);
        Assert.Contains(attempts.ToString(), ex.Message);
    }

    [Fact]
    public void Constructor_MaxRetryAttemptsOfOne_DoesNotThrow()
    {
        var exception = Record.Exception(() => Create(new EventBusOptions { MaxRetryAttempts = 1 }));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-250)]
    public void Constructor_RetryDelayNotPositive_ThrowsArgumentOutOfRangeException(long milliseconds)
    {
        var options = new EventBusOptions { RetryDelay = TimeSpan.FromMilliseconds(milliseconds) };

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Create(options));

        Assert.Contains("RetryDelay", ex.Message);
        Assert.Contains(milliseconds.ToString(), ex.Message);
    }

    [Fact]
    public void Constructor_MaxRetryDelayBelowRetryDelay_ThrowsArgumentOutOfRangeException()
    {
        var options = new EventBusOptions
        {
            RetryDelay = TimeSpan.FromMilliseconds(500),
            MaxRetryDelay = TimeSpan.FromMilliseconds(200)
        };

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Create(options));

        Assert.Contains("MaxRetryDelay", ex.Message);
        Assert.Contains("200", ex.Message);
    }

    [Fact]
    public void Constructor_MaxRetryDelayEqualToRetryDelay_DoesNotThrow()
    {
        var options = new EventBusOptions
        {
            RetryDelay = TimeSpan.FromMilliseconds(500),
            MaxRetryDelay = TimeSpan.FromMilliseconds(500)
        };

        var exception = Record.Exception(() => Create(options));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(0.0)]
    [InlineData(-2.0)]
    [InlineData(double.NaN)]
    public void Constructor_RetryDelayMultiplierBelowOne_ThrowsArgumentOutOfRangeException(double multiplier)
    {
        var options = new EventBusOptions { RetryDelayMultiplier = multiplier };

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Create(options));

        Assert.Contains("RetryDelayMultiplier", ex.Message);
    }

    [Fact]
    public void Constructor_RetryDelayMultiplierOfOne_DoesNotThrow()
    {
        var exception = Record.Exception(() => Create(new EventBusOptions { RetryDelayMultiplier = 1.0 }));
        Assert.Null(exception);
    }

    [Fact]
    public void ComputeBackoffDelay_WithMaxRetryDelay_CapsDelayBeforeJitter()
    {
        // Base delay doubles each attempt; by attempt 4 the uncapped value (800ms) exceeds the 100ms cap.
        // Jitter is +/-25%, so the result must stay within [75ms, 125ms].
        var options = new EventBusOptions
        {
            RetryDelay = TimeSpan.FromMilliseconds(50),
            RetryDelayMultiplier = 2.0,
            MaxRetryDelay = TimeSpan.FromMilliseconds(100)
        };
        var handler = Create(options);

        for (int i = 0; i < 50; i++)
        {
            var delay = handler.ComputeBackoffDelay(4).TotalMilliseconds;
            Assert.InRange(delay, 75, 125);
        }
    }
}
