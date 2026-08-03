using System;
using System.Collections.Concurrent;
using System.Linq;
using BenchmarkDotNet.Attributes;
using DotnetEventBus.Advanced;

namespace DotnetEventBus.Benchmarks
{
    [MemoryDiagnoser]
    public class MetricsCollectorBenchmarks
    {
        private MetricsCollector _metricsCollector = null!;
        private const int MaxSamplesPerEventType = 1000; // same as in MetricsCollector

        [Params(10, 100, 1000)]
        public int EventTypeCount;

        [Params(10, 100, 1000)]
        public int HandlerCount;

        [Params(100, 1000, 10000)]
        public int SampleCount;

        [GlobalSetup]
        public void Setup()
        {
            _metricsCollector = new MetricsCollector();

            // Pre-populate event types for RecordEventPublished benchmarks
            for (int i = 0; i < EventTypeCount; i++)
            {
                var eventType = $"EventType{i}";
                _metricsCollector.RecordEventPublished(eventType, 10);
            }

            // Pre-populate handler/event combinations for RecordHandlerExecution benchmarks
            for (int i = 0; i < HandlerCount; i++)
            {
                var handlerName = $"Handler{i}";
                var eventType = $"EventType{i}";
                _metricsCollector.RecordHandlerExecution(handlerName, eventType, 10, true);
            }

            // Pre-populate samples for GetLatencyStats benchmark
            var latencyEventType = "LatencyEvent";
            for (int i = 0; i < SampleCount; i++)
            {
                // Use varying durations to avoid compiler optimizations
                var duration = 10 + (i % 100); // varies between 10 and 109
                _metricsCollector.RecordEventPublished(latencyEventType, duration);
            }
        }

        [Benchmark]
        public void RecordEventPublished_ExistingType()
        {
            _metricsCollector.RecordEventPublished("EventType0", 10);
        }

        [Benchmark]
        public void RecordEventPublished_NewType()
        {
            _metricsCollector.RecordEventPublished($"NewEventType{Guid.NewGuid()}", 10);
        }

        [Benchmark]
        public void RecordHandlerExecution()
        {
            if (HandlerCount > 0)
            {
                _metricsCollector.RecordHandlerExecution("Handler0", "EventType0", 10, true);
            }
        }

        [Benchmark]
        public LatencyStats? GetLatencyStats()
        {
            return _metricsCollector.GetLatencyStats("LatencyEvent");
        }
    }
}