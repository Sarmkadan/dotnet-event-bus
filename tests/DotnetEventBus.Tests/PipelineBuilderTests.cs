#nullable enable

using FluentAssertions;
using Xunit;
using DotnetEventBus.Middleware;

namespace DotnetEventBus.Tests;

public sealed class PipelineBuilderTests
{
    [Fact]
    public void Use_WithNullMiddleware_ShouldThrowArgumentNullException()
    {
        // Arrange
        var builder = new PipelineBuilder();

        // Act & Assert
        builder.Invoking(b => b.Use(null!))
            .Should().Throw<ArgumentNullException>();
    }

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
