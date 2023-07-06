#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DotnetEventBus.Formatters;

/// <summary>
/// Provides validation helpers for <see cref="EventFormatterFactory"/> instances.
/// Ensures factory configuration is valid for runtime use.
/// </summary>
public static class EventFormatterFactoryValidation
{
    /// <summary>
    /// Validates an <see cref="EventFormatterFactory"/> instance.
    /// </summary>
    /// <param name="value">The factory to validate.</param>
    /// <returns>A list of validation problems (empty if valid).</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this EventFormatterFactory? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate _formatters dictionary state
        if (value.GetAllFormatters().Any(f => f is null))
        {
            problems.Add("Factory contains null formatters.");
        }

        // Validate each formatter
        foreach (var formatter in value.GetAllFormatters())
        {
            if (formatter is null)
            {
                continue; // Already reported above
            }

            if (string.IsNullOrWhiteSpace(formatter.Format))
            {
                problems.Add($"Formatter '{formatter.GetType().Name}' has null or whitespace Format.");
            }

            if (string.IsNullOrWhiteSpace(formatter.ContentType))
            {
                problems.Add($"Formatter '{formatter.GetType().Name}' has null or whitespace ContentType.");
            }
        }

        // Validate supported formats
        var supportedFormats = value.GetSupportedFormats().ToList();
        if (supportedFormats.Count == 0)
        {
            problems.Add("Factory has no registered formatters.");
        }

        // Validate format names are valid identifiers
        foreach (var format in supportedFormats)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                problems.Add("Factory contains empty format name.");
                continue;
            }

            if (format.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)))
            {
                problems.Add($"Format name '{format}' contains invalid characters.");
            }
        }

        // Validate format name uniqueness (case-insensitive)
        var formatGroups = supportedFormats
            .GroupBy(f => f.ToLower(CultureInfo.InvariantCulture))
            .Where(g => g.Count() > 1)
            .ToList();

        if (formatGroups.Count > 0)
        {
            problems.Add(
                $"Factory has duplicate format names (case-insensitive): {string.Join(", ", formatGroups.Select(g => $"'{g.Key}'"))}.");
        }

        // Validate content type uniqueness (case-insensitive)
        var contentTypes = value.GetAllFormatters()
            .Where(f => f is not null)
            .Select(f => f.ContentType.ToLower(CultureInfo.InvariantCulture))
            .ToList();

        var duplicateContentTypes = contentTypes
            .GroupBy(ct => ct)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateContentTypes.Count > 0)
        {
            problems.Add(
                $"Factory has duplicate content types (case-insensitive): {string.Join(", ", duplicateContentTypes.Select(g => $"'{g.Key}'"))}.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether an <see cref="EventFormatterFactory"/> instance is valid.
    /// </summary>
    /// <param name="value">The factory to check.</param>
    /// <returns>True if the factory is valid; otherwise, false.</returns>
    public static bool IsValid(this EventFormatterFactory? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that an <see cref="EventFormatterFactory"/> instance is valid.
    /// </summary>
    /// <param name="value">The factory to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the factory is invalid, containing a list of validation problems.</exception>
    public static void EnsureValid(this EventFormatterFactory? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"EventFormatterFactory is invalid. Problems:\n- {string.Join("\n- ", problems)}");
    }
}
