# PipelineBuilderExtensionsJsonExtensions

The `PipelineBuilderExtensionsJsonExtensions` class provides serialization and deserialization capabilities for pipeline configurations within the `dotnet-event-bus` ecosystem. It enables the conversion of pipeline builder states to and from JSON formats, facilitating the persistence, transmission, and dynamic reconstruction of middleware pipelines. Additionally, it exposes inspection properties to determine the presence of specific middleware components such as logging, error handling, and rate limiting within a pipeline configuration.

## API

### ToJson
```csharp
public static string ToJson(...)
```
Serializes a pipeline builder configuration into a JSON string representation. This method captures the current state of the pipeline, including registered middleware and configuration flags.
*   **Parameters**: Accepts the pipeline builder instance or configuration object required for serialization (specific signature details depend on the overloaded implementation).
*   **Return Value**: Returns a `string` containing the JSON representation of the pipeline.
*   **Exceptions**: May throw a serialization exception if the pipeline configuration contains non-serializable types or circular references.

### FromJson
```csharp
public static global::DotnetEventBus.Middleware.PipelineBuilder? FromJson(...)
```
Deserializes a JSON string into a new `PipelineBuilder` instance. This method reconstructs the pipeline structure based on the provided JSON data.
*   **Parameters**: Accepts a `string` containing the JSON representation of the pipeline.
*   **Return Value**: Returns a `PipelineBuilder` instance if deserialization is successful, or `null` if the input is invalid or empty.
*   **Exceptions**: May throw a format exception if the JSON structure does not match the expected schema for a pipeline configuration.

### TryFromJson
```csharp
public static bool TryFromJson(...)
```
Attempts to deserialize a JSON string into a `PipelineBuilder` instance without throwing exceptions on failure. This is the preferred method for parsing untrusted or optional configuration data.
*   **Parameters**: Accepts the input JSON string and an `out` parameter for the resulting `PipelineBuilder`.
*   **Return Value**: Returns `true` if the deserialization succeeds and the `out` parameter is populated; otherwise, returns `false`.
*   **Exceptions**: This method is designed not to throw exceptions for invalid formats; it returns `false` instead.

### HasLogging
```csharp
public bool? HasLogging { get; }
```
Indicates whether the logging middleware is present in the current pipeline configuration.
*   **Return Value**: Returns `true` if logging is enabled, `false` if explicitly disabled, or `null` if the state is undefined or not yet determined.

### HasErrorHandling
```csharp
public bool? HasErrorHandling { get; }
```
Indicates whether the error handling middleware is present in the current pipeline configuration.
*   **Return Value**: Returns `true` if error handling is enabled, `false` if explicitly disabled, or `null` if the state is undefined.

### HasRateLimiting
```csharp
public bool? HasRateLimiting { get; }
```
Indicates whether the rate limiting middleware is present in the current pipeline configuration.
*   **Return Value**: Returns `true` if rate limiting is enabled, `false` if explicitly disabled, or `null` if the state is undefined.

## Usage

### Example 1: Persisting and Restoring a Pipeline Configuration
This example demonstrates how to serialize a configured pipeline to JSON for storage and subsequently restore it using the safe `TryFromJson` method.

```csharp
using DotnetEventBus.Middleware;
using DotnetEventBus.Extensions;

// Assume 'builder' is an existing PipelineBuilder instance configured with middleware
string jsonConfig = PipelineBuilderExtensionsJsonExtensions.ToJson(builder);

// Later, perhaps on application startup or in a different service
if (PipelineBuilderExtensionsJsonExtensions.TryFromJson(jsonConfig, out var restoredBuilder))
{
    // Use the restoredBuilder to construct the event bus
    var eventBus = restoredBuilder.Build();
}
else
{
    // Fallback to a default configuration if deserialization fails
    var eventBus = new PipelineBuilder().Build();
}
```

### Example 2: Inspecting Pipeline Capabilities
This example shows how to inspect a deserialized pipeline to verify the presence of critical middleware components before activation.

```csharp
using DotnetEventBus.Middleware;
using DotnetEventBus.Extensions;

string jsonConfig = GetStoredConfiguration();
var builder = PipelineBuilderExtensionsJsonExtensions.FromJson(jsonConfig);

if (builder != null)
{
    // Check extension properties to validate pipeline integrity
    if (builder.HasErrorHandling != true)
    {
        Console.WriteLine("Warning: Error handling middleware is missing.");
    }

    if (builder.HasRateLimiting == true)
    {
        Console.WriteLine("Rate limiting is active for this pipeline.");
    }
    
    // Proceed with building only if requirements are met
    if (builder.HasErrorHandling == true)
    {
        var bus = builder.Build();
    }
}
```

## Notes

*   **Nullable Boolean States**: The inspection properties (`HasLogging`, `HasErrorHandling`, `HasRateLimiting`) return `bool?`. A value of `null` signifies that the specific middleware status has not been explicitly set or cannot be determined from the current state. Consumers should explicitly check for `true` rather than relying on truthy evaluation to avoid logic errors when the value is `null`.
*   **Deserialization Safety**: Prefer `TryFromJson` over `FromJson` when processing configuration data from external sources (e.g., user input, network streams, or configuration files) to prevent application crashes due to malformed JSON. `FromJson` should only be used when the input source is strictly controlled and guaranteed to be valid.
*   **Thread Safety**: As the class consists primarily of static utility methods for serialization and stateless inspection properties on instances, the static methods are generally thread-safe provided the underlying JSON serializer is thread-safe. However, the `PipelineBuilder` instances returned by `FromJson` are mutable and should not be shared across threads without external synchronization during the configuration phase.
*   **Schema Compatibility**: Changes to the internal structure of the `PipelineBuilder` or its middleware components in future versions of `dotnet-event-bus` may render older JSON strings incompatible. It is recommended to version configuration schemas if long-term persistence is required.
