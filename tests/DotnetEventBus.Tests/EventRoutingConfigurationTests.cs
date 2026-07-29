using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using DotnetEventBus.Configuration;

namespace DotnetEventBus.Tests;

public class EventRoutingConfigurationTests
{
    [Fact]
    public void AddRoute_ShouldStoreRule_ForEventType()
    {
        var config = new EventRoutingConfiguration();
        var rule = new RoutingRule { TargetHandler = "HandlerA" };

        config.AddRoute("OrderCreated", rule);

        var routes = config.GetRoutes("OrderCreated");
        Assert.Single(routes);
        Assert.Same(rule, routes.First());
    }

    [Fact]
    public void ShouldRoute_WithNoRoutes_ReturnsTrue()
    {
        var config = new EventRoutingConfiguration();

        var result = config.ShouldRoute("OrderCreated", "HandlerA");

        Assert.True(result);
    }

    [Fact]
    public void ShouldRoute_WithMatchingHandler_ReturnsTrue()
    {
        var config = new EventRoutingConfiguration();
        config.AddRoute("OrderCreated", new RoutingRule { TargetHandler = "HandlerA" });

        var result = config.ShouldRoute("OrderCreated", "HandlerA");

        Assert.True(result);
    }

    [Fact]
    public void ShouldRoute_WithNonMatchingHandler_ReturnsFalse()
    {
        var config = new EventRoutingConfiguration();
        config.AddRoute("OrderCreated", new RoutingRule { TargetHandler = "HandlerA" });

        var result = config.ShouldRoute("OrderCreated", "HandlerB");

        Assert.False(result);
    }

    [Fact]
    public void ShouldRoute_WithCondition_MatchingMetadata_ReturnsTrue()
    {
        var config = new EventRoutingConfiguration();
        var rule = new RoutingRule
        {
            TargetHandler = "HandlerA",
            Condition = meta => meta.ContainsKey("Region") && meta["Region"].Equals("US")
        };
        config.AddRoute("OrderCreated", rule);

        var metadata = new Dictionary<string, object> { { "Region", "US" } };
        var result = config.ShouldRoute("OrderCreated", "HandlerA", metadata);

        Assert.True(result);
    }

    [Fact]
    public void ShouldRoute_WithCondition_NonMatchingMetadata_ReturnsFalse()
    {
        var config = new EventRoutingConfiguration();
        var rule = new RoutingRule
        {
            TargetHandler = "HandlerA",
            Condition = meta => meta.ContainsKey("Region") && meta["Region"].Equals("US")
        };
        config.AddRoute("OrderCreated", rule);

        var metadata = new Dictionary<string, object> { { "Region", "EU" } };
        var result = config.ShouldRoute("OrderCreated", "HandlerA", metadata);

        Assert.False(result);
    }

    [Fact]
    public void AddRoute_NullEventType_ThrowsArgumentNullException()
    {
        var config = new EventRoutingConfiguration();
        Assert.Throws<ArgumentNullException>(() => config.AddRoute(null!, new RoutingRule { TargetHandler = "H" }));
    }

    [Fact]
    public void AddRoute_NullRule_ThrowsArgumentNullException()
    {
        var config = new EventRoutingConfiguration();
        Assert.Throws<ArgumentNullException>(() => config.AddRoute("Type", null!));
    }

    [Fact]
    public void Clear_ShouldRemoveAllRoutes()
    {
        var config = new EventRoutingConfiguration();
        config.AddRoute("Type1", new RoutingRule { TargetHandler = "H1" });
        config.AddRoute("Type2", new RoutingRule { TargetHandler = "H2" });

        config.Clear();

        Assert.Empty(config.GetConfiguredEventTypes());
    }

    [Fact]
    public void EventRoutingBuilder_RouteByMetadata_SetsConditionCorrectly()
    {
        var builder = new EventRoutingBuilder();
        builder.RouteByMetadata("OrderCreated", "HandlerA", "Source", "Web");
        var config = builder.Build();

        var metadata = new Dictionary<string, object> { { "Source", "Web" } };
        var result = config.ShouldRoute("OrderCreated", "HandlerA", metadata);

        Assert.True(result);
    }
}
