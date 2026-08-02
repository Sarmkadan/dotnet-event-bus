// SPDX-License-Identifier: MIT
// Tests for the SagaOrchestrator implementation.
// Uses the same namespace style as the existing test files in the repository.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotnetEventBus.Advanced;
using Xunit;

namespace DotnetEventBus.Tests;

public class SagaOrchestratorTests
{
    private sealed class DummyContext
    {
        public List<string> Log { get; } = new();
    }

    [Fact]
    public async Task ExecuteAsync_WithOnlySuccessfulSteps_ReturnsSuccessAndAllStepsCompleted()
    {
        // Arrange
        var orchestrator = new SagaOrchestrator<DummyContext>("happy-path");
        orchestrator
            .AddStep(
                "step1",
                ctx =>
                {
                    ctx.Log.Add("step1");
                    return Task.CompletedTask;
                })
            .AddStep(
                "step2",
                ctx =>
                {
                    ctx.Log.Add("step2");
                    return Task.CompletedTask;
                });

        var context = new DummyContext();

        // Act
        var result = await orchestrator.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.FailedStep);
        var steps = orchestrator.GetStepStatus().ToList();
        Assert.All(steps, s => Assert.Equal(SagaStepStatus.Completed, s.Status));
        Assert.Equal(new[] { "step1", "step2" }, context.Log);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStepFails_PerformsCompensationAndReturnsFailure()
    {
        // Arrange
        var orchestrator = new SagaOrchestrator<DummyContext>("compensation-test");
        orchestrator
            .AddStep(
                "step1",
                ctx =>
                {
                    ctx.Log.Add("step1");
                    return Task.CompletedTask;
                },
                ctx =>
                {
                    ctx.Log.Add("compensate-step1");
                    return Task.CompletedTask;
                })
            .AddStep(
                "step2",
                ctx => throw new InvalidOperationException("boom"),
                null);

        var context = new DummyContext();

        // Act
        var result = await orchestrator.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("step2", result.FailedStep);
        Assert.Contains("boom", result.Error ?? string.Empty);

        var steps = orchestrator.GetStepStatus().ToDictionary(s => s.Name);
        Assert.Equal(SagaStepStatus.Compensated, steps["step1"].Status);
        Assert.Equal(SagaStepStatus.Failed, steps["step2"].Status);
        Assert.Contains("compensate-step1", context.Log);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoSteps_ReturnsSuccess()
    {
        // Arrange
        var orchestrator = new SagaOrchestrator<DummyContext>("empty-saga");
        var context = new DummyContext();

        // Act
        var result = await orchestrator.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(orchestrator.GetStepStatus());
    }

    [Fact]
    public void Constructor_NullSagaId_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SagaOrchestrator<DummyContext>(null!));
    }

    [Fact]
    public void AddStep_NullStepName_ThrowsArgumentNullException()
    {
        // Arrange
        var orchestrator = new SagaOrchestrator<DummyContext>("test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            orchestrator.AddStep(null!, ctx => Task.CompletedTask));
    }

    [Fact]
    public void AddStep_NullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var orchestrator = new SagaOrchestrator<DummyContext>("test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            orchestrator.AddStep("step", null!));
    }
}
