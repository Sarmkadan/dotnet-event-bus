[MemoryDiagnoser]
public class EventBusExceptionBenchmarks
{
    [GlobalSetup]
    public void Setup()
    {
        // Set up realistic test data here
    }

    [Benchmark]
    public void Benchmark_Exception_Throwing()
    {
        // Test throwing an EventBusException
    }

    [Benchmark]
    [Params(10, 100, 1000)]
    public void Benchmark_Exception_Handling()
    {
        // Test handling an EventBusException
    }

    [Benchmark]
    public void Benchmark_Exception_Serialization()
    {
        // Test serializing an EventBusException
    }
}