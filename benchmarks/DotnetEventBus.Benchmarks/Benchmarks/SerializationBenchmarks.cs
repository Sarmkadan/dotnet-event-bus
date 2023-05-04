using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotnetEventBus.Formatters;
using DotnetEventBus.Models;

namespace DotnetEventBus.Benchmarks;

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SerializationBenchmarks
{
    private readonly JsonEventFormatter _jsonFormatter = new JsonEventFormatter();
    private readonly CsvEventFormatter _csvFormatter = new CsvEventFormatter();
    private readonly XmlEventFormatter _xmlFormatter = new XmlEventFormatter();
    
    private EventMessage _eventMessage;

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

    [Benchmark]
    public string Serialize_Json()
    {
        return _jsonFormatter.Serialize(_eventMessage);
    }

    [Benchmark]
    public string Serialize_Csv()
    {
        return _csvFormatter.Serialize(_eventMessage);
    }

    [Benchmark]
    public string Serialize_Xml()
    {
        return _xmlFormatter.Serialize(_eventMessage);
    }
}
