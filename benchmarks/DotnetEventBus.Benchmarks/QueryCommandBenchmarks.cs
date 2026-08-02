[Benchmark]
[Benchmark(MinTime = 100, MaxTime = 1000)]
[MemoryDiagnoser]
public class QueryCommandBenchmarks
{
    [GlobalSetup]
    public void Setup()
    {
        // Initialize test data here
    }

    [Benchmark]
    public void Benchmark_QueryCommand_SmallInputSize()
    {
        // Test QueryCommand with small input size (e.g., 10)
    }

    [Benchmark]
    [Params(10, 100, 1000)]
    public void Benchmark_QueryCommand_MediumInputSize()
    {
        // Test QueryCommand with medium input size (e.g., 100)
    }

    [Benchmark]
    [Params(10, 100, 1000)]
    public void Benchmark_QueryCommand_LargeInputSize()
    {
        // Test QueryCommand with large input size (e.g., 1000)
    }
}
