# PipelineBuilderExtensions

Provides a set of extension methods for the `PipelineBuilder` class, enabling the fluent composition of middleware components and the creation of pre-configured event-processing pipelines. These methods simplify the construction of common pipeline topologies by encapsulating the registration of logging, error handling, rate limiting, and custom middleware, as well as offering factory methods for standard, high-performance, and development-oriented pipelines.

## API

### `AddLogging`
```csharp
public static PipelineBuilder AddLogging(this PipelineBuilder builder)
```
Adds a logging middleware component to the pipeline.  
**Parameters:**  
- `builder` – The `PipelineBuilder` instance to extend.  

**Returns:** The same `PipelineBuilder` instance for chaining.  

**Throws:**  
- `ArgumentNullException` – if `builder` is `null`.

### `AddErrorHandling`
```csharp
public static PipelineBuilder AddErrorHandling(this PipelineBuilder builder)
```
Adds an error-handling middleware component that catches and processes exceptions occurring in downstream middleware.  
**Parameters:**  
- `builder` – The `PipelineBuilder` instance to extend.  

**Returns:** The same `PipelineBuilder` instance for chaining.  

**Throws:**  
- `ArgumentNullException` – if `builder` is `null`.

### `AddRateLimiting`
```csharp
public static PipelineBuilder AddRateLimiting(this PipelineBuilder builder)
```
Adds a rate-limiting middleware component to control the throughput of event processing.  
**Parameters:**  
- `builder` – The `PipelineBuilder` instance to extend.  

**Returns:** The same `PipelineBuilder` instance for chaining.  

**Throws:**  
- `ArgumentNullException` – if `builder` is `null`.

### `UseMiddleware`
```csharp
public static PipelineBuilder UseMiddleware<TMiddleware>(this PipelineBuilder builder)
```
Registers a custom middleware type to be executed as part of the pipeline. The middleware type must be compatible with the pipeline’s middleware contract.  
**Parameters:**  
- `builder` – The `PipelineBuilder` instance to extend.  

**Type parameters:**  
- `TMiddleware` – The type of the middleware to add.  

**Returns:** The same `PipelineBuilder` instance for chaining.  

**Throws:**  
- `ArgumentNullException` – if `builder` is `null`.  
- `InvalidOperationException` – if `TMiddleware` does not satisfy the required middleware interface or cannot be instantiated.

### `CreateStandardPipeline`
```csharp
public static PipelineBuilder CreateStandardPipeline(this PipelineBuilder builder)
```
Configures the builder with a standard set of middleware components suitable for general-purpose event processing. The exact composition is implementation-defined but typically includes logging, error handling, and basic observability.  
**Parameters:**  
- `builder` – The `PipelineBuilder` instance to extend.  

**Returns:** The same `PipelineBuilder` instance for chaining.  

**Throws:**  
- `ArgumentNullException` – if `builder` is `null`.

### `CreateHighPerformancePipeline`
```csharp
public static PipelineBuilder CreateHighPerformancePipeline(this PipelineBuilder builder)
```
Configures the builder with a minimal set of middleware components optimized for throughput and low latency. Observability and error-handling overhead are reduced compared to the standard pipeline.  
**Parameters:**  
- `builder` – The `PipelineBuilder` instance to extend.  

**Returns:** The same `PipelineBuilder` instance for chaining.  

**Throws:**  
- `ArgumentNullException` – if `builder` is `null`.

### `CreateDevelopmentPipeline`
```csharp
public static PipelineBuilder CreateDevelopmentPipeline(this PipelineBuilder builder)
```
Configures the builder with an expanded set of middleware components intended for development and debugging. This pipeline typically includes verbose logging, detailed error reporting, and additional diagnostic middleware.  
**Parameters:**  
- `builder` – The `PipelineBuilder` instance to extend.  

**Returns:** The same `PipelineBuilder` instance for chaining.  

**Throws:**  
- `ArgumentNullException` – if `builder` is `null`.

## Usage

### Example 1: Building a standard pipeline with custom middleware
```csharp
using EventBus.Pipeline;

var builder = new PipelineBuilder();

builder
    .CreateStandardPipeline()
    .UseMiddleware<AuditMiddleware>()
    .AddRateLimiting();

var pipeline = builder.Build();
```
This example starts with a standard pipeline configuration, then appends a custom `AuditMiddleware` and a rate limiter. The resulting pipeline is built and ready for event processing.

### Example 2: Creating a development pipeline with error handling
```csharp
using EventBus.Pipeline;

var builder = new PipelineBuilder();

builder
    .CreateDevelopmentPipeline()
    .AddErrorHandling();  // Error handling is already included, but can be overridden or reordered

var pipeline = builder.Build();
```
Here a development pipeline is created, and error handling is explicitly added (or re-added) to ensure it appears at the desired position in the middleware stack. The builder pattern allows further customization before building.

## Notes

- **Thread safety:** The `PipelineBuilder` instance is not guaranteed to be thread-safe. All extension methods should be called from a single thread during pipeline construction. Concurrent modifications may result in undefined behavior.
- **Order of middleware:** The order in which middleware components are added determines their execution sequence. Methods like `CreateStandardPipeline`, `CreateHighPerformancePipeline`, and `CreateDevelopmentPipeline` apply a predefined order; subsequent calls to `AddLogging`, `AddErrorHandling`, `AddRateLimiting`, or `UseMiddleware` append components after the pre-configured set.
- **Duplicate middleware:** Adding the same middleware type multiple times is permitted but may lead to redundant processing. The pipeline builder does not deduplicate entries.
- **Pipeline finalization:** Once `Build()` is called on the `PipelineBuilder`, further calls to these extension methods may throw an `InvalidOperationException` or be silently ignored, depending on the implementation. Always complete configuration before building.
- **Null arguments:** All extension methods throw `ArgumentNullException` if the `builder` parameter is `null`. Ensure the builder is instantiated before calling any extension method.
