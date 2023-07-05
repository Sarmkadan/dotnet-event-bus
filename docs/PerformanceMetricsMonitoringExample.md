# PerformanceMetricsMonitoringExample

The `PerformanceMetricsMonitoringExample` class is a sample implementation within the `dotnet-event-bus` project that demonstrates how to subscribe to and handle performance metric events. It exposes observable state (process identifier, data size, process type) and provides multiple event-handling overrides, each designed to process a different event type from the event bus. The class also includes a static entry point for running the example as a standalone application.

## API

### `public string ProcessId`

Gets or sets the identifier of the process being monitored. This value is typically set during initialization and may be updated by event handlers.

- **Type**: `string`
- **Return value**: The current process identifier.
- **Throws**: No exceptions are thrown by this property.

### `public int DataSize`

Gets or sets the size (in bytes) of the data payload associated with the current metrics snapshot.

- **Type**: `int`
- **Return value**: The current data size.
- **Throws**: No exceptions are thrown by this property.

### `public string ProcessType`

Gets or sets a string describing the type or category of the process (e.g., "Background", "Realtime").

- **Type**: `string`
- **Return value**: The current process type.
- **Throws**: No exceptions are thrown by this property.

### `public override async Task Handle`

Three override implementations of the `Handle` method, each processing a different event type defined by the base class. The exact event types are determined by the base class contract and are not exposed directly by this example. Each override performs asynchronous processing of the received event, typically updating the instance properties (`ProcessId`, `DataSize`, `ProcessType`) or logging metrics.

- **Parameters**: Each override accepts a single event argument of a distinct type (inherited from the base class). The parameter is not listed here because the member signature only shows the method name; refer to the base class documentation for the full signature.
- **Return value**: A `Task` representing the asynchronous operation.
- **Throws**: May throw exceptions originating from the event bus or from user-provided callbacks. No custom exceptions are defined by this class.

### `public static async Task Main`

The application entry point. This method initializes the event bus, creates an instance of `PerformanceMetricsMonitoringExample`, subscribes its `Handle` methods to the appropriate events, and starts processing incoming metrics. It runs until the application is terminated.

- **Parameters**: None (standard `Main` signature).
- **Return value**: A `Task` representing the asynchronous lifetime of the application.
- **Throws**: May throw exceptions during event bus setup or if the bus fails to start.

## Usage

The following examples illustrate typical usage of `PerformanceMetricsMonitoringExample` within a console application.

### Example 1: Basic subscription and event handling

```csharp
using dotnet_event_bus;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Create an instance of the monitoring example
        var monitor = new PerformanceMetricsMonitoringExample
        {
            ProcessId = "monitor-001",
            DataSize = 4096,
            ProcessType = "Realtime"
        };

        // Assume an event bus is available (e.g., from dependency injection)
        var bus = new EventBus();

        // Subscribe the three Handle methods to their respective events
        bus.Subscribe<MetricsSnapshotEvent>(monitor.Handle);
        bus.Subscribe<ThresholdAlertEvent>(monitor.Handle);
        bus.Subscribe<ProcessStatusEvent>(monitor.Handle);

        // Start the bus and run until cancelled
        await bus.StartAsync();
        Console.WriteLine("Monitoring started. Press Ctrl+C to stop.");
        await Task.Delay(Timeout.Infinite);
    }
}
```

### Example 2: Using the static Main entry point

```csharp
// The class itself provides a static Main method for direct execution.
// This is equivalent to running the project as a standalone application.
// No additional code is required; simply set the project's startup object
// to dotnet_event_bus.PerformanceMetricsMonitoringExample.
```

## Notes

- **Edge cases**:  
  - `ProcessId`, `DataSize`, and `ProcessType` are not validated by the class. Setting `ProcessId` or `ProcessType` to `null` or empty strings may cause unexpected behavior in downstream consumers.  
  - `DataSize` can be set to any integer value, including negative numbers. No range checking is performed.  
  - The three `Handle` overrides are invoked asynchronously; if an event handler throws an exception, the event bus may log the error and continue processing other events, depending on the bus implementation.

- **Thread safety**:  
  This class is **not thread-safe**. The public properties (`ProcessId`, `DataSize`, `ProcessType`) are mutable and are intended to be accessed from a single thread (typically the event-processing thread). Concurrent reads and writes from multiple threads may lead to inconsistent state. If concurrent access is required, external synchronization (e.g., a lock or `ConcurrentDictionary`) should be used.

- **Inheritance**:  
  The class inherits from a base class that defines the abstract or virtual `Handle` methods. The exact base class is not specified here; the overrides must match the signatures defined by that base class. Modifying the base class’s event types may require updating the overrides in this example.
