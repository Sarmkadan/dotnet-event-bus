using BenchmarkDotNet.Attributes;
using DotnetEventBus.Advanced;
using System;
using System.Collections.Generic;

namespace DotnetEventBus.Benchmarks;

[MemoryDiagnoser]
public class EventSourcedAggregateBenchmarks
{
    [Params(10, 100, 1000)]
    public int EventCount { get; set; }

    private List<object> _events = null!;
    private AggregateSnapshot _snapshot = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Prepare events for LoadFromHistory benchmark
        _events = new List<object>(EventCount);
        for (int i = 0; i < EventCount; i++)
        {
            _events.Add(new TestEvent($"Name-{i}", i));
        }

        // Prepare snapshot for LoadSnapshot/CreateSnapshot benchmarks
        var state = new Dictionary<string, object?>
        {
            { "Id", "aggregate-123" },
            { "Name", "SnapshotName" },
            { "Value", 999 }
        };

        _snapshot = new AggregateSnapshot
        {
            AggregateId = "aggregate-123",
            AggregateType = nameof(TestAggregate),
            Version = EventCount,
            CreatedAt = DateTime.UtcNow,
            State = state
        };
    }

    [Benchmark]
    public void LoadFromHistory()
    {
        var aggregate = new TestAggregate("init-id");
        aggregate.LoadFromHistory(_events);
    }

    [Benchmark]
    public void LoadSnapshot()
    {
        var aggregate = new TestAggregate("init-id");
        aggregate.LoadSnapshot(_snapshot);
    }

    [Benchmark]
    public void CreateSnapshot()
    {
        var aggregate = new TestAggregate("snapshot-id");
        // Ensure the aggregate has state to capture
        aggregate.LoadSnapshot(_snapshot);
        aggregate.CreateSnapshot();
    }

    // Helper classes for benchmarking
    private sealed class TestAggregate : EventSourcedAggregate
    {
        public TestAggregate(string id) => Id = id;

        public string? Name { get; private set; }
        public int Value { get; private set; }

        // This method is called via reflection by ApplyEvent
        private void Apply(TestEvent @event)
        {
            Name = @event.Name;
            Value = @event.Value;
        }
    }

    private sealed record TestEvent(string Name, int Value);
}
