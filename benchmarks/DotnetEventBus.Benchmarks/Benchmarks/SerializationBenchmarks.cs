using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotnetEventBus.Formatters;
using DotnetEventBus.Models;

namespace DotnetEventBus.Benchmarks;

/// <summary>
/// A benchmark class for measuring the serialization performance of different event formatters.
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SerializationBenchmarks
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SerializationBenchmarks"/> class.
    /// </summary>
    private readonly JsonEventFormatter _jsonFormatter = new JsonEventFormatter();
    private readonly CsvEventFormatter _csvFormatter = new CsvEventFormatter();
    private readonly XmlEventFormatter _xmlFormatter = new XmlEventFormatter();
    
    /// <summary>
    /// The event message used for serialization benchmarks.
    /// </summary>
    private EventMessage _eventMessage;

    /// <summary>
    /// Sets up the benchmark by creating a new event message.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _eventMessage = new EventMessage
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = "TestEvent",
            Payload = "{\"Name\": \"Test\", \"Value\": 42}",
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Serializes the event message using the JSON event formatter.
    /// </summary>
    /// <returns>The serialized event message as a string.</returns>
    [Benchmark]
    public string Serialize_Json()
    {
        return _jsonFormatter.Serialize(_eventMessage);
    }

    /// <summary>
    /// Serializes the event message using the CSV event formatter.
    /// </summary>
    /// <returns>The serialized event message as a string.</returns>
    [Benchmark]
    public string Serialize_Csv()
    {
        return _csvFormatter.Serialize(_eventMessage);
    }

    /// <summary>
    /// Serializes the event message using the XML event formatter.
    /// </summary>
    /// <returns>The serialized event message as a string.</returns>
    [Benchmark]
    public string Serialize_Xml()
    {
        return _xmlFormatter.Serialize(_eventMessage);
    }
}
