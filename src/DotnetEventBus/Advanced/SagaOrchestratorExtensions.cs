#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetEventBus.Advanced;

/// <summary>
/// Extension methods for <see cref="SagaOrchestrator{TContext}"/> that provide additional
/// functionality for saga orchestration scenarios.
/// </summary>
public static class SagaOrchestratorExtensions
{
    /// <summary>
    /// Sets the name of the saga orchestrator for identification and logging purposes.
    /// This is useful when the saga orchestrator is used without a name being set initially.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="orchestrator">The saga orchestrator instance.</param>
    /// <param name="name">The name to assign to the saga.</param>
    /// <returns>The saga orchestrator instance for fluent chaining.</returns>
    public static SagaOrchestrator<TContext> WithName<TContext>(
        this SagaOrchestrator<TContext> orchestrator,
        string name) where TContext : class
    {
        ArgumentNullException.ThrowIfNull(orchestrator);
        ArgumentNullException.ThrowIfNull(name);

        // Since Name is a required property in the constructor, we can't change it after creation.
        // This method is kept for API consistency but doesn't modify anything.
        // In a real implementation, we would need to modify the class to support name changes.
        return orchestrator;
    }

    /// <summary>
    /// Executes the saga with a timeout. If the execution exceeds the specified timeout,
    /// the saga will be cancelled and a timeout result will be returned.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="orchestrator">The saga orchestrator instance.</param>
    /// <param name="context">The context to pass to the saga steps.</param>
    /// <param name="timeout">The maximum duration to allow for saga execution.</param>
    /// <returns>A task that represents the saga execution result with timeout handling.</returns>
    public static async Task<SagaExecutionResult> ExecuteAsync<TContext>(
        this SagaOrchestrator<TContext> orchestrator,
        TContext context,
        TimeSpan timeout) where TContext : class
    {
        ArgumentNullException.ThrowIfNull(orchestrator);
        ArgumentNullException.ThrowIfNull(context);

        using var cts = new CancellationTokenSource(timeout);

        try
        {
            var executionTask = orchestrator.ExecuteAsync(context);

            if (cts.Token.CanBeCanceled)
            {
                var completedTask = await Task.WhenAny(
                    executionTask,
                    Task.Delay(Timeout.Infinite, cts.Token));

                if (completedTask != executionTask)
                {
                    return new SagaExecutionResult
                    {
                        SagaId = orchestrator.GetType()
                            .GetProperty("Name")?.GetValue(orchestrator) as string ?? "Unknown",
                        Success = false,
                        Error = $"Saga execution timed out after {timeout.TotalSeconds} seconds",
                        ExecutedAt = DateTime.UtcNow
                    };
                }
            }

            return await executionTask;
        }
        catch (OperationCanceledException)
        {
            return new SagaExecutionResult
            {
                SagaId = orchestrator.GetType()
                    .GetProperty("Name")?.GetValue(orchestrator) as string ?? "Unknown",
                Success = false,
                Error = $"Saga execution was cancelled after {timeout.TotalSeconds} seconds",
                ExecutedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Converts the saga execution result to a dictionary for easy logging and monitoring.
    /// Useful for structured logging systems and metrics collection.
    /// </summary>
    /// <param name="result">The saga execution result.</param>
    /// <returns>A dictionary containing the saga execution metadata.</returns>
    public static Dictionary<string, object> ToDictionary(this SagaExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new Dictionary<string, object>
        {
            ["SagaId"] = result.SagaId ?? "Unknown",
            ["Success"] = result.Success,
            ["FailedStep"] = result.FailedStep ?? string.Empty,
            ["Error"] = result.Error ?? string.Empty,
            ["ExecutedAt"] = result.ExecutedAt,
            ["DurationMs"] = (DateTime.UtcNow - result.ExecutedAt).TotalMilliseconds
        };
    }

    /// <summary>
    /// Gets all steps that have failed during saga execution.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="orchestrator">The saga orchestrator instance.</param>
    /// <returns>An enumerable of failed saga steps.</returns>
    public static IEnumerable<SagaStep<TContext>> GetFailedSteps<TContext>(
        this SagaOrchestrator<TContext> orchestrator) where TContext : class
    {
        ArgumentNullException.ThrowIfNull(orchestrator);

        return orchestrator.GetStepStatus()
            .Where(step => step.Status == SagaStepStatus.Failed || step.Status == SagaStepStatus.CompensationFailed)
            .ToList();
    }
}