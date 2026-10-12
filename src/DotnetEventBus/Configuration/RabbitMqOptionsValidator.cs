#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Options;

namespace DotnetEventBus.Configuration;

/// <summary>
/// Validates <see cref="RabbitMqOptions"/>. Every failure message names the offending property.
/// Registered by the RabbitMQ overload of <c>AddEventBus</c>, and usable with the Options pattern
/// (<c>IValidateOptions&lt;RabbitMqOptions&gt;</c>) for callers that bind configuration themselves.
/// </summary>
public sealed class RabbitMqOptionsValidator : IValidateOptions<RabbitMqOptions>
{
    /// <summary>
    /// Validates the supplied options, collecting every failure rather than stopping at the first one.
    /// </summary>
    /// <param name="name">The options name. Ignored; the validator applies to every name.</param>
    /// <param name="options">The options to validate.</param>
    /// <returns>A success result, or a failed result listing each invalid property.</returns>
    public ValidateOptionsResult Validate(string? name, RabbitMqOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.HostName))
            failures.Add($"{nameof(RabbitMqOptions.HostName)} must not be empty.");

        if (options.Port is < 1 or > 65535)
            failures.Add(
                $"{nameof(RabbitMqOptions.Port)} must be between 1 and 65535, but was {options.Port}.");

        if (string.IsNullOrWhiteSpace(options.ExchangeName))
            failures.Add($"{nameof(RabbitMqOptions.ExchangeName)} must not be empty.");

        if (options.PrefetchCount <= 0)
            failures.Add(
                $"{nameof(RabbitMqOptions.PrefetchCount)} must be greater than zero, but was {options.PrefetchCount}.");

        if (options.MaxRetryAttempts < 0)
            failures.Add(
                $"{nameof(RabbitMqOptions.MaxRetryAttempts)} cannot be negative, but was {options.MaxRetryAttempts}.");

        if (options.RetryDelay < TimeSpan.Zero)
            failures.Add($"{nameof(RabbitMqOptions.RetryDelay)} cannot be negative.");

        // Written as a negated comparison so that NaN is rejected too.
        if (!(options.RetryDelayMultiplier >= 1.0))
            failures.Add(
                $"{nameof(RabbitMqOptions.RetryDelayMultiplier)} must be at least 1.0, " +
                $"but was {options.RetryDelayMultiplier}.");

        if (options.MaxRetryDelay < options.RetryDelay)
            failures.Add(
                $"{nameof(RabbitMqOptions.MaxRetryDelay)} must not be less than {nameof(RabbitMqOptions.RetryDelay)}.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
