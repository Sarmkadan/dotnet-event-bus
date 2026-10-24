#nullable enable

using System;
using System.Threading.Tasks;
using DotnetEventBus.Advanced;
using FluentAssertions;
using Xunit;

namespace DotnetEventBus.Tests;

/// <summary>
/// Unit tests for <see cref="RequestResponseBus"/> and <see cref="RequestResponseHandler{TRequest,TResponse}"/>.
/// Covers constructor validation, the request/handler round trip, failure propagation, timeouts and unknown request ids.
/// </summary>
public sealed class RequestResponseBusTests
{
    [Fact]
    public void Constructor_WithNullPublisher_ShouldThrowArgumentNullException()
    {
        var act = () => new RequestResponseBus((Func<string, RequestMessage<object>, Task>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task RequestAsync_WithPublisherAndHandler_ShouldReturnHandlerResponseAndClearPending()
    {
        RequestResponseBus? bus = null;
        var handler = new EchoHandler();
        string? publishedRequestId = null;

        bus = new RequestResponseBus(async (eventType, message) =>
        {
            eventType.Should().Be("ping");
            publishedRequestId = message.RequestId;
            await handler.ProcessRequestAsync((Ping)message.Payload, message.RequestId!, bus!);
        });

        var response = await bus.RequestAsync<Ping, Pong>("ping", new Ping("hello"));

        response.Should().NotBeNull();
        response!.Text.Should().Be("hello!");
        handler.CapturedRequestId.Should().Be(publishedRequestId);
        handler.CapturedBus.Should().BeSameAs(bus);
        bus.GetPendingRequestCount().Should().Be(0);

        // A late response for an already completed request must be rejected.
        bus.SendResponse(publishedRequestId!, new Pong("late")).Should().BeFalse();
        bus.FailRequest(publishedRequestId!, new InvalidOperationException()).Should().BeFalse();
    }

    [Fact]
    public async Task ProcessRequestAsync_WhenHandlerThrows_ShouldRethrowOriginalExceptionToCaller()
    {
        RequestResponseBus? bus = null;
        var handler = new FailingHandler();

        bus = new RequestResponseBus(async (_, message) =>
        {
            await handler.ProcessRequestAsync((Ping)message.Payload, message.RequestId!, bus!);
        });

        var act = () => bus.RequestAsync<Ping, Pong>("ping", new Ping("boom"));

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("handler failed");
        bus.GetPendingRequestCount().Should().Be(0);
    }

    [Fact]
    public async Task ProcessRequestAsync_WithNullArguments_ShouldThrowArgumentNullException()
    {
        var bus = new RequestResponseBus();
        var handler = new EchoHandler();

        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.ProcessRequestAsync(null!, "id", bus));
        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.ProcessRequestAsync(new Ping("x"), null!, bus));
        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.ProcessRequestAsync(new Ping("x"), "id", null!));
    }

    [Fact]
    public async Task RequestAsync_WithZeroTimeout_ShouldThrowTimeoutExceptionAndClearPending()
    {
        var bus = new RequestResponseBus(defaultTimeout: TimeSpan.FromMinutes(5));

        var act = () => bus.RequestAsync<Ping, Pong>("ping", new Ping("slow"), TimeSpan.Zero);

        await act.Should().ThrowAsync<TimeoutException>();
        bus.GetPendingRequestCount().Should().Be(0);
    }

    [Fact]
    public void SendResponseAndFailRequest_WithUnknownRequestId_ShouldReturnFalse()
    {
        var bus = new RequestResponseBus();

        bus.SendResponse("missing", new Pong("x")).Should().BeFalse();
        bus.FailRequest("missing", new InvalidOperationException()).Should().BeFalse();
        bus.GetPendingRequestCount().Should().Be(0);

        // Cancelling when nothing is pending is a no-op and must not throw.
        var act = () => bus.CancelAllRequests();

        act.Should().NotThrow();
        bus.GetPendingRequestCount().Should().Be(0);
    }

    private sealed record Ping(string Text);

    private sealed record Pong(string Text);

    private sealed class EchoHandler : RequestResponseHandler<Ping, Pong>
    {
        public string? CapturedRequestId => RequestId;

        public RequestResponseBus? CapturedBus => Bus;

        public override Task<Pong> HandleAsync(Ping request) => Task.FromResult(new Pong(request.Text + "!"));
    }

    private sealed class FailingHandler : RequestResponseHandler<Ping, Pong>
    {
        public override Task<Pong> HandleAsync(Ping request) =>
            Task.FromException<Pong>(new InvalidOperationException("handler failed"));
    }
}
