# PerformanceProfilerValidation

Provides static validation helpers for the performance profiler configuration used within the dotnet-event-bus library. The type contains no instance state and is intended to be called directly on the type to verify that profiler settings are correct before use.

## API

### `public static IReadOnlyList<string> Validate()`
- **Purpose**: Returns a read‑only list of validation messages describing any problems with the current performance profiler configuration.
- **Parameters**: None.
- **Return value**: An `IReadOnlyList<string>` where each entry is a validation error message. An empty list indicates that the configuration is valid.
- **Exceptions**: None under normal operation. If an unexpected internal error occurs, the method may propagate the underlying exception (e.g., `NullReferenceException` if required static data has not been initialized).

### `public static IReadOnlyList<string> Validate()`
- **Purpose**: Identical to the first `Validate` overload; provided to support separate calling contexts where semantic distinction is desired (e.g., pre‑flight vs. post‑flight checks).
- **Parameters**: None.
- **Return value**: An `IReadOnlyList<string>` of validation error messages; empty when the configuration passes validation.
- **Exceptions**: Same as the first overload.

### `public static bool IsValid()`
- **Purpose**: Determines whether the performance profiler configuration is valid without returning detailed messages.
- **Parameters**: None.
- **Return value**: `true` if the configuration contains no validation errors; otherwise `false`.
- **Exceptions**: None under normal operation. Unexpected errors are propagated as with `Validate`.

### `public static bool IsValid()`
- **Purpose**: Duplicate of the first `IsValid` overload, intended for use in scenarios where a separate validity check is logically distinct (e.g., library initialization vs. runtime guard).
- **Parameters**: None.
- **Return value**: `true` when the configuration is valid, `false` otherwise.
- **Exceptions**: Same as the first `IsValid` overload.

### `public static void EnsureValid()`
- **Purpose**: Validates the configuration and throws an exception if any validation errors are found, guaranteeing that the caller can proceed only when the configuration is correct.
- **Parameters**: None.
- **Return value**: None.
- **Exceptions**: Throws an `InvalidOperationException` whose message contains the concatenated validation error messages returned by `Validate`. If `Validate` returns an empty list, no exception is thrown.

### `public static void EnsureValid()`
- **Purpose**: Duplicate of the first `EnsureValid` overload, offered for semantic separation (e.g., one call during startup, another before a critical operation).
- **Parameters**: None.
- **Return value**: None.
- **Exceptions**: Same as the first `EnsureValid` overload.

## Usage

```csharp
using DotNetEventBus.Profiling;

// Example 1: Simple validation check
if (!PerformanceProfilerValidation.IsValid())
{
    var errors = PerformanceProfilerValidation.Validate();
    foreach (var err in errors)
    {
        Console.WriteLine($"Profiler configuration error: {err}");
    }
    // Handle invalid configuration (e.g., fallback defaults)
}

// Example 2: Assert validity before starting a benchmark
try
{
    PerformanceProfilerValidation.EnsureValid();
    // Proceed with benchmark knowing the profiler is correctly configured
}
catch (InvalidOperationException ex)
{
    // Log or surface the validation failure
    Console.Error.WriteLine($"Cannot start benchmark: {ex.Message}");
}
```

## Notes

- All members are **static** and operate on shared internal state that is initialized at library load time. Consequently, the methods are thread‑safe for concurrent calls as long as the underlying state is not mutated after initialization.
- The duplicated signatures exist to allow callers to express different validation intents (e.g., a lightweight check vs. a guaranteed‑validity guard) without introducing additional parameters. Functionally, each pair behaves identically.
- If the library’s internal profiler data has not been initialized (for example, if the type is accessed before any profiler‑related code runs), the validation methods may throw exceptions originating from the initialization logic. Callers should ensure that any necessary profiler setup has occurred before invoking these members.
- The validation messages returned by `Validate` are intended for diagnostic purposes and may change between library versions; consumers should not rely on specific wording for programmatic logic beyond checking for emptiness.
