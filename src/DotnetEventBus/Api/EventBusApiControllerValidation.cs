#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotnetEventBus.Api;

/// <summary>
/// Provides validation helpers for <see cref="EventBusApiController"/> instances.
/// </summary>
public static class EventBusApiControllerValidation
{
    /// <summary>
    /// Validates an <see cref="EventBusApiController"/> instance and returns a list of validation errors.
    /// </summary>
    /// <param name="value">The controller instance to validate.</param>
    /// <returns>A read-only list of validation error messages. Empty since EventBusApiController has no validation constraints.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this EventBusApiController value)
    {
        ArgumentNullException.ThrowIfNull(value);

        // EventBusApiController itself has no public properties to validate
        // Validation of its return types (ApiResponse<T>, EventPublishResult, etc.)
        // is handled by the extension methods on those types
        return [];
    }

    /// <summary>
    /// Determines whether the specified <see cref="EventBusApiController"/> is valid.
    /// </summary>
    /// <param name="value">The controller instance to check.</param>
    /// <returns>True if the controller is valid; always true since EventBusApiController has no validation constraints.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static bool IsValid(this EventBusApiController value) => value.Validate().Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="EventBusApiController"/> is valid, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="value">The controller instance to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is invalid; contains all validation errors.</exception>
    public static void EnsureValid(this EventBusApiController value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            var errorMessages = string.Join("\n- ", errors);
            throw new ArgumentException(
                $"EventBusApiController is invalid. Validation errors:\n- {errorMessages}",
                nameof(value));
        }
    }

    /// <summary>
    /// Validates an <see cref="ApiResponse{T}"/> instance and returns a list of validation errors.
    /// </summary>
    /// <typeparam name="T">The type of data in the response.</typeparam>
    /// <param name="value">The API response to validate.</param>
    /// <returns>A read-only list of validation error messages. Empty since EventBusApiController has no validation constraints.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate<T>(this ApiResponse<T> value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate IsSuccess
        // Note: IsSuccess can be true or false, both are valid states

        // Validate Timestamp (should not be default DateTime)
        if (value.Timestamp == default)
        {
            errors.Add("Timestamp cannot be the default DateTime value.");
        }
        else if (value.Timestamp > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("Timestamp cannot be in the future.");
        }
        else if (value.Timestamp < DateTime.UtcNow.AddYears(-1))
        {
            errors.Add("Timestamp cannot be more than one year in the past.");
        }

        // Validate consistency between IsSuccess, Data, and ErrorMessage
        if (value.IsSuccess)
        {
            if (value.ErrorMessage is not null)
            {
                errors.Add("ErrorMessage must be null when IsSuccess is true.");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(value.ErrorMessage))
            {
                errors.Add("ErrorMessage is required when IsSuccess is false.");
            }
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="ApiResponse{T}"/> is valid.
    /// </summary>
    /// <typeparam name="T">The type of data in the response.</typeparam>
    /// <param name="value">The API response to check.</param>
    /// <returns>True if the controller is valid; always true since EventBusApiController has no validation constraints.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static bool IsValid<T>(this ApiResponse<T> value) => value.Validate().Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="ApiResponse{T}"/> is valid, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <typeparam name="T">The type of data in the response.</typeparam>
    /// <param name="value">The API response to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is invalid; contains all validation errors.</exception>
    public static void EnsureValid<T>(this ApiResponse<T> value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            var errorMessages = string.Join("\n- ", errors);
            throw new ArgumentException(
                $"ApiResponse<{typeof(T).Name}> is invalid. Validation errors:\n- {errorMessages}",
                nameof(value));
        }
    }

    /// <summary>
    /// Validates an <see cref="EventPublishResult"/> instance and returns a list of validation errors.
    /// </summary>
    /// <param name="value">The publish result to validate.</param>
    /// <returns>A read-only list of validation error messages. Empty since EventBusApiController has no validation constraints.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this EventPublishResult value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate EventId (can be null in error cases, but if present should be valid)
        if (value.EventId is not null && string.IsNullOrWhiteSpace(value.EventId))
        {
            errors.Add("EventId cannot be empty or whitespace if set.");
        }

        // Validate EventType (required for successful publishes)
        if (string.IsNullOrWhiteSpace(value.EventType))
        {
            errors.Add("EventType is required.");
        }

        // Validate PublishedAt (should not be default DateTime)
        if (value.PublishedAt == default)
        {
            errors.Add("PublishedAt cannot be the default DateTime value.");
        }
        else if (value.PublishedAt > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("PublishedAt cannot be in the future.");
        }
        else if (value.PublishedAt < DateTime.UtcNow.AddYears(-1))
        {
            errors.Add("PublishedAt cannot be more than one year in the past.");
        }

        // Validate Success (should be consistent with other fields)
        // Note: Success can be true or false, both are valid states

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="EventPublishResult"/> is valid.
    /// </summary>
    /// <param name="value">The publish result to check.</param>
    /// <returns>True if the controller is valid; always true since EventBusApiController has no validation constraints.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static bool IsValid(this EventPublishResult value) => value.Validate().Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="EventPublishResult"/> is valid, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="value">The publish result to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is invalid; contains all validation errors.</exception>
    public static void EnsureValid(this EventPublishResult value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            var errorMessages = string.Join("\n- ", errors);
            throw new ArgumentException(
                $"EventPublishResult is invalid. Validation errors:\n- {errorMessages}",
                nameof(value));
        }
    }

    /// <summary>
    /// Validates a <see cref="BatchPublishResult"/> instance and returns a list of validation errors.
    /// </summary>
    /// <param name="value">The batch publish result to validate.</param>
    /// <returns>A read-only list of validation error messages. Empty since EventBusApiController has no validation constraints.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this BatchPublishResult value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate BatchId (can be null in error cases, but if present should be valid)
        if (value.BatchId is not null && string.IsNullOrWhiteSpace(value.BatchId))
        {
            errors.Add("BatchId cannot be empty or whitespace if set.");
        }

        // Validate EventCount (should be non-negative)
        if (value.EventCount < 0)
        {
            errors.Add("EventCount cannot be negative.");
        }

        // Validate PublishedAt (should not be default DateTime)
        if (value.PublishedAt == default)
        {
            errors.Add("PublishedAt cannot be the default DateTime value.");
        }
        else if (value.PublishedAt > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("PublishedAt cannot be in the future.");
        }
        else if (value.PublishedAt < DateTime.UtcNow.AddYears(-1))
        {
            errors.Add("PublishedAt cannot be more than one year in the past.");
        }

        // Validate Success (should be consistent with other fields)
        // Note: Success can be true or false, both are valid states

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="BatchPublishResult"/> is valid.
    /// </summary>
    /// <param name="value">The batch publish result to check.</param>
    /// <returns>True if the controller is valid; always true since EventBusApiController has no validation constraints.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static bool IsValid(this BatchPublishResult value) => value.Validate().Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="BatchPublishResult"/> is valid, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="value">The batch publish result to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is invalid; contains all validation errors.</exception>
    public static void EnsureValid(this BatchPublishResult value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            var errorMessages = string.Join("\n- ", errors);
            throw new ArgumentException(
                $"BatchPublishResult is invalid. Validation errors:\n- {errorMessages}",
                nameof(value));
        }
    }

    /// <summary>
    /// Validates an <see cref="EventBusStats"/> instance and returns a list of validation errors.
    /// </summary>
    /// <param name="value">The stats to validate.</param>
    /// <returns>A read-only list of validation error messages. Empty since EventBusApiController has no validation constraints.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this EventBusStats value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate Status (can be null in error cases)
        if (string.IsNullOrWhiteSpace(value.Status))
        {
            errors.Add("Status is required.");
        }

        // Validate TotalEventsPublished (should be non-negative)
        if (value.TotalEventsPublished < 0)
        {
            errors.Add("TotalEventsPublished cannot be negative.");
        }

        // Validate TotalEventsFailed (should be non-negative)
        if (value.TotalEventsFailed < 0)
        {
            errors.Add("TotalEventsFailed cannot be negative.");
        }

        // Validate ActiveSubscriptions (should be non-negative)
        if (value.ActiveSubscriptions < 0)
        {
            errors.Add("ActiveSubscriptions cannot be negative.");
        }

        // Validate LastCheckTime (should not be default DateTime)
        if (value.LastCheckTime == default)
        {
            errors.Add("LastCheckTime cannot be the default DateTime value.");
        }
        else if (value.LastCheckTime > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("LastCheckTime cannot be in the future.");
        }
        else if (value.LastCheckTime < DateTime.UtcNow.AddYears(-1))
        {
            errors.Add("LastCheckTime cannot be more than one year in the past.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="EventBusStats"/> is valid.
    /// </summary>
    /// <param name="value">The stats to check.</param>
    /// <returns>True if the controller is valid; always true since EventBusApiController has no validation constraints.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static bool IsValid(this EventBusStats value) => value.Validate().Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="EventBusStats"/> is valid, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="value">The stats to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is invalid; contains all validation errors.</exception>
    public static void EnsureValid(this EventBusStats value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            var errorMessages = string.Join("\n- ", errors);
            throw new ArgumentException(
                $"EventBusStats is invalid. Validation errors:\n- {errorMessages}",
                nameof(value));
        }
    }

    /// <summary>
    /// Validates a <see cref="HealthStatus"/> value.
    /// </summary>
    /// <param name="value">The health status to validate.</param>
    /// <returns>A read-only list of validation error messages. Empty since EventBusApiController has no validation constraints.</returns>
    public static IReadOnlyList<string> Validate(this HealthStatus value) => [];

    /// <summary>
    /// Determines whether the specified <see cref="HealthStatus"/> is valid.
    /// </summary>
    /// <param name="value">The health status to check.</param>
    /// <returns>True if the health status is valid; always true since all enum values are valid.</returns>
    public static bool IsValid(this HealthStatus value) => value.Validate().Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="HealthStatus"/> is valid.
    /// This method exists for API consistency but does not throw since all values are valid.
    /// </summary>
    /// <param name="value">The health status to validate.</param>
    public static void EnsureValid(this HealthStatus value)
    {
        // No validation needed - all enum values are valid
    }
}