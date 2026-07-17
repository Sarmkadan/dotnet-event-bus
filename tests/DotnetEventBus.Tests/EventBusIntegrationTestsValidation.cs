#nullable enable

using System.Globalization;

namespace DotnetEventBus.Tests;

/// <summary>
/// Provides validation helpers for <see cref="EventBusIntegrationTests"/> to ensure test data integrity.
/// </summary>
public static class EventBusIntegrationTestsValidation
{
    /// <summary>
    /// Validates the specified <see cref="EventBusIntegrationTests"/> instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>An immutable list of validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this EventBusIntegrationTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate that all test methods are properly async and follow naming conventions
        var methods = value!.GetType().GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

        foreach (var method in methods)
        {
            if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
                continue;

            // All test methods should be async Task
            if (method.ReturnType != typeof(System.Threading.Tasks.Task))
            {
                problems.Add($"Method '{method.Name}' should return Task but returns {method.ReturnType.Name}");
            }

            // Test methods should be named with async suffix or follow xUnit conventions
            if (!method.Name.StartsWith("EventBus_") && !method.Name.StartsWith("CircuitBreaker_") &&
                !method.Name.StartsWith("MetricsCollector_") && !method.Name.StartsWith("EventFilter_") &&
                !method.Name.StartsWith("BatchEventPublisher_") && !method.Name.StartsWith("Pipeline_") &&
                !method.Name.StartsWith("Concurrency_"))
            {
                problems.Add($"Method '{method.Name}' should follow test naming convention (EventBus_, CircuitBreaker_, etc.)");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="EventBusIntegrationTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this EventBusIntegrationTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Validate(value).Count is 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="EventBusIntegrationTests"/> instance is valid, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid, containing a list of problems.</exception>
    public static void EnsureValid(this EventBusIntegrationTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count is not 0)
        {
            throw new ArgumentException(
                $"EventBusIntegrationTests instance is not valid. Problems:\n{string.Join("\n", problems)}",
                nameof(value));
        }
    }
}
