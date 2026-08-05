using System;
using DotnetEventBus.Configuration;
using DotnetEventBus.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotnetEventBus.Tests;

public class MiddlewareConfigurationTests
{
    private sealed class SampleMiddleware : IEventBusMiddleware
    {
        public System.Threading.Tasks.Task InvokeAsync(EventMiddlewareContext context, EventMiddlewareDelegate next)
            => next(context);
    }

    private sealed class AnotherMiddleware : IEventBusMiddleware
    {
        public System.Threading.Tasks.Task InvokeAsync(EventMiddlewareContext context, EventMiddlewareDelegate next)
            => next(context);
    }

    [Fact]
    public void UseMiddleware_AddsMiddlewareType()
    {
        var options = new EventBusOptions();

        var result = options.UseMiddleware<SampleMiddleware>();

        Assert.Same(options, result);
        Assert.Contains(typeof(SampleMiddleware), options.MiddlewareTypes);
    }

    [Fact]
    public void UseMiddleware_CalledTwiceWithSameType_DoesNotAddDuplicate()
    {
        var options = new EventBusOptions();

        options.UseMiddleware<SampleMiddleware>();
        options.UseMiddleware<SampleMiddleware>();

        Assert.Single(options.MiddlewareTypes);
    }

    [Fact]
    public void UseMiddleware_NullOptions_ThrowsArgumentNullException()
    {
        EventBusOptions? options = null;

        Assert.Throws<ArgumentNullException>(() => options!.UseMiddleware<SampleMiddleware>());
    }

    [Fact]
    public void UseMiddlewareIf_PredicateTrue_AddsMiddleware()
    {
        var options = new EventBusOptions();

        var result = options.UseMiddlewareIf<SampleMiddleware>(_ => true);

        Assert.Same(options, result);
        Assert.Contains(typeof(SampleMiddleware), options.MiddlewareTypes);
    }

    [Fact]
    public void UseMiddlewareIf_PredicateFalse_DoesNotAddMiddleware()
    {
        var options = new EventBusOptions();

        var result = options.UseMiddlewareIf<SampleMiddleware>(_ => false);

        Assert.Same(options, result);
        Assert.Empty(options.MiddlewareTypes);
    }

    [Fact]
    public void UseMiddlewareIf_NullPredicate_ThrowsArgumentNullException()
    {
        var options = new EventBusOptions();

        Assert.Throws<ArgumentNullException>(() => options.UseMiddlewareIf<SampleMiddleware>(null!));
    }

    [Fact]
    public void UseMiddlewareIf_NullOptions_ThrowsArgumentNullException()
    {
        EventBusOptions? options = null;

        Assert.Throws<ArgumentNullException>(() => options!.UseMiddlewareIf<SampleMiddleware>(_ => true));
    }

    [Fact]
    public void AddEventBusMiddleware_RegistersMiddlewareAsTransient()
    {
        var services = new ServiceCollection();

        var result = services.AddEventBusMiddleware<SampleMiddleware>();

        Assert.Same(services, result);
        var first = result.BuildServiceProvider().GetRequiredService<SampleMiddleware>();
        var second = result.BuildServiceProvider().GetRequiredService<SampleMiddleware>();
        Assert.NotSame(first, second);
    }

    [Fact]
    public void AddEventBusMiddleware_MultipleDifferentMiddlewares_RegistersBoth()
    {
        var services = new ServiceCollection();

        services.AddEventBusMiddleware<SampleMiddleware>();
        services.AddEventBusMiddleware<AnotherMiddleware>();

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<SampleMiddleware>());
        Assert.NotNull(provider.GetRequiredService<AnotherMiddleware>());
    }

    [Fact]
    public void AddEventBusMiddleware_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;

        Assert.Throws<ArgumentNullException>(() => services!.AddEventBusMiddleware<SampleMiddleware>());
    }
}
