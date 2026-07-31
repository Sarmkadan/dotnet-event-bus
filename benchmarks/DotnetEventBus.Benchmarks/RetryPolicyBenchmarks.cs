using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Diagnostics;
using BenchmarkDotNet.Diagnostics.Memory;
using BenchmarkDotNet.Engines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotnetEventBus;
using DotnetEventBus.Integration;

namespace DotnetEventBus.Benchmarks
{
    [MemoryDiagnoser]
    public class RetryPolicyBenchmarks
    {
        [GlobalSetup]
        public void Setup()
        {
            // setup test data
        }

        [Benchmark]
        public void Benchmark_RetryPolicy_WithNoRetries()
        {
            // benchmark code
        }

        [Benchmark]
        [Params(10, 100, 1000)]
        public void Benchmark_RetryPolicy_WithRetries()
        {
            // benchmark code
        }

        [Benchmark]
        public void Benchmark_RetryPolicy_WithExponentialBackoff()
        {
            // benchmark code
        }
    }
}