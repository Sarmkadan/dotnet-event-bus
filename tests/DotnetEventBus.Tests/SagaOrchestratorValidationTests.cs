#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using DotnetEventBus.Advanced;
using FluentAssertions;
using Xunit;

namespace DotnetEventBus.Tests;

/// <summary>
/// Unit tests for SagaOrchestratorValidation class.
/// </summary>
public sealed class SagaOrchestratorValidationTests
{
    private sealed class OrderContext
    {
    }

    private static Task NoOp(OrderContext context) => Task.CompletedTask;

    [Fact]
    public void Validate_WithConfiguredOrchestrator_ReturnsEmptyListAndIsValid()
    {
        // Arrange
        var orchestrator = new SagaOrchestrator<OrderContext>("order-saga");
        orchestrator.AddStep("reserve-stock", NoOp, NoOp);

        // Act
        var errors = orchestrator.Validate();

        // Assert
        errors.Should().BeEmpty();
        orchestrator.IsValid().Should().BeTrue();
        orchestrator.EnsureValid();
    }

    [Fact]
    public void Validate_WithNullOrchestrator_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SagaOrchestratorValidation.Validate<OrderContext>(null!));
        Assert.Throws<ArgumentNullException>(() => SagaOrchestratorValidation.IsValid<OrderContext>(null!));
        Assert.Throws<ArgumentNullException>(() => SagaOrchestratorValidation.EnsureValid<OrderContext>(null!));
    }

    [Fact]
    public void Validate_WithoutStepsAndWhitespaceName_ReturnsBothErrorsInOrder()
    {
        // Arrange
        var orchestrator = new SagaOrchestrator<OrderContext>("order-saga") { Name = "   " };

        // Act
        var errors = orchestrator.Validate();

        // Assert
        errors.Should().Equal(
            "Name cannot be null, empty, or whitespace.",
            "At least one step must be added to the saga orchestrator.");
        orchestrator.IsValid().Should().BeFalse();
    }

    [Fact]
    public void Validate_WithDefectiveSteps_ReportsIndexedNameAndNullActionErrors()
    {
        // Arrange
        var orchestrator = new SagaOrchestrator<OrderContext>("order-saga");
        orchestrator.AddStep("reserve-stock", NoOp);
        orchestrator.AddStep("  ", NoOp);
        orchestrator.AddStep("ship", NoOp);
        orchestrator.GetStepStatus().ElementAt(2).Action = null!;

        // Act
        var errors = orchestrator.Validate();

        // Assert
        errors.Should().Equal(
            "Step at index 1 has null or empty name.",
            "Step 'ship' has null Action.");
    }

    [Fact]
    public void Validate_WithFailedStep_RequiresNonWhitespaceErrorMessage()
    {
        // Arrange
        var orchestrator = new SagaOrchestrator<OrderContext>("order-saga");
        orchestrator.AddStep("charge-card", NoOp);
        var step = orchestrator.GetStepStatus().Single();
        step.Status = SagaStepStatus.Failed;
        step.ErrorMessage = "   ";

        // Act & Assert: whitespace-only message counts as missing
        orchestrator.Validate().Should().Equal(
            "Step 'charge-card' is in Failed state but has no ErrorMessage.");

        // Boundary: any non-whitespace message makes the saga valid again
        step.ErrorMessage = "card declined";
        orchestrator.IsValid().Should().BeTrue();
    }

    [Fact]
    public void EnsureValid_WithInvalidOrchestrator_ThrowsArgumentExceptionListingErrors()
    {
        // Arrange
        var orchestrator = new SagaOrchestrator<OrderContext>("order-saga");

        // Act
        var exception = Assert.Throws<ArgumentException>(() => orchestrator.EnsureValid());

        // Assert
        exception.Message.Should().StartWith("SagaOrchestrator validation failed:");
        exception.Message.Should().Contain("At least one step must be added to the saga orchestrator.");
    }
}
