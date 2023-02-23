// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetEventBus.Advanced;

/// <summary>
/// Orchestrates multi-step distributed transactions using the Saga pattern.
/// Coordinates compensating transactions on failure for guaranteed consistency.
/// Why: Critical for maintaining data consistency across microservices.
/// </summary>
public class SagaOrchestrator<TContext> where TContext : class
{
    private readonly List<SagaStep<TContext>> _steps = [];
    private readonly string _sagaId;

    public SagaOrchestrator(string sagaId)
    {
        _sagaId = sagaId ?? throw new ArgumentNullException(nameof(sagaId));
    }

    /// <summary>
    /// Adds a step to the saga.
    /// </summary>
    public SagaOrchestrator<TContext> AddStep(
        string stepName,
        Func<TContext, Task> action,
        Func<TContext, Task>? compensationAction = null)
    {
        ArgumentNullException.ThrowIfNull(stepName);
        ArgumentNullException.ThrowIfNull(action);

        var step = new SagaStep<TContext>
        {
            Name = stepName,
            Action = action,
            CompensationAction = compensationAction,
            Status = SagaStepStatus.Pending
        };

        _steps.Add(step);
        return this;
    }

    /// <summary>
    /// Executes the saga orchestration.
    /// Rolls back all completed steps if any step fails.
    /// </summary>
    public async Task<SagaExecutionResult> ExecuteAsync(TContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var completedSteps = new List<SagaStep<TContext>>();
        var executionResult = new SagaExecutionResult { SagaId = _sagaId };

        try
        {
            // Execute forward path
            foreach (var step in _steps)
            {
                try
                {
                    step.Status = SagaStepStatus.Running;
                    await step.Action(context);
                    step.Status = SagaStepStatus.Completed;
                    completedSteps.Add(step);
                }
                catch (Exception ex)
                {
                    step.Status = SagaStepStatus.Failed;
                    step.ErrorMessage = ex.Message;
                    executionResult.FailedStep = step.Name;
                    executionResult.Error = ex.Message;

                    // Rollback: execute compensation actions in reverse order
                    await RollbackAsync(completedSteps, context);

                    executionResult.Success = false;
                    return executionResult;
                }
            }

            executionResult.Success = true;
            return executionResult;
        }
        catch (Exception ex)
        {
            executionResult.Success = false;
            executionResult.Error = $"Saga execution failed: {ex.Message}";
            return executionResult;
        }
    }

    private async Task RollbackAsync(List<SagaStep<TContext>> completedSteps, TContext context)
    {
        // Execute compensations in reverse order
        foreach (var step in completedSteps.AsEnumerable().Reverse())
        {
            if (step.CompensationAction != null)
            {
                try
                {
                    step.Status = SagaStepStatus.Compensating;
                    await step.CompensationAction(context);
                    step.Status = SagaStepStatus.Compensated;
                }
                catch (Exception ex)
                {
                    step.Status = SagaStepStatus.CompensationFailed;
                    step.ErrorMessage = ex.Message;
                }
            }
        }
    }

    /// <summary>
    /// Gets the status of all steps.
    /// </summary>
    public IEnumerable<SagaStep<TContext>> GetStepStatus()
    {
        return _steps.ToList();
    }
}

public class SagaStep<T> where T : class
{
    public required string Name { get; set; }
    public required Func<T, Task> Action { get; set; }
    public Func<T, Task>? CompensationAction { get; set; }
    public SagaStepStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum SagaStepStatus
{
    Pending,
    Running,
    Completed,
    Compensating,
    Compensated,
    Failed,
    CompensationFailed
}

public class SagaExecutionResult
{
    public string? SagaId { get; set; }
    public bool Success { get; set; }
    public string? FailedStep { get; set; }
    public string? Error { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}
