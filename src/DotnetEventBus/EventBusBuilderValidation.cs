using System.Globalization;
using DotnetEventBus.Configuration;

namespace DotnetEventBus;

/// <summary>
/// Provides validation methods for <see cref="EventBusBuilder"/> instances.
/// </summary>
public static class EventBusBuilderValidation
{
    /// <summary>
    /// Validates the <see cref="EventBusBuilder"/> instance.
    /// </summary>
    /// <param name="value">The builder instance to validate.</param>
    /// <returns>A list of validation error messages. Empty list if validation passes.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this EventBusBuilder value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate EventBusOptions
        var options = value.GetOptions();
        errors.AddRange(ValidateOptions(options));

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the <see cref="EventBusBuilder"/> instance is valid.
    /// </summary>
    /// <param name="value">The builder instance to check.</param>
    /// <returns>True if the builder is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this EventBusBuilder value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Validates the <see cref="EventBusBuilder"/> instance and throws an <see cref="ArgumentException"/> if invalid.
    /// </summary>
    /// <param name="value">The builder instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the builder is invalid, containing the validation errors.</exception>
    public static void EnsureValid(this EventBusBuilder value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"EventBusBuilder is invalid. Validation errors:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    private static EventBusOptions GetOptions(this EventBusBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Use reflection to access the private _options field
        // This is a temporary workaround until EventBusBuilder exposes options publicly
        var field = typeof(EventBusBuilder).GetField(
            "_options",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field is null)
        {
            throw new InvalidOperationException(
                "Failed to retrieve EventBusOptions: _options field not found on EventBusBuilder. " +
                "This may indicate a breaking change in the EventBusBuilder API.");
        }

        var options = field.GetValue(builder);
        if (options is null)
        {
            throw new InvalidOperationException(
                "EventBusBuilder._options field returned null, which is not a valid state.");
        }

        return (EventBusOptions)options;
    }

    private static IReadOnlyList<string> ValidateOptions(EventBusOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();

        if (options.RequestTimeout <= TimeSpan.Zero)
        {
            errors.Add($"RequestTimeout must be greater than zero (current: {options.RequestTimeout.TotalMilliseconds}ms)");
        }

        if (options.MaxRetryAttempts < 0)
        {
            errors.Add($"MaxRetryAttempts cannot be negative (current: {options.MaxRetryAttempts})");
        }

        if (options.RetryDelay < TimeSpan.Zero)
        {
            errors.Add($"RetryDelay cannot be negative (current: {options.RetryDelay.TotalMilliseconds}ms)");
        }

        if (options.RetryDelayMultiplier < 1.0)
        {
            errors.Add($"RetryDelayMultiplier must be at least 1.0 (current: {options.RetryDelayMultiplier.ToString(CultureInfo.InvariantCulture)})");
        }

        if (options.MaxConcurrentHandlers < 1)
        {
            errors.Add($"MaxConcurrentHandlers must be at least 1 (current: {options.MaxConcurrentHandlers})");
        }

        if (options.IsDistributed && string.IsNullOrWhiteSpace(options.DistributedTransportType))
        {
            errors.Add("DistributedTransportType must be specified when IsDistributed is true");
        }

        if (options.DefaultHandlerTimeout <= TimeSpan.Zero)
        {
            errors.Add($"DefaultHandlerTimeout must be greater than zero (current: {options.DefaultHandlerTimeout.TotalMilliseconds}ms)");
        }

        if (options.MaxRetryDelay <= TimeSpan.Zero)
        {
            errors.Add($"MaxRetryDelay must be greater than zero (current: {options.MaxRetryDelay.TotalMilliseconds}ms)");
        }

        return errors.AsReadOnly();
    }
}