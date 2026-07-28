// SPDX-License-Identifier: MIT
// Tests for the EventEnvelope model.
// The project already references xUnit, so we use that framework.

using System;
using System.Collections.Generic;
using DotnetEventBus.Models;
using Xunit;

namespace DotnetEventBus.Tests;

public sealed class EventEnvelopeTests
{
    [Fact]
    public void Create_ShouldPopulateRequiredFields()
    {
        // Arrange
        var payload = new { Name = "Test" };
        const string eventType = "test.created";

        // Act
        var envelope = EventEnvelope.Create(eventType, payload);

        // Assert
        Assert.NotNull(envelope);
        Assert.Equal(eventType, envelope.EventType);
        Assert.Equal(payload, envelope.Payload);
        Assert.False(string.IsNullOrWhiteSpace(envelope.EventId));
        Assert.True(envelope.CreatedAt <= DateTime.UtcNow);
        Assert.Equal(1, envelope.Version);
        Assert.Empty(envelope.Metadata);
    }

    [Fact]
    public void GetHeaders_ShouldContainAllStandardHeaders()
    {
        // Arrange
        var envelope = new EventEnvelope
        {
            EventType = "order.placed",
            Payload = new { OrderId = 123 },
            CorrelationId = "corr-1",
            Source = "order-service",
            Actor = "user-42"
        };

        // Act
        var headers = envelope.GetHeaders();

        // Assert
        Assert.Equal(envelope.EventId ?? string.Empty, headers["X-Event-ID"]);
        Assert.Equal(envelope.EventType, headers["X-Event-Type"]);
        Assert.Equal(envelope.Version.ToString(), headers["X-Event-Version"]);
        Assert.Equal(envelope.CreatedAt.ToString("o"), headers["X-Created-At"]);
        Assert.Equal(envelope.CorrelationId, headers["X-Correlation-ID"]);
        Assert.Equal(envelope.Source, headers["X-Source"]);
        Assert.Equal(envelope.Actor, headers["X-Actor"]);
    }

    [Fact]
    public void IsValid_ReturnsTrueWhenAllRequiredFieldsArePresent()
    {
        // Arrange
        var envelope = new EventEnvelope
        {
            EventType = "user.registered",
            Payload = new { UserId = Guid.NewGuid() }
        };

        // Act
        var result = envelope.IsValid();

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsValid_ReturnsFalseWhenEventTypeIsMissing(string? eventType)
    {
        // Arrange
        var envelope = new EventEnvelope
        {
            EventType = eventType!,
            Payload = new { Dummy = 1 }
        };

        // Act
        var result = envelope.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_ReturnsFalseWhenPayloadIsNull()
    {
        // Arrange
        var envelope = new EventEnvelope
        {
            EventType = "test.event",
            Payload = null!
        };

        // Act
        var result = envelope.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Metadata_ShouldBeMutableAndPreserveValues()
    {
        // Arrange
        var envelope = new EventEnvelope
        {
            EventType = "metadata.test",
            Payload = "data"
        };

        // Act
        envelope.Metadata["key1"] = 123;
        envelope.Metadata["key2"] = "value";

        // Assert
        Assert.Equal(2, envelope.Metadata.Count);
        Assert.Equal(123, envelope.Metadata["key1"]);
        Assert.Equal("value", envelope.Metadata["key2"]);
    }

    [Fact]
    public void DefaultValues_ShouldMatchSpecification()
    {
        // Arrange
        var envelope = new EventEnvelope
        {
            EventType = "default.check",
            Payload = "payload"
        };

        // Assert
        Assert.Equal(1, envelope.Version);
        Assert.False(envelope.IsTestEvent);
        Assert.Equal(0, envelope.ProcessingAttempts);
        Assert.Equal(TimeSpan.FromMinutes(5), envelope.ProcessingTimeout);
        Assert.Equal(50, envelope.Priority);
        Assert.False(envelope.IsCritical);
    }
}
