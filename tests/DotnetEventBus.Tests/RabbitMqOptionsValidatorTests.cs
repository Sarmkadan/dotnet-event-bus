using DotnetEventBus.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotnetEventBus.Tests;

public class RabbitMqOptionsValidatorTests
{
    private readonly RabbitMqOptionsValidator _validator = new();

    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        // Act
        var result = _validator.Validate(null, new RabbitMqOptions());

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _validator.Validate(null, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankHostName_FailsNamingHostName(string hostName)
    {
        // Arrange
        var options = new RabbitMqOptions { HostName = hostName };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle().Which.Should().Contain("HostName");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    [InlineData(int.MaxValue)]
    public void Validate_PortOutOfRange_FailsNamingPort(int port)
    {
        // Arrange
        var options = new RabbitMqOptions { Port = port };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle().Which.Should().Contain("Port");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5672)]
    [InlineData(65535)]
    public void Validate_PortInRange_Succeeds(int port)
    {
        // Act
        var result = _validator.Validate(null, new RabbitMqOptions { Port = port });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_BlankExchangeName_FailsNamingExchangeName(string exchangeName)
    {
        // Arrange
        var options = new RabbitMqOptions { ExchangeName = exchangeName };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle().Which.Should().Contain("ExchangeName");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_NonPositivePrefetchCount_FailsNamingPrefetchCount(int prefetchCount)
    {
        // Arrange
        var options = new RabbitMqOptions { PrefetchCount = prefetchCount };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle().Which.Should().Contain("PrefetchCount");
    }

    [Fact]
    public void Validate_NegativeMaxRetryAttempts_FailsNamingMaxRetryAttempts()
    {
        // Arrange
        var options = new RabbitMqOptions { MaxRetryAttempts = -1 };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle().Which.Should().Contain("MaxRetryAttempts");
    }

    [Fact]
    public void Validate_NegativeRetryDelay_FailsNamingRetryDelay()
    {
        // Arrange
        var options = new RabbitMqOptions { RetryDelay = TimeSpan.FromMilliseconds(-1) };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle().Which.Should().Contain("RetryDelay");
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(0.0)]
    [InlineData(double.NaN)]
    public void Validate_MultiplierBelowOne_FailsNamingRetryDelayMultiplier(double multiplier)
    {
        // Arrange
        var options = new RabbitMqOptions { RetryDelayMultiplier = multiplier };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle().Which.Should().Contain("RetryDelayMultiplier");
    }

    [Fact]
    public void Validate_MaxRetryDelayBelowRetryDelay_FailsNamingMaxRetryDelay()
    {
        // Arrange
        var options = new RabbitMqOptions
        {
            RetryDelay = TimeSpan.FromSeconds(10),
            MaxRetryDelay = TimeSpan.FromSeconds(1)
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle().Which.Should().Contain("MaxRetryDelay");
    }

    [Fact]
    public void Validate_MultipleInvalidProperties_ReportsEveryFailure()
    {
        // Arrange
        var options = new RabbitMqOptions
        {
            HostName = "",
            Port = 0,
            ExchangeName = "",
            PrefetchCount = 0
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCount(4);
        result.Failures.Should().Contain(f => f.Contains("HostName"));
        result.Failures.Should().Contain(f => f.Contains("Port"));
        result.Failures.Should().Contain(f => f.Contains("ExchangeName"));
        result.Failures.Should().Contain(f => f.Contains("PrefetchCount"));
    }
}
