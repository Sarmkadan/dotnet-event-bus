#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DotnetEventBus.Advanced;

/// <summary>
/// Provides validation helpers for <see cref="SagaOrchestrator{TContext}"/> instances.
/// Validates the state of saga orchestrators to ensure they are properly configured
/// before execution.
/// </summary>
public static class SagaOrchestratorValidation
{
    /// <summary>
    /// Validates the saga orchestrator instance.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="value">The saga orchestrator to validate.</param>
    /// <returns>A list of validation errors. Empty list if validation succeeds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public static IReadOnlyList<string> Validate<TContext>(this SagaOrchestrator<TContext> value) where TContext : class
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate Name (if it exists as a property - checking via reflection since it's required in constructor)
        // Note: Name is set via constructor parameter, so we validate it's not null/empty
        var nameProperty = typeof(SagaOrchestrator<TContext>).GetProperty("Name");
        if (nameProperty != null)
        {
            var nameValue = nameProperty.GetValue(value) as string;
            if (string.IsNullOrWhiteSpace(nameValue))
            {
                errors.Add("Name cannot be null, empty, or whitespace.");
            }
        }

        // Validate Steps collection
        var steps = value.GetStepStatus().ToList();
        if (steps.Count == 0)
        {
            errors.Add("At least one step must be added to the saga orchestrator.");
        }
        else
        {
            // Validate each step
            foreach (var step in steps)
            {
                if (string.IsNullOrWhiteSpace(step.Name))
                {
                    errors.Add($"Step at index {steps.IndexOf(step)} has null or empty name.");
                }

                if (step.Action == null)
                {
                    errors.Add($"Step '{step.Name ?? "unknown"}' has null Action.");
                }

                if (step.Status == SagaStepStatus.Failed && string.IsNullOrWhiteSpace(step.ErrorMessage))
                {
                    errors.Add($"Step '{step.Name ?? "unknown"}' is in Failed state but has no ErrorMessage.");
                }
            }
        }

        // Validate SagaId (if it exists as a property)
        var sagaIdProperty = typeof(SagaOrchestrator<TContext>).GetProperty("SagaId");
        if (sagaIdProperty != null)
        {
            var sagaIdValue = sagaIdProperty.GetValue(value) as string;
            if (string.IsNullOrWhiteSpace(sagaIdValue))
            {
                errors.Add("SagaId cannot be null, empty, or whitespace.");
            }
        }

        // Validate Success flag consistency
        var successProperty = typeof(SagaOrchestrator<TContext>).GetProperty("Success");
        if (successProperty != null)
        {
            var successValue = (bool?)successProperty.GetValue(value) ?? false;
            var errorProperty = typeof(SagaOrchestrator<TContext>).GetProperty("Error");
            var errorValue = errorProperty?.GetValue(value) as string;
            var failedStepProperty = typeof(SagaOrchestrator<TContext>).GetProperty("FailedStep");
            var failedStepValue = failedStepProperty?.GetValue(value) as string;

            if (successValue && !string.IsNullOrEmpty(errorValue))
            {
                errors.Add("Success is true but Error message is not empty. These should be mutually exclusive.");
            }

            if (!successValue && string.IsNullOrEmpty(errorValue) && string.IsNullOrEmpty(failedStepValue))
            {
                errors.Add("Success is false but neither Error nor FailedStep is set. At least one should indicate the failure reason.");
            }
        }

        // Validate ExecutedAt is not default
        var executedAtProperty = typeof(SagaOrchestrator<TContext>).GetProperty("ExecutedAt");
        if (executedAtProperty != null)
        {
            var executedAtValue = (DateTime?)executedAtProperty.GetValue(value) ?? default;
            if (executedAtValue == default)
            {
                errors.Add("ExecutedAt has not been set to a valid date/time value.");
            }
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the saga orchestrator instance is in a valid state.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="value">The saga orchestrator to check.</param>
    /// <returns>True if the orchestrator is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public static bool IsValid<TContext>(this SagaOrchestrator<TContext> value) where TContext : class
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the saga orchestrator instance is in a valid state.
    /// Throws an <see cref="ArgumentException"/> with detailed validation messages if validation fails.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="value">The saga orchestrator to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when validation fails with detailed error messages.</exception>
    public static void EnsureValid<TContext>(this SagaOrchestrator<TContext> value) where TContext : class
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"SagaOrchestrator validation failed:{Environment.NewLine}- {
                    string.Join(Environment.NewLine + "- ", errors)
                }");
        }
    }
}
