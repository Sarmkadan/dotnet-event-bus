// -----------------------------------------------------------------------------
// Tests for ErrorHandlingMiddleware
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotnetEventBus.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotnetEventBus.Tests;

public class ErrorHandlingMiddlewareTests
{
    // Helper to create a minimal EventContext – the real type in the library
    // exposes the members used by the middleware (IsProcessed, ProcessingException,
    // Metadata and EventType). If the library adds a constructor, the default
    // parameter‑less constructor will still work because all properties are set
    // via object initializer.
    private static EventContext CreateContext()
    {
        return new EventContext
        {
            EventType = "TestEvent",
            Metadata = new Dictionary<string, object>()
        };
    }

    [Fact]
    public async Task Create_Should_ProcessSuccessfully_When_NextCompletes()
    {
        // Arrange
        var logger = NullLogger<ErrorHandlingMiddleware>.Instance;
        var middleware = new ErrorHandlingMiddleware(logger);
        var context = CreateContext();

        EventBusMiddleware next = _ => Task.CompletedTask;

        var pipeline = middleware.Create(next);

        // Act
        await pipeline(context);

        // Assert
        Assert.True(context.IsProcessed);
        Assert.Null(context.ProcessingException);
    }

    [Fact]
    public async Task Create_Should_Retry_OnTransientFailures_And_Succeed()
    {
        // Arrange
        var logger = NullLogger<ErrorHandlingMiddleware>.Instance;
        var middleware = new ErrorHandlingMiddleware(logger, maxRetries: 3, retryDelay: TimeSpan.Zero);
        var context = CreateContext();

        int callCount = 0;
        EventBusMiddleware next = _ =>
        {
            callCount++;
            if (callCount < 3) // fail first two attempts
                throw new InvalidOperationException("Transient failure");
            return Task.CompletedTask;
        };

        var pipeline = middleware.Create(next);

        // Act
        await pipeline(context);

        // Assert
        Assert.True(context.IsProcessed);
        Assert.Equal(3, callCount);
        Assert.True(context.Metadata.ContainsKey("attempt"));
        Assert.Equal(3, context.Metadata["attempt"]);
    }

    [Fact]
    public async Task Create_Should_ThrowEventProcessingException_When_AllRetriesFail_And_NoHandler()
    {
        // Arrange
        var logger = NullLogger<ErrorHandlingMiddleware>.Instance;
        var middleware = new ErrorHandlingMiddleware(logger, maxRetries: 2, retryDelay: TimeSpan.Zero);
        var context = CreateContext();

        EventBusMiddleware next = _ => throw new InvalidOperationException("Always fails");

        var pipeline = middleware.Create(next);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<EventProcessingException>(async () => await pipeline(context));
        Assert.False(context.IsProcessed);
        Assert.Contains("failed after 2 retries", ex.Message);
    }

    [Fact]
    public async Task Create_Should_InvokeCustomErrorHandler_And_MarkAsRecovered()
    {
        // Arrange
        var logger = NullLogger<ErrorHandlingMiddleware>.Instance;
        var middleware = new ErrorHandlingMiddleware(
            logger,
            maxRetries: 1,
            retryDelay: TimeSpan.Zero,
            errorHandler: async (ctx, exc) =>
            {
                // simulate handling and returning true (recovered)
                await Task.Yield();
                return true;
            });

        var context = CreateContext();

        EventBusMiddleware next = _ => throw new InvalidOperationException("Failure");

        var pipeline = middleware.Create(next);

        // Act
        await pipeline(context);

        // Assert
        Assert.True(context.IsProcessed);
        Assert.True(context.Metadata.ContainsKey("recoveredByHandler"));
        Assert.True((bool)context.Metadata["recoveredByHandler"]);
    }

    [Fact]
    public void Constructor_Should_ThrowArgumentNullException_When_LoggerIsNull()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ErrorHandlingMiddleware(null!));
    }

    [Fact]
    public async Task Create_Should_NotRetry_When_MaxRetriesIsZero_And_HandlerRecovers()
    {
        // Arrange
        var logger = NullLogger<ErrorHandlingMiddleware>.Instance;
        var middleware = new ErrorHandlingMiddleware(
            logger,
            maxRetries: 0,
            retryDelay: TimeSpan.Zero,
            errorHandler: async (ctx, exc) =>
            {
                await Task.Yield();
                return true;
            });

        var context = CreateContext();

        int callCount = 0;
        EventBusMiddleware next = _ =>
        {
            callCount++;
            throw new InvalidOperationException("Failure");
        };

        var pipeline = middleware.Create(next);

        // Act
        await pipeline(context);

        // Assert
        Assert.Equal(0, callCount); // loop never entered, handler called directly
        Assert.True(context.IsProcessed);
        Assert.True((bool)context.Metadata["recoveredByHandler"]);
    }
}
