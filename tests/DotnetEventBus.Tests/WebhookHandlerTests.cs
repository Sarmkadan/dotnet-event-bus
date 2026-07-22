using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DotnetEventBus.Integration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetEventBus.Tests;

public class WebhookHandlerTests
{
    [Fact]
    public void Subscribe_ShouldAddSubscriptionAndAssignId_WhenIdIsNull()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<WebhookHandler>>();
        var handler = new WebhookHandler(logger: loggerMock.Object);
        var subscription = new WebhookSubscription
        {
            Url = "https://example.com/webhook",
            EventTypes = new List<string> { "test.event" }
            // Id left null on purpose
        };

        // Act
        handler.Subscribe(subscription);

        // Assert
        subscription.Id.Should().NotBeNullOrEmpty();
        var all = handler.GetAllSubscriptions();
        all.Should().ContainSingle()
            .Which.Should().BeSameAs(subscription);
    }

    [Fact]
    public void Subscribe_ShouldThrowArgumentNullException_WhenSubscriptionIsNull()
    {
        // Arrange
        var handler = new WebhookHandler();

        // Act
        Action act = () => handler.Subscribe(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Unsubscribe_ShouldRemoveExistingSubscription_AndReturnTrue()
    {
        // Arrange
        var handler = new WebhookHandler();
        var subscription = new WebhookSubscription
        {
            Url = "https://example.com",
            EventTypes = new List<string> { "event" }
        };
        handler.Subscribe(subscription);
        var id = subscription.Id!;

        // Act
        var result = handler.Unsubscribe(id);

        // Assert
        result.Should().BeTrue();
        handler.GetAllSubscriptions().Should().BeEmpty();
    }

    [Fact]
    public void Unsubscribe_ShouldReturnFalse_WhenIdNotFound()
    {
        // Arrange
        var handler = new WebhookHandler();

        // Act
        var result = handler.Unsubscribe("non-existent-id");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetWebhooksForEvent_ShouldReturnOnlyActiveMatchingSubscriptions()
    {
        // Arrange
        var handler = new WebhookHandler();
        var matchingActive = new WebhookSubscription
        {
            Url = "https://active.com",
            EventTypes = new List<string> { "order.created" },
            IsActive = true
        };
        var matchingInactive = new WebhookSubscription
        {
            Url = "https://inactive.com",
            EventTypes = new List<string> { "order.created" },
            IsActive = false
        };
        var wildcard = new WebhookSubscription
        {
            Url = "https://wildcard.com",
            EventTypes = new List<string> { "*" },
            IsActive = true
        };
        var nonMatching = new WebhookSubscription
        {
            Url = "https://other.com",
            EventTypes = new List<string> { "customer.updated" },
            IsActive = true
        };

        handler.Subscribe(matchingActive);
        handler.Subscribe(matchingInactive);
        handler.Subscribe(wildcard);
        handler.Subscribe(nonMatching);

        // Act
        var result = handler.GetWebhooksForEvent("order.created").ToList();

        // Assert
        result.Should().Contain(matchingActive);
        result.Should().Contain(wildcard);
        result.Should().NotContain(matchingInactive);
        result.Should().NotContain(nonMatching);
    }

    [Fact]
    public void GenerateSignature_ShouldReturnEmpty_WhenSigningSecretIsNull()
    {
        // Arrange
        var handler = new WebhookHandler(); // no secret

        // Act
        var signature = handler.GenerateSignature("payload");

        // Assert
        signature.Should().BeEmpty();
    }

    [Fact]
    public void GenerateSignature_ShouldReturnCorrectBase64Signature_WhenSecretProvided()
    {
        // Arrange
        const string secret = "super-secret";
        const string payload = "test-payload";
        var handler = new WebhookHandler(secret);

        // Compute expected signature using the same algorithm
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expectedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var expectedSignature = Convert.ToBase64String(expectedHash);

        // Act
        var actualSignature = handler.GenerateSignature(payload);

        // Assert
        actualSignature.Should().Be(expectedSignature);
    }

    [Fact]
    public void VerifySignature_ShouldReturnTrue_WhenSignatureMatches()
    {
        // Arrange
        const string secret = "my-secret";
        const string payload = "payload-data";
        var handler = new WebhookHandler(secret);
        var validSignature = handler.GenerateSignature(payload);

        // Act
        var result = handler.VerifySignature(payload, validSignature);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifySignature_ShouldReturnFalse_WhenSignatureDoesNotMatch()
    {
        // Arrange
        const string secret = "my-secret";
        const string payload = "payload-data";
        var handler = new WebhookHandler(secret);
        var invalidSignature = "invalid-signature";

        // Act
        var result = handler.VerifySignature(payload, invalidSignature);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void UpdateSubscription_ShouldApplyUpdatesAndReturnTrue()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<WebhookHandler>>();
        var handler = new WebhookHandler(logger: loggerMock.Object);
        var subscription = new WebhookSubscription
        {
            Url = "https://old.com",
            EventTypes = new List<string> { "event" }
        };
        handler.Subscribe(subscription);
        var id = subscription.Id!;

        // Act
        var result = handler.UpdateSubscription(id, s => s.Url = "https://new.com");

        // Assert
        result.Should().BeTrue();
        var updated = handler.GetAllSubscriptions().First();
        updated.Url.Should().Be("https://new.com");
    }

    [Fact]
    public void UpdateSubscription_ShouldReturnFalse_WhenSubscriptionNotFound()
    {
        // Arrange
        var handler = new WebhookHandler();

        // Act
        var result = handler.UpdateSubscription("non-existent-id", s => s.Url = "https://new.com");

        // Assert
        result.Should().BeFalse();
    }
}
