#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotnetEventBus.Advanced;
using DotnetEventBus.Exceptions;
using FluentAssertions;
using Xunit;

namespace DotnetEventBus.Tests;

public sealed class SagaOrchestratorExtensionsTests
{
    private sealed class SagaContext
    {
        public List<string> Log { get; } = [];
    }

    private static Task Record(SagaContext context, string entry)
    {
        context.Log.Add(entry);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ExecuteAsync_WhenIntermediateStepThrows_CompensatesOnlyCompletedStepsInReverse()
    {
        // Arrange
        var context = new SagaContext();
        var orchestrator = new SagaOrchestrator<SagaContext>("intermediate-failure")
            .Step("step1", c => Record(c, "step1"), c => Record(c, "compensate-step1"))
            .Step("step2", c => Record(c, "step2"), c => Record(c, "compensate-step2"))
            .Step(
                "step3",
                _ => Task.FromException(new InvalidOperationException("step3 failed")),
                c => Record(c, "compensate-step3"))
            .Step("step4", c => Record(c, "step4"), c => Record(c, "compensate-step4"));

        // Act
        var result = await orchestrator.ExecuteAsync(context, TimeSpan.FromSeconds(5));

        // Assert
        result.Success.Should().BeFalse();
        result.FailedStep.Should().Be("step3");
        result.Error.Should().Be("step3 failed");

        context.Log.Should().Equal("step1", "step2", "compensate-step2", "compensate-step1");
        orchestrator.GetStepStatus().Select(s => s.Status).Should().Equal(
            SagaStepStatus.Compensated,
            SagaStepStatus.Compensated,
            SagaStepStatus.Failed,
            SagaStepStatus.Pending);
        orchestrator.GetFailedSteps().Select(s => s.Name).Should().Equal("step3");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoSteps_ReturnsSuccessWithoutExecutingOrCompensating()
    {
        // Arrange
        var context = new SagaContext();
        var orchestrator = new SagaOrchestrator<SagaContext>("empty-saga");

        // Act
        var result = await orchestrator.ExecuteAsync(context, TimeSpan.FromSeconds(5));

        // Assert
        result.Success.Should().BeTrue();
        result.SagaId.Should().Be("empty-saga");
        result.FailedStep.Should().BeNull();
        result.Error.Should().BeNull();
        context.Log.Should().BeEmpty();
        orchestrator.GetStepStatus().Should().BeEmpty();
        orchestrator.GetFailedSteps().Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCompensationThrows_SurfacesOriginalAndCompensationFailures()
    {
        // Arrange
        var context = new SagaContext();
        var orchestrator = new SagaOrchestrator<SagaContext>("compensation-failure")
            .Step("step1", c => Record(c, "step1"), c => Record(c, "compensate-step1"))
            .Step(
                "step2",
                c => Record(c, "step2"),
                _ => Task.FromException(new InvalidOperationException("compensate-step2 failed")))
            .Step("step3", _ => Task.FromException(new InvalidOperationException("forward boom")));

        Func<Task> act = () => orchestrator.ExecuteAsync(context, TimeSpan.FromSeconds(5));

        // Act
        var exception = (await act.Should().ThrowAsync<SagaCompensationException>()).Which;

        // Assert
        exception.OriginalException.Message.Should().Be("forward boom");
        exception.FailedStepName.Should().Be("step3");
        exception.SagaResult.Success.Should().BeFalse();
        exception.Message.Should().Contain("forward boom").And.Contain("compensate-step2 failed");

        var failure = exception.CompensationFailures.Should().ContainSingle().Which;
        failure.StepName.Should().Be("step2");
        failure.Exception.Message.Should().Be("compensate-step2 failed");

        // The failing compensation must not stop the rollback of earlier completed steps.
        context.Log.Should().Equal("step1", "step2", "compensate-step1");
    }

    [Fact]
    public async Task ExecuteAsync_WhenTimeoutFiresMidSaga_CompensatesCompletedStepsExactlyOnce()
    {
        // Arrange
        var context = new SagaContext();
        var releaseStep2 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var rollbackFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var orchestrator = new SagaOrchestrator<SagaContext>("timeout-mid-saga")
            .Step(
                "step1",
                c => Record(c, "step1"),
                c =>
                {
                    c.Log.Add("compensate-step1");
                    rollbackFinished.SetResult();
                    return Task.CompletedTask;
                })
            .Step(
                "step2",
                async c =>
                {
                    await releaseStep2.Task;
                    c.Log.Add("step2");
                },
                c => Record(c, "compensate-step2"))
            .Step("step3", _ => Task.FromException(new InvalidOperationException("step3 failed")));

        // Act
        var timedOut = await orchestrator.ExecuteAsync(context, TimeSpan.FromMilliseconds(100));

        // Assert: the timeout returns while step2 is still blocked on the first execution.
        timedOut.Success.Should().BeFalse();
        timedOut.Error.Should().Contain("timed out");

        // Let the in-flight saga run to its failure, then wait for the single rollback pass to finish.
        releaseStep2.SetResult();
        await rollbackFinished.Task.WaitAsync(TimeSpan.FromSeconds(5));

        context.Log.Should().Equal("step1", "step2", "compensate-step2", "compensate-step1");
    }
}
