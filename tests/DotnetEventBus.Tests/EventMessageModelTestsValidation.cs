#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;
using DotnetEventBus.Models;

namespace DotnetEventBus.Tests;

/// <summary>
/// Provides validation helpers for EventMessage and Subscription model classes.
/// </summary>
public static class EventMessageModelTestsValidation
{
    /// <summary>
    /// Validates an EventMessage instance and returns a list of validation problems.
    /// </summary>
    /// <param name="value">The EventMessage to validate</param>
    /// <returns>An enumerable of validation problems, or empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    public static IReadOnlyList<string> ValidateEventMessage(this EventMessage value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate MessageId
        if (string.IsNullOrWhiteSpace(value.MessageId))
        {
            problems.Add("MessageId cannot be null or whitespace");
        }
        else if (value.MessageId == Guid.Empty.ToString())
        {
            problems.Add("MessageId cannot be an empty GUID");
        }

        // Validate EventType
        if (string.IsNullOrWhiteSpace(value.EventType))
        {
            problems.Add("EventType cannot be null or whitespace");
        }

        // Validate Payload
        if (string.IsNullOrWhiteSpace(value.Payload))
        {
            problems.Add("Payload cannot be null or whitespace");
        }

        // Validate CreatedAtUtc
        if (value.CreatedAtUtc == default)
        {
            problems.Add("CreatedAtUtc cannot be default(DateTime)");
        }
        else if (value.CreatedAtUtc > DateTime.UtcNow.AddMinutes(1))
        {
            problems.Add("CreatedAtUtc cannot be in the future");
        }

        // Validate ProcessingAttempts
        if (value.ProcessingAttempts < 0)
        {
            problems.Add("ProcessingAttempts cannot be negative");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates a Subscription instance and returns a list of validation problems.
    /// </summary>
    /// <param name="value">The Subscription to validate</param>
    /// <returns>An enumerable of validation problems, or empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    public static IReadOnlyList<string> ValidateSubscription(this Subscription value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate Id
        if (string.IsNullOrWhiteSpace(value.Id))
        {
            problems.Add("Id cannot be null or whitespace");
        }
        else if (value.Id == Guid.Empty.ToString())
        {
            problems.Add("Id cannot be an empty GUID");
        }

        // Validate EventType
        if (string.IsNullOrWhiteSpace(value.EventType))
        {
            problems.Add("EventType cannot be null or whitespace");
        }

        // Validate Handler
        if (value.Handler == null)
        {
            problems.Add("Handler cannot be null");
        }

        // Validate HandlerName
        if (string.IsNullOrWhiteSpace(value.HandlerName))
        {
            problems.Add("HandlerName cannot be null or whitespace");
        }

        // Validate IsActive (no specific validation needed)

        // Validate Priority
        if (value.Priority < 0)
        {
            problems.Add("Priority cannot be negative");
        }

        // Validate CreatedAtUtc
        if (value.CreatedAtUtc == default)
        {
            problems.Add("CreatedAtUtc cannot be default(DateTime)");
        }
        else if (value.CreatedAtUtc > DateTime.UtcNow.AddMinutes(1))
        {
            problems.Add("CreatedAtUtc cannot be in the future");
        }

        // Validate Timeout
        if (value.Timeout.HasValue)
        {
            if (value.Timeout <= TimeSpan.Zero)
            {
                problems.Add("Timeout must be greater than zero if specified");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether an EventMessage is valid.
    /// </summary>
    /// <param name="value">The EventMessage to check</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValid(this EventMessage value)
    {
        return value.ValidateEventMessage().Count == 0;
    }

    /// <summary>
    /// Determines whether a Subscription is valid.
    /// </summary>
    /// <param name="value">The Subscription to check</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValid(this Subscription value)
    {
        return value.ValidateSubscription().Count == 0;
    }

    /// <summary>
    /// Ensures that an EventMessage is valid, throwing an ArgumentException with validation details if not.
    /// </summary>
    /// <param name="value">The EventMessage to validate</param>
    /// <exception cref="ArgumentException">Thrown if the message is invalid</exception>
    public static void EnsureValid(this EventMessage value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.ValidateEventMessage();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"EventMessage is invalid:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }

    /// <summary>
    /// Ensures that a Subscription is valid, throwing an ArgumentException with validation details if not.
    /// </summary>
    /// <param name="value">The Subscription to validate</param>
    /// <exception cref="ArgumentException">Thrown if the subscription is invalid</exception>
    public static void EnsureValid(this Subscription value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.ValidateSubscription();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Subscription is invalid:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }
}