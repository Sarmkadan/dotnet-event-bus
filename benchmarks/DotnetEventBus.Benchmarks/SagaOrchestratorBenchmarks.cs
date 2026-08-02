[MemoryDiagnoser]
public class SagaOrchestratorBenchmarks
{
    [Benchmark]
    public void BenchmarkMethod1()
    {
        // setup test data
        var testData = new List<string>();
        for (int i = 0; i < 100; i++)
        {
            testData.Add("testData" + i);
        }
        // method call
        var result = new SagaOrchestrator().Method1(testData);
    }

    [Benchmark]
    public void BenchmarkMethod2([Params(10)] int inputSize)
    {
        // setup test data
        var testData = new List<string>();
        for (int i = 0; i < inputSize; i++)
        {
            testData.Add("testData" + i);
        }
        // method call
        var result = new SagaOrchestrator().Method2(testData);
    }

    [Benchmark]
    public void BenchmarkMethod3()
    {
        // setup test data
        var testData = new Dictionary<string, string>();
        for (int i = 0; i < 100; i++)
        {
            testData.Add("testData" + i, "testData" + i);
        }
        // method call
        var result = new SagaOrchestrator().Method3(testData);
    }
}