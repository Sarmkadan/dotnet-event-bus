#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using DotnetEventBus.Advanced;

namespace DotnetEventBus.Exceptions;

/// <summary>
/// Exception thrown when saga compensation fails, carrying both the original failure
/// and any compensation step failures that occurred during rollback.
/// </summary>
public sealed class SagaCompensationException : EventBusException
{
    /// <summary>
    /// Gets the original exception that caused the saga to fail.
    /// </summary>
    public Exception OriginalException { get; }

    /// <summary>
    /// Gets the name of the step that failed during the forward execution.
    /// </summary>
    public string? FailedStepName { get; }

    /// <summary>
    /// Gets a collection of compensation failures that occurred during rollback.
    /// Each failure contains the step name and the exception that occurred.
    /// </summary>
    public IReadOnlyList<CompensationFailure> CompensationFailures { get; }

    /// <summary>
    /// Gets the saga execution result that contains information about the failed saga.
    /// </summary>
    public SagaExecutionResult SagaResult { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaCompensationException"/> class.
    /// </summary>
    /// <param name="originalException">The original exception that caused the saga to fail.</param>
    /// <param name="failedStepName">The name of the step that failed during forward execution.</param>
    /// <param name="compensationFailures">Collection of compensation failures that occurred during rollback.</param>
    /// <param name="sagaResult">The saga execution result containing saga metadata.</param>
    public SagaCompensationException(
        Exception originalException,
        string? failedStepName,
        IEnumerable<CompensationFailure> compensationFailures,
        SagaExecutionResult sagaResult)
        : base(CreateErrorMessage(originalException, failedStepName, compensationFailures))
    {
        ArgumentNullException.ThrowIfNull(originalException);
        ArgumentNullException.ThrowIfNull(compensationFailures);
        ArgumentNullException.ThrowIfNull(sagaResult);

        OriginalException = originalException;
        FailedStepName = failedStepName;
        CompensationFailures = compensationFailures.ToList().AsReadOnly();
        SagaResult = sagaResult;
    }

    private static string CreateErrorMessage(
        Exception originalException,
        string? failedStepName,
        IEnumerable<CompensationFailure> compensationFailures)
    {
        var message = new System.Text.StringBuilder();
        message.AppendLine("Saga compensation failed.");
        message.AppendLine($"Original failure: {originalException.Message}");

        if (!string.IsNullOrEmpty(failedStepName))
        {
            message.AppendLine($"Failed step: {failedStepName}");
        }

        var failures = compensationFailures.ToList();
        if (failures.Count > 0)
        {
            message.AppendLine($"Compensation failures ({failures.Count}):");
            foreach (var failure in failures)
            {
                message.AppendLine($"  - Step '{failure.StepName}': {failure.Exception.Message}");
            }
        }
        else
        {
            message.AppendLine("No compensation failures occurred.");
        }

        return message.ToString();
    }
}

/// <summary>
/// Represents a compensation failure for a specific saga step.
/// </summary>
public sealed class CompensationFailure
{
    /// <summary>
    /// Gets the name of the saga step that failed during compensation.
    /// </summary>
    public string StepName { get; }

    /// <summary>
    /// Gets the exception that occurred during compensation.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompensationFailure"/> class.
    /// </summary>
    /// <param name="stepName">The name of the saga step.</param>
    /// <param name="exception">The exception that occurred during compensation.</param>
    public CompensationFailure(string stepName, Exception exception)
    {
        ArgumentException.ThrowIfNullOrEmpty(stepName);
        ArgumentNullException.ThrowIfNull(exception);

        StepName = stepName;
        Exception = exception;
    }
}