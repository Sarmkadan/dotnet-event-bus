using System;
using System.Collections.Generic;
using DotnetEventBus.Configuration;
using DotnetEventBus.Exceptions;
using Xunit;

namespace DotnetEventBus.Tests;

public class EventBusOptionsTests
{
    [Fact]
    public void Validate_WithDefaultValues_DoesNotThrow()
    {
        var options = new EventBusOptions();
        var exception = Record.Exception(() => options.Validate());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_RequestTimeoutZeroOrNegative_ThrowsValidationException(long milliseconds)
    {
        var options = new EventBusOptions
        {
            RequestTimeout = TimeSpan.FromMilliseconds(milliseconds)
        };

        Assert.Throws<ValidationException>(() => options.Validate());
    }

    [Fact]
    public void Validate_NegativeMaxRetryAttempts_ThrowsValidationException()
    {
        var options = new EventBusOptions
        {
            MaxRetryAttempts = -5
        };

        Assert.Throws<ValidationException>(() => options.Validate());
    }

    [Fact]
    public void Validate_NegativeRetryDelay_ThrowsValidationException()
    {
        var options = new EventBusOptions
        {
            RetryDelay = TimeSpan.FromMilliseconds(-10)
        };

        Assert.Throws<ValidationException>(() => options.Validate());
    }

    [Fact]
    public void Validate_RetryDelayMultiplierLessThanOne_ThrowsValidationException()
    {
        var options = new EventBusOptions
        {
            RetryDelayMultiplier = 0.9
        };

        Assert.Throws<ValidationException>(() => options.Validate());
    }

    [Fact]
    public void Validate_MaxConcurrentHandlersLessThanOne_ThrowsValidationException()
    {
        var options = new EventBusOptions
        {
            MaxConcurrentHandlers = 0
        };

        Assert.Throws<ValidationException>(() => options.Validate());
    }

    [Fact]
    public void Validate_DistributedWithoutTransportType_ThrowsValidationException()
    {
        var options = new EventBusOptions
        {
            IsDistributed = true,
            DistributedTransportType = null
        };

        Assert.Throws<ValidationException>(() => options.Validate());
    }

    [Fact]
    public void Validate_MaxDeadLetterEntriesLessThanOne_ThrowsValidationException()
    {
        var options = new EventBusOptions
        {
            MaxDeadLetterEntries = 0
        };

        Assert.Throws<ValidationException>(() => options.Validate());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    public void CalculateRetryDelay_NegativeAttempt_ThrowsArgumentException(int attempt)
    {
        var options = new EventBusOptions();
        Assert.Throws<ArgumentException>(() => options.CalculateRetryDelay(attempt));
    }

    [Fact]
    public void CalculateRetryDelay_ExponentialBackoff_RespectsMaxRetryDelay()
    {
        var options = new EventBusOptions
        {
            RetryDelay = TimeSpan.FromSeconds(1),
            RetryDelayMultiplier = 2.0,
            MaxRetryDelay = TimeSpan.FromSeconds(5)
        };

        // attempt 0 => 1 sec
        Assert.Equal(TimeSpan.FromSeconds(1), options.CalculateRetryDelay(0));

        // attempt 1 => 2 sec
        Assert.Equal(TimeSpan.FromSeconds(2), options.CalculateRetryDelay(1));

        // attempt 2 => 4 sec
        Assert.Equal(TimeSpan.FromSeconds(4), options.CalculateRetryDelay(2));

        // attempt 3 => 8 sec, but capped at MaxRetryDelay (5 sec)
        Assert.Equal(TimeSpan.FromSeconds(5), options.CalculateRetryDelay(3));
    }

    [Fact]
    public void Clone_CopiesAllProperties_And_IsIndependent()
    {
        var original = new EventBusOptions
        {
            DefaultHandlerTimeout = TimeSpan.FromSeconds(10),
            MaxRetryAttempts = 7,
            RetryDelay = TimeSpan.FromMilliseconds(200),
            RetryDelayMultiplier = 3.0,
            MaxRetryDelay = TimeSpan.FromSeconds(20),
            AllowParallelHandling = false,
            MaxConcurrentHandlers = 2,
            EnableDeadLetterQueue = false,
            ThrowOnHandlerFailure = true,
            ThrowOnNoHandlers = true,
            DeadLetterOnNoHandlers = true,
            MaxDeadLetterEntries = 42,
            IsDistributed = true,
            DistributedTransportType = "Kafka",
            DistributedTransportConnectionString = "Endpoint=localhost;Port=9092",
            RequestTimeout = TimeSpan.FromSeconds(15)
        };
        original.MiddlewareTypes.Add(typeof(string));
        original.MiddlewareTypes.Add(typeof(int));

        var clone = original.Clone();

        // Verify scalar properties
        Assert.Equal(original.DefaultHandlerTimeout, clone.DefaultHandlerTimeout);
        Assert.Equal(original.MaxRetryAttempts, clone.MaxRetryAttempts);
        Assert.Equal(original.RetryDelay, clone.RetryDelay);
        Assert.Equal(original.RetryDelayMultiplier, clone.RetryDelayMultiplier);
        Assert.Equal(original.MaxRetryDelay, clone.MaxRetryDelay);
        Assert.Equal(original.AllowParallelHandling, clone.AllowParallelHandling);
        Assert.Equal(original.MaxConcurrentHandlers, clone.MaxConcurrentHandlers);
        Assert.Equal(original.EnableDeadLetterQueue, clone.EnableDeadLetterQueue);
        Assert.Equal(original.ThrowOnHandlerFailure, clone.ThrowOnHandlerFailure);
        Assert.Equal(original.ThrowOnNoHandlers, clone.ThrowOnNoHandlers);
        Assert.Equal(original.DeadLetterOnNoHandlers, clone.DeadLetterOnNoHandlers);
        Assert.Equal(original.MaxDeadLetterEntries, clone.MaxDeadLetterEntries);
        Assert.Equal(original.IsDistributed, clone.IsDistributed);
        Assert.Equal(original.DistributedTransportType, clone.DistributedTransportType);
        Assert.Equal(original.DistributedTransportConnectionString, clone.DistributedTransportConnectionString);
        Assert.Equal(original.RequestTimeout, clone.RequestTimeout);

        // Verify collection copy
        Assert.Equal(original.MiddlewareTypes, clone.MiddlewareTypes);
        Assert.NotSame(original.MiddlewareTypes, clone.MiddlewareTypes);

        // Mutate original and ensure clone is unaffected
        original.MiddlewareTypes.Clear();
        Assert.Empty(original.MiddlewareTypes);
        Assert.NotEmpty(clone.MiddlewareTypes);
    }
}
