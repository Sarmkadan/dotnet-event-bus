[MemoryDiagnoser]
public class PredicateSubscriptionBuilderBenchmarks
{
    [Benchmark]
    public void Benchmark_PredicateSubscriptionBuilder_Create()
    {
        // setup test data
        var builder = new PredicateSubscriptionBuilder();
        var predicate = new Predicate();
        // benchmark
        for (int i = 0; i < 1000; i++)
        {
            builder.Create(predicate);
        }
    }

    [Benchmark]
    public void Benchmark_PredicateSubscriptionBuilder_Create_WithParams([Params(1000)])
    {
        // setup test data
        var builder = new PredicateSubscriptionBuilder();
        var predicate = new Predicate();
        // benchmark
        for (int i = 0; i < 1000; i++)
        {
            builder.Create(predicate, i);
        }
    }

    [Benchmark]
    public void Benchmark_PredicateSubscriptionBuilder_Create_WithParams_Multiple([Params(100, 1000)])
    {
        // setup test data
        var builder = new PredicateSubscriptionBuilder();
        var predicate = new Predicate();
        // benchmark
        for (int i = 0; i < 100; i++)
        {
            for (int j = 0; j < 1000; j++)
            {
                builder.Create(predicate, i);
            }
        }
    }
}