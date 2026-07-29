#nullable enable

using FluentAssertions;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using DotnetEventBus.Middleware;
using DotnetEventBus.Configuration;

namespace DotnetEventBus.Tests;

public sealed class PipelineBuilderExtensionsTests
{
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly PipelineBuilder _pipelineBuilder;

    public PipelineBuilderExtensionsTests()
    {
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger>());
        _pipelineBuilder = new PipelineBuilder();
    }

    [Fact]
    public void AddLogging_WithValidInputs_ShouldNotThrow()
    {
        var act = () => _pipelineBuilder.AddLogging(_loggerFactoryMock.Object);
        act.Should().NotThrow();
    }

    [Fact]
    public void AddLogging_WithNullBuilder_ShouldThrowArgumentNullException()
    {
        var act = () => ((PipelineBuilder?)null)!.AddLogging(_loggerFactoryMock.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddErrorHandling_WithValidInputs_ShouldNotThrow()
    {
        var act = () => _pipelineBuilder.AddErrorHandling(_loggerFactoryMock.Object);
        act.Should().NotThrow();
    }

    [Fact]
    public void AddErrorHandling_WithNegativeMaxRetries_ShouldThrowArgumentOutOfRangeException()
    {
        var act = () => _pipelineBuilder.AddErrorHandling(_loggerFactoryMock.Object, maxRetries: -1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AddRateLimiting_WithValidInputs_ShouldNotThrow()
    {
        var act = () => _pipelineBuilder.AddRateLimiting(_loggerFactoryMock.Object);
        act.Should().NotThrow();
    }

    [Fact]
    public void AddRateLimiting_WithInvalidRequestsPerWindow_ShouldThrowArgumentOutOfRangeException()
    {
        var act = () => _pipelineBuilder.AddRateLimiting(_loggerFactoryMock.Object, requestsPerWindow: 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CreateStandardPipeline_ShouldNotThrow()
    {
        var act = () => _pipelineBuilder.CreateStandardPipeline(_loggerFactoryMock.Object);
        act.Should().NotThrow();
    }
}
