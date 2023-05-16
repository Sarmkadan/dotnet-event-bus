# SerializationBenchmarks

The `SerializationBenchmarks` class provides a set of performance testing methods designed to evaluate the throughput and latency of different event serialization formats (JSON, CSV, and XML) within the `dotnet-event-bus` framework. It utilizes `BenchmarkDotNet` to execute comparative benchmarks on these formatters.

## API

### `public void GlobalSetup()`
Prepares the necessary data structures and test entities for the benchmark execution. This method generates a new `EventMessage` instance with dummy data to ensure consistent input for all serialization benchmarks. It does not take any parameters and does not return a value.

### `public string Serialize_Json()`
Performs benchmark testing for the JSON serialization formatter. It serializes the pre-configured `EventMessage` to a JSON string. Returns the serialized JSON string.

### `public string Serialize_Csv()`
Performs benchmark testing for the CSV serialization formatter. It serializes the pre-configured `EventMessage` to a CSV-formatted string. Returns the serialized CSV string.

### `public string Serialize_Xml()`
Performs benchmark testing for the XML serialization formatter. It serializes the pre-configured `EventMessage` to an XML-formatted string. Returns the serialized XML string.

## Usage

```csharp
// Example 1: Running benchmarks for the SerializationBenchmarks class using BenchmarkDotNet
using BenchmarkDotNet.Running;
using DotnetEventBus.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<SerializationBenchmarks>();
    }
}
```

```csharp
// Example 2: Invoking benchmark methods directly for manual verification
var benchmarks = new SerializationBenchmarks();
benchmarks.GlobalSetup(); // Initialize test data

string jsonOutput = benchmarks.Serialize_Json();
string csvOutput = benchmarks.Serialize_Csv();
string xmlOutput = benchmarks.Serialize_Xml();
```

## Notes

*   **Benchmarking Context:** These methods are intended for use within a `BenchmarkDotNet` benchmarking suite. The members are designed to be executed repeatedly by the harness to provide accurate performance metrics.
*   **Thread Safety:** The methods are not inherently thread-safe and should be used within the controlled, single-threaded context provided by the `BenchmarkDotNet` runner.
*   **Initialization:** The `GlobalSetup` method MUST be called prior to invoking any `Serialize_*` methods; otherwise, the serialization methods will operate on an uninitialized `EventMessage`, likely resulting in `NullReferenceException` or invalid output.
