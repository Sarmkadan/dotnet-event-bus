[MemoryDiagnoser]
public class HttpEventPublisherBenchmarks
{
    [GlobalSetup]
    public void Setup()
    {
        // setup test data
    }

    [Benchmark]
    public void Benchmark_Method1()
    {
        // benchmark method 1
    }

    [Benchmark]
    [Params(10, 100, 1000)]
    public void Benchmark_Method2(int inputSize)
    {
        // benchmark method 2
    }

    [Benchmark]
    public void Benchmark_Method3()
    {
        // benchmark method 3
    }
}
