#nullable enable

using FluentAssertions;
using Xunit;
using DotnetEventBus.Middleware;

namespace DotnetEventBus.Tests;

/// <summary>
/// Provides unit tests for the <see cref="PipelineBuilder"/> class to verify middleware pipeline construction and behavior.
/// Tests cover middleware registration, execution order, context manipulation, error handling, and pipeline building.
/// </summary>
public sealed class PipelineBuilderTests
{
    /// <summary>
    /// Tests that using a null middleware throws an <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void Use_WithNullMiddleware_ShouldThrowArgumentNullException()
    {
        // Arrange
        var builder = new PipelineBuilder();

        // Act & Assert
        builder.Invoking(b => b.Use(null!))
            .Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that building a pipeline without any middleware returns a valid, non-null pipeline.
    /// </summary>
    [Fact]
    public void Build_WithoutMiddleware_ShouldReturnValidPipeline()
    {
        // Arrange
        var builder = new PipelineBuilder();

        // Act
        var pipeline = builder.Build();

        // Assert
        pipeline.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that a pipeline with a single middleware executes that middleware when invoked.
    /// </summary>
    [Fact]
    public async Task Build_WithSingleMiddleware_ShouldExecuteMiddleware()
    {
        // Arrange
        var builder = new PipelineBuilder();
        var middlewareExecuted = false;

        builder.Use(next => async context =>
        {
            middlewareExecuted = true;
            await next(context);
        });

        var pipeline = builder.Build();
        var context = new EventContext { EventType = "TestEvent", EventData = "test" };

        // Act
        await pipeline(context);

        // Assert
        middlewareExecuted.Should().BeTrue();
    }

    /// <summary>
    /// Tests that multiple middleware components execute in the order they were added (FIFO - first in, first out).
    /// Verifies that middleware start and end handlers are called in the correct sequence.
    /// </summary>
    [Fact]
    public async Task Build_WithMultipleMiddleware_ShouldExecuteInOrder()
    {
        // Arrange
        var builder = new PipelineBuilder();
        var executionOrder = new List<string>();

        builder.Use(next => async context =>
        {
            executionOrder.Add("FirstStart");
            await next(context);
            executionOrder.Add("FirstEnd");
        });

        builder.Use(next => async context =>
        {
            executionOrder.Add("SecondStart");
            await next(context);
            executionOrder.Add("SecondEnd");
        });

        var pipeline = builder.Build();
        var context = new EventContext { EventType = "TestEvent", EventData = "test" };

        // Act
        await pipeline(context);

        // Assert
        executionOrder.Should().ContainInOrder(
            "FirstStart", "SecondStart", "SecondEnd", "FirstEnd"
        );
    }

    /// <summary>
    /// Tests that middleware can modify the event context by adding metadata during processing.
    /// </summary>
    [Fact]
    public async Task Build_MiddlewareCanModifyContext()
    {
        // Arrange
        var builder = new PipelineBuilder();

        builder.Use(next => async context =>
        {
            context.Metadata["processed"] = true;
            await next(context);
        });

        var pipeline = builder.Build();
        var context = new EventContext { EventType = "TestEvent", EventData = "test" };

        // Act
        await pipeline(context);

        // Assert
        context.Metadata.Should().ContainKey("processed");
        context.Metadata["processed"].Should().Be(true);
    }

    /// <summary>
    /// Tests that middleware can short-circuit the pipeline by not calling the next delegate.
    /// Verifies that subsequent middleware are not executed when a middleware short-circuits.
    /// </summary>
    [Fact]
    public async Task Build_MiddlewareCanShortCircuit()
    {
        // Arrange
        var builder = new PipelineBuilder();
        var secondMiddlewareExecuted = false;

        builder.Use(next => async context =>
        {
            context.IsProcessed = true;
            // Don't call next - short-circuit
            await Task.CompletedTask;
        });

        builder.Use(next => async context =>
        {
            secondMiddlewareExecuted = true;
            await next(context);
        });

        var pipeline = builder.Build();
        var context = new EventContext { EventType = "TestEvent", EventData = "test" };

        // Act
        await pipeline(context);

        // Assert
        secondMiddlewareExecuted.Should().BeFalse();
        context.IsProcessed.Should().BeTrue();
    }

    /// <summary>
    /// Tests that middleware can handle exceptions thrown by subsequent middleware in the pipeline.
    /// Verifies that exceptions are properly caught and handled within the middleware chain.
    /// </summary>
    [Fact]
    public async Task Build_MiddlewareCanHandleExceptions()
    {
        // Arrange
        var builder = new PipelineBuilder();
        var exceptionHandled = false;

        builder.Use(next => async context =>
        {
            try
            {
                await next(context);
            }
            catch (InvalidOperationException)
            {
                exceptionHandled = true;
            }
        });

        builder.Use(next => async context =>
        {
            throw new InvalidOperationException("Error in middleware");
        });

        var pipeline = builder.Build();
        var context = new EventContext { EventType = "TestEvent", EventData = "test" };

        // Act
        await pipeline(context);

        // Assert
        exceptionHandled.Should().BeTrue();
    }

    /// <summary>
    /// Tests that calling Clear() removes all registered middleware from the builder.
    /// Verifies that the pipeline can still be built and executed after clearing.
    /// </summary>
    [Fact]
    public void Clear_ShouldRemoveAllMiddleware()
    {
        // Arrange
        var builder = new PipelineBuilder();
        builder.Use(next => async context => await next(context));
        builder.Use(next => async context => await next(context));

        // Act
        builder.Clear();
        var pipeline = builder.Build();

        // Assert
        pipeline.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the Use() method returns the builder instance for method chaining.
    /// </summary>
    [Fact]
    public void Use_ShouldReturnBuilderForChaining()
    {
        // Arrange
        var builder = new PipelineBuilder();

        // Act
        var result = builder.Use(next => async context => await next(context));

        // Assert
        result.Should().BeSameAs(builder);
    }

    /// <summary>
    /// Tests that async middleware with delays are properly awaited and executed.
    /// Verifies that asynchronous operations within middleware are completed before continuing.
    /// </summary>
    [Fact]
    public async Task Build_WithAsyncMiddleware_ShouldAwaitProperly()
    {
        // Arrange
        var builder = new PipelineBuilder();
        var delayExecuted = false;

        builder.Use(next => async context =>
        {
            await Task.Delay(10);
            delayExecuted = true;
            await next(context);
        });

        var pipeline = builder.Build();
        var context = new EventContext { EventType = "TestEvent", EventData = "test" };

        // Act
        await pipeline(context);

        // Assert
        delayExecuted.Should().BeTrue();
    }

    /// <summary>
    /// Tests a complex pipeline combining logging, error handling, and processing middleware.
    /// Verifies that middleware can work together to provide cross-cutting concerns like logging and error handling.
    /// </summary>
    [Fact]
    public async Task Build_ComplexPipeline_WithLoggingAndErrorHandling()
    {
        // Arrange
        var builder = new PipelineBuilder();
        var logs = new List<string>();

        // Logging middleware
        builder.Use(next => async context =>
        {
            logs.Add($"Start: {context.EventType}");
            await next(context);
            logs.Add($"End: {context.EventType}");
        });

        // Error handling middleware
        builder.Use(next => async context =>
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logs.Add($"Error: {ex.Message}");
                context.ProcessingException = ex;
            }
        });

        // Processing middleware
        builder.Use(next => async context =>
        {
            context.IsProcessed = true;
            await next(context);
        });

        var pipeline = builder.Build();
        var context = new EventContext { EventType = "OrderPlaced", EventData = new { OrderId = 123 } };

        // Act
        await pipeline(context);

        // Assert
        context.IsProcessed.Should().BeTrue();
        logs.Should().Contain("Start: OrderPlaced");
        logs.Should().Contain("End: OrderPlaced");
    }

    /// <summary>
    /// Tests that the <see cref="EventContext"/> has proper default values when initialized.
    /// Verifies that metadata is initialized as an empty dictionary, timestamps are set correctly,
    /// and processing flags are in their initial state.
    /// </summary>
    [Fact]
    public async Task EventContext_ShouldHaveProperDefaults()
    {
        // Arrange
        var context = new EventContext { EventType = "Test", EventData = "data" };

        // Assert
        context.Metadata.Should().NotBeNull();
        context.Metadata.Should().BeEmpty();
        context.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        context.IsProcessed.Should().BeFalse();
        context.ProcessingException.Should().BeNull();
    }
}
