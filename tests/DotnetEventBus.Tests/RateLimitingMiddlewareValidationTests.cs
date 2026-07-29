using DotnetEventBus.Middleware;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetEventBus.Tests;

public class RateLimitingMiddlewareValidationTests
{
    private readonly Mock<ILogger<RateLimitingMiddleware>> _loggerMock = new();

    [Fact]
    public void Validate_ValidMiddleware_ReturnsNoProblems()
    {
        var middleware = new RateLimitingMiddleware(_loggerMock.Object, 100, TimeSpan.FromSeconds(10));
        var problems = middleware.Validate();
        problems.Should().BeEmpty();
    }

    [Fact]
    public void Validate_InvalidRequestsPerWindow_ReturnsProblems()
    {
        var middleware = new RateLimitingMiddleware(_loggerMock.Object, 0, TimeSpan.FromSeconds(10));
        var problems = middleware.Validate();
        problems.Should().NotBeEmpty();
        problems.Should().Contain(p => p.Contains("RequestsPerWindow must be greater than 0"));
    }

    [Fact]
    public void Validate_InvalidTimeWindow_ReturnsProblems()
    {
        var middleware = new RateLimitingMiddleware(_loggerMock.Object, 100, TimeSpan.FromSeconds(-1));
        var problems = middleware.Validate();
        problems.Should().NotBeEmpty();
        problems.Should().Contain(p => p.Contains("TimeWindow must be greater than TimeSpan.Zero"));
    }

    [Fact]
    public void IsValid_ValidMiddleware_ReturnsTrue()
    {
        var middleware = new RateLimitingMiddleware(_loggerMock.Object, 100, TimeSpan.FromSeconds(10));
        middleware.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_InvalidMiddleware_ReturnsFalse()
    {
        var middleware = new RateLimitingMiddleware(_loggerMock.Object, 0, TimeSpan.FromSeconds(10));
        middleware.IsValid().Should().BeFalse();
    }

    [Fact]
    public void EnsureValid_ValidMiddleware_DoesNotThrow()
    {
        var middleware = new RateLimitingMiddleware(_loggerMock.Object, 100, TimeSpan.FromSeconds(10));
        var act = () => middleware.EnsureValid();
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureValid_InvalidMiddleware_ThrowsArgumentException()
    {
        var middleware = new RateLimitingMiddleware(_loggerMock.Object, 0, TimeSpan.FromSeconds(10));
        var act = () => middleware.EnsureValid();
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EnsureValid_NullMiddleware_ThrowsArgumentNullException()
    {
        RateLimitingMiddleware? middleware = null;
        var act = () => middleware.EnsureValid();
        act.Should().Throw<ArgumentNullException>();
    }
}
