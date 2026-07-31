[MemoryDiagnoser]
public class EventBusBuilderBenchmarks
{
    [GlobalSetup]
    public void Setup()
    {
        // setup test data here
    }

    [Benchmark]
    public void BenchmarkMethod1()
    {
        // test method 1
    }

    [Benchmark]
    public void BenchmarkMethod2()
    {
        // test method 2
    }

    [Benchmark]
    [Params(10, 100, 1000)]
    public void BenchmarkMethod3()
    {
        // test method 3
    }
}