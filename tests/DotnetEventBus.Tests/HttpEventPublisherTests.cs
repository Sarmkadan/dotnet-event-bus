#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using DotnetEventBus.Integration;

namespace DotnetEventBus.Tests;

/// <summary>
/// Contains unit tests for the <see cref="HttpEventPublisher"/> class.
/// Tests publishing, retry behavior, timeouts and error handling against a stubbed HTTP handler.
/// </summary>
public class HttpEventPublisherTests
{
    private const string Url = "https://example.test/events";

    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        using var client = new HttpClient();
        var logger = NullLogger<HttpEventPublisher>.Instance;

        var nullClient = () => new HttpEventPublisher(null!, logger);
        var nullLogger = () => new HttpEventPublisher(client, null!);

        nullClient.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
        nullLogger.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Options_Defaults_MatchDocumentedValues()
    {
        var options = new HttpEventPublisherOptions();

        options.MaxRetries.Should().Be(3);
        options.RetryDelayMs.Should().Be(1000);
        options.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        options.CorrelationIdHeaderName.Should().Be("X-Correlation-Id");
    }

    [Fact]
    public async Task PublishAsync_Success_PostsJsonWithCustomAndCorrelationHeaders()
    {
        var handler = StubHandler.Status(HttpStatusCode.OK);
        var publisher = CreatePublisher(handler);
        var headers = new Dictionary<string, string> { ["X-Tenant"] = "acme" };

        var result = await publisher.PublishAsync(Url, new { Id = 1, Name = "order" }, headers, "application/cloudevents+json");

        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.ErrorMessage.Should().BeNull();
        handler.Requests.Should().ContainSingle();
        handler.Bodies[0].Should().Be(JsonSerializer.Serialize(new { Id = 1, Name = "order" }));
        handler.Requests[0].Content!.Headers.ContentType!.MediaType.Should().Be("application/cloudevents+json");
        handler.Requests[0].Content!.Headers.GetValues("X-Tenant").Should().ContainSingle("acme");
        handler.Requests[0].Headers.GetValues("X-Correlation-Id").Should().ContainSingle();
    }

    [Fact]
    public async Task PublishAsync_NullArguments_ThrowsArgumentNullException()
    {
        var publisher = CreatePublisher(StubHandler.Status(HttpStatusCode.OK));

        Func<Task> nullUrl = () => publisher.PublishAsync(null!, new { Id = 1 });
        Func<Task> nullData = () => publisher.PublishAsync(Url, null!);

        await nullUrl.Should().ThrowAsync<ArgumentNullException>().WithParameterName("url");
        await nullData.Should().ThrowAsync<ArgumentNullException>().WithParameterName("eventData");
    }

    [Fact]
    public async Task PublishAsync_TransientFailureThenSuccess_StopsRetryingOnSuccess()
    {
        var handler = StubHandler.Status(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
        var publisher = CreatePublisher(handler, new HttpEventPublisherOptions { RetryDelayMs = 0 });

        var result = await publisher.PublishAsync(Url, new { Id = 1 });

        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        handler.Requests.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, 3)] // 5xx is retried up to MaxRetries
    [InlineData(HttpStatusCode.BadRequest, 1)]          // 4xx is final, never retried
    public async Task PublishAsync_FailureStatus_RetriesOnlyTransientErrors(HttpStatusCode status, int expectedAttempts)
    {
        var handler = StubHandler.Status(status);
        var publisher = CreatePublisher(handler, new HttpEventPublisherOptions { MaxRetries = 3, RetryDelayMs = 0 });

        var result = await publisher.PublishAsync(Url, new { Id = 1 });

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be((int)status);
        result.ErrorMessage.Should().Be("boom");
        handler.Requests.Should().HaveCount(expectedAttempts);
    }

    [Fact]
    public async Task PublishAsync_ExceptionOnEveryAttempt_ReturnsStatusZeroWithMessage()
    {
        var handler = new StubHandler((_, _) => throw new HttpRequestException("network down"));
        var publisher = CreatePublisher(handler, new HttpEventPublisherOptions { MaxRetries = 2, RetryDelayMs = 0 });

        var result = await publisher.PublishAsync(Url, new { Id = 1 });

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(0);
        result.ErrorMessage.Should().Be("network down");
        handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task PublishAsync_SlowEndpoint_TimesOutAndReturnsFailure()
    {
        var handler = new StubHandler(async (_, ct) =>
        {
            await Task.Delay(System.Threading.Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var publisher = CreatePublisher(handler, new HttpEventPublisherOptions
        {
            MaxRetries = 1,
            Timeout = TimeSpan.FromMilliseconds(50)
        });

        var result = await publisher.PublishAsync(Url, new { Id = 1 });

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(0);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    private static HttpEventPublisher CreatePublisher(HttpMessageHandler handler, HttpEventPublisherOptions? options = null)
    {
        var client = new HttpClient(handler);
        return new HttpEventPublisher(client, NullLogger<HttpEventPublisher>.Instance, options);
    }

    /// <summary>
    /// Records every outgoing request and answers with the delegate's response for that attempt index.
    /// </summary>
    private sealed class StubHandler(Func<int, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = new();
        public List<string> Bodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return await respond(Requests.Count - 1, cancellationToken);
        }

        /// <summary>Responds with the given status codes in order; the last one repeats.</summary>
        public static StubHandler Status(params HttpStatusCode[] codes) =>
            new((i, _) => Task.FromResult(new HttpResponseMessage(codes[Math.Min(i, codes.Length - 1)])
            {
                Content = new StringContent("boom")
            }));
    }
}
