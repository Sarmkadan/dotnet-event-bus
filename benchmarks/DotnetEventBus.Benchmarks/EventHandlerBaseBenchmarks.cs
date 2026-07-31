using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Diagnostics;
using BenchmarkDotNet.Diagnostics.Memory;
using System.Collections.Generic;
using System.Linq;
using DotnetEventBus;
using DotnetEventBus.Handlers;

namespace DotnetEventBus.Benchmarks
{
    [MemoryDiagnoser]
    public class EventHandlerBaseBenchmarks
    {
        [GlobalSetup]
        public void Setup()
        {
            // TODO: set up test data
        }

        [Benchmark]
        public void Benchmark_HandlerBase_HandleEvent()
        {
            // TODO: implement benchmark
        }

        [Benchmark]
        [Params(100)]
        public void Benchmark_HandlerBase_HandleEvent_LargeInput()
        {
            // TODO: implement benchmark
        }

        [Benchmark]
        [Params(1000)]
        public void Benchmark_HandlerBase_HandleEvent_LargeInput_Multiple()
        {
            // TODO: implement benchmark
        }
    }
}