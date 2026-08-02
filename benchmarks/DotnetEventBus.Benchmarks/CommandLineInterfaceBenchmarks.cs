[Benchmark]
[Benchmark(MinTime = 100, MaxTime = 1000)]
[MemoryDiagnoser]
public class CommandLineInterfaceBenchmarks
{
    [GlobalSetup]
    public void Setup()
    {
        // Initialize test data here
    }

    [Benchmark]
    [Params(10, 100, 1000)]
    public void Benchmark_Method1()
    {
        // Benchmark code for the first method
    }

    [Benchmark]
    [Params(10, 100, 1000)]
    public void Benchmark_Method2()
    {
        // Benchmark code for the second method
    }

    [Benchmark]
    [Params(10, 100, 1000)]
    public void Benchmark_Method3()
    {
        // Benchmark code for the third method
    }
}
