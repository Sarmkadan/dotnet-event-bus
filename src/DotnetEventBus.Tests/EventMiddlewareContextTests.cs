using System;
using System.Threading;
using DotnetEventBus.Middleware;
using DotnetEventBus.Models;
using Xunit;

namespace DotnetEventBus.Tests;

public class EventMiddlewareContextTests
{
    private static readonly EventMessage SampleMessage = new();

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var payload = new { Name = "Test" };
        var eventType = payload.GetType();
        var correlationId = "corr-123";
        var cancellationToken = new CancellationTokenSource().Token;

        // Act
        var context = new EventMiddlewareContext(
            @event: payload,
            eventType: eventType,
            correlationId: correlationId,
            eventMessage: SampleMessage,
            cancellationToken: cancellationToken);

        // Assert
        Assert.Same(payload, context.Event);
        Assert.Equal(eventType, context.EventType);
        Assert.Equal(correlationId, context.CorrelationId);
        Assert.Same(SampleMessage, context.EventMessage);
        Assert.Equal(cancellationToken, context.CancellationToken);
    }

    [Fact]
    public void Constructor_AllowsNullCorrelationId()
    {
        // Arrange
        var payload = "string-event";
        var eventType = typeof(string);

        // Act
        var context = new EventMiddlewareContext(
            @event: payload,
            eventType: eventType,
            correlationId: null,
            eventMessage: SampleMessage,
            cancellationToken: CancellationToken.None);

        // Assert
        Assert.Null(context.CorrelationId);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenEventIsNull()
    {
        // Arrange
        var eventType = typeof(object);

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new EventMiddlewareContext(
                @event: null!,
                eventType: eventType,
                correlationId: "id",
                eventMessage: SampleMessage,
                cancellationToken: CancellationToken.None));

        Assert.Equal("event", ex.ParamName);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenEventTypeIsNull()
    {
        // Arrange
        var payload = new object();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new EventMiddlewareContext(
                @event: payload,
                eventType: null!,
                correlationId: "id",
                eventMessage: SampleMessage,
                cancellationToken: CancellationToken.None));

        Assert.Equal("eventType", ex.ParamName);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenEventMessageIsNull()
    {
        // Arrange
        var payload = new object();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new EventMiddlewareContext(
                @event: payload,
                eventType: payload.GetType(),
                correlationId: "id",
                eventMessage: null!,
                cancellationToken: CancellationToken.None));

        Assert.Equal("eventMessage", ex.ParamName);
    }

    [Fact]
    public void CorrelationId_Setter_UpdatesValue()
    {
        // Arrange
        var payload = new object();
        var context = new EventMiddlewareContext(
            @event: payload,
            eventType: payload.GetType(),
            correlationId: "initial",
            eventMessage: SampleMessage,
            cancellationToken: CancellationToken.None);

        // Act
        context.CorrelationId = "updated";

        // Assert
        Assert.Equal("updated", context.CorrelationId);
    }

    [Fact]
    public void EventMiddlewareDelegate_CanBeInvoked()
    {
        // Arrange
        var payload = new object();
        var context = new EventMiddlewareContext(
            @event: payload,
            eventType: payload.GetType(),
            correlationId: null,
            eventMessage: SampleMessage,
            cancellationToken: CancellationToken.None);

        bool wasCalled = false;
        EventMiddlewareDelegate next = ctx =>
        {
            wasCalled = true;
            Assert.Same(context, ctx);
            return Task.CompletedTask;
        };

        // Act
        var task = next(context);
        task.GetAwaiter().GetResult();

        // Assert
        Assert.True(wasCalled);
    }
}
