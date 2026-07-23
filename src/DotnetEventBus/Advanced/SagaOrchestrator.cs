#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DotnetEventBus.Exceptions;

namespace DotnetEventBus.Advanced;

/// <summary>
/// Orchestrates multi-step distributed transactions using the Saga pattern.
/// Coordinates compensating transactions on failure for guaranteed consistency.
/// Why: Critical for maintaining data consistency across microservices.
/// </summary>
public sealed class SagaOrchestrator<TContext> where TContext : class
{
    private readonly List<SagaStep<TContext>> _steps = [];
    private readonly string _sagaId;
    private readonly ILogger<SagaOrchestrator<TContext>>? _logger;

    public SagaOrchestrator(string sagaId, ILogger<SagaOrchestrator<TContext>>? logger = null)
    {
        _sagaId = sagaId ?? throw new ArgumentNullException(nameof(sagaId));
        _logger = logger;
        Name = sagaId;
    }

    /// <summary>
    /// Display name of the saga used for identification and logging. Defaults to the saga id.
    /// </summary>
    public string Name { get; set; }

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

        _logger?.LogDebug("Saga step added: {StepName} ({Status})", stepName, step.Status);
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
                    var compensationFailures = await RollbackAsync(completedSteps, context);

                    if (compensationFailures.Count > 0)
                    {
                        executionResult.Success = false;
                        throw new SagaCompensationException(ex, step.Name, compensationFailures, executionResult);
                    }

                    executionResult.Success = false;
                    return executionResult;
                }
            }

            executionResult.Success = true;

            _logger?.LogInformation("Saga {SagaId} completed successfully with {StepsCount} steps",
                _sagaId, _steps.Count);
            return executionResult;
        }
        catch (SagaCompensationException)
        {
            // Re-throw SagaCompensationException as it's already properly formatted
            throw;
        }
        catch (Exception ex)
        {
            executionResult.Success = false;
            executionResult.Error = $"Saga execution failed: {ex.Message}";

            _logger?.LogError(ex, "Saga {SagaId} failed", _sagaId);
            return executionResult;
        }
    }

    private async Task<List<CompensationFailure>> RollbackAsync(List<SagaStep<TContext>> completedSteps, TContext context)
    {
        _logger?.LogWarning("Starting rollback for saga {SagaId} with {StepsCount} completed steps",
            _sagaId, completedSteps.Count);

        var compensationFailures = new List<CompensationFailure>();

        // Execute compensations in reverse order
        foreach (var step in completedSteps.AsEnumerable().Reverse())
        {
            if (step.CompensationAction is not null)
            {
                try
                {
                    step.Status = SagaStepStatus.Compensating;
                    _logger?.LogDebug("Executing compensation for step: {StepName}", step.Name);
                    await step.CompensationAction(context);
                    step.Status = SagaStepStatus.Compensated;
                    _logger?.LogDebug("Compensation completed for step: {StepName}", step.Name);
                }
                catch (Exception ex)
                {
                    step.Status = SagaStepStatus.CompensationFailed;
                    step.ErrorMessage = ex.Message;
                    _logger?.LogError(ex, "Compensation failed for step: {StepName}", step.Name);
                    compensationFailures.Add(new DotnetEventBus.Exceptions.CompensationFailure(step.Name, ex));
                }
            }
        }

        _logger?.LogWarning("Rollback completed for saga {SagaId} with {FailuresCount} compensation failures",
            _sagaId, compensationFailures.Count);

        return compensationFailures;
    }

    /// <summary>
    /// Gets the status of all steps.
    /// </summary>
    public IEnumerable<SagaStep<TContext>> GetStepStatus()
    {
        return _steps.ToList();
    }
}


public sealed class SagaStep<T> where T : class
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

public sealed class SagaExecutionResult
{
    public string? SagaId { get; set; }
    public bool Success { get; set; }
    public string? FailedStep { get; set; }
    public string? Error { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}