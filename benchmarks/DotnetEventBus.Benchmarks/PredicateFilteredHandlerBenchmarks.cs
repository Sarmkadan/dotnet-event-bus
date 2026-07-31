[MemoryDiagnoser]
public class PredicateFilteredHandlerBenchmarks
{
    [Benchmark]
    public void Benchmark_PredicateFilteredHandler_Filter()
    {
        // Setup test data
        var testHandler = new PredicateFilteredHandler();
        var testEvents = new List<Event>>();
        for (int i = 0; i < 100; i++)
        {
            testEvents.Add(new Event());
        }
        // Benchmark
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            testHandler.Handle(testEvents);
        }
        Debug.WriteLine("Average time per handle: " + sw.ElapsedMilliseconds / 1000);
    }

    [Benchmark]
    [Params(10, 100, 1000)]
    public void Benchmark_PredicateFilteredHandler_Filter_Params([Params] int eventsCount)
    {
        // Setup test data
        var testHandler = new PredicateFilteredHandler();
        var testEvents = new List<Event>>();
        for (int i = 0; i < eventsCount; i++)
        {
            testEvents.Add(new Event());
        }
        // Benchmark
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < eventsCount; i++)
        {
            testHandler.Handle(testEvents);
        }
        Debug.WriteLine("Average time per handle: " + sw.ElapsedMilliseconds / eventsCount);
    }
}