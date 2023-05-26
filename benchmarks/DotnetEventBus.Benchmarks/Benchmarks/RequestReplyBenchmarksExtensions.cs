using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using DotnetEventBus.Benchmarks;

namespace DotnetEventBus.Benchmarks;

/// <summary>
/// Extension methods for RequestReplyBenchmarks providing additional benchmarking utilities
/// and convenience methods for common benchmarking scenarios.
/// </summary>
public static class RequestReplyBenchmarksExtensions
{
    /// <summary>
    /// Creates a new RequestReplyBenchmarks instance and runs a single benchmark iteration.
    /// Useful for quick validation of benchmark setup.
    /// </summary>
    /// <param name="benchmark">The benchmarks instance</param>
    /// <param name="benchmarkName">Name of the benchmark to run</param>
    /// <returns>Task representing the benchmark execution</returns>
    public static async Task RunSingleBenchmarkAsync(this RequestReplyBenchmarks benchmark, string benchmarkName)
    {
        _ = benchmark ?? throw new ArgumentNullException(nameof(benchmark));

        switch (benchmarkName)
        {
            case nameof(RequestReplyBenchmarks.Request_Reply_Single):
                await benchmark.Request_Reply_Single();
                break;
            case nameof(RequestReplyBenchmarks.Request_Reply_10_Sequential):
                await benchmark.Request_Reply_10_Sequential();
                break;
            case nameof(RequestReplyBenchmarks.Request_Reply_10_Parallel):
                await benchmark.Request_Reply_10_Parallel();
                break;
            case nameof(RequestReplyBenchmarks.Request_Reply_With_Timeout):
                await benchmark.Request_Reply_With_Timeout();
                break;
            case nameof(RequestReplyBenchmarks.Request_Reply_Large_Payload):
                await benchmark.Request_Reply_Large_Payload();
                break;
            default:
                throw new ArgumentException($"Unknown benchmark: {benchmarkName}", nameof(benchmarkName));
        }
    }

    /// <summary>
    /// Runs all request-reply benchmarks sequentially.
    /// Useful for comprehensive validation or when running benchmarks in constrained environments.
    /// </summary>
    /// <param name="benchmark">The benchmarks instance</param>
    /// <returns>Dictionary mapping benchmark names to their execution tasks</returns>
    public static Dictionary<string, Task> RunAllBenchmarksSequentially(this RequestReplyBenchmarks benchmark)
    {
        _ = benchmark ?? throw new ArgumentNullException(nameof(benchmark));

        var results = new Dictionary<string, Task>();

        results[nameof(RequestReplyBenchmarks.Request_Reply_Single)] = benchmark.Request_Reply_Single();
        results[nameof(RequestReplyBenchmarks.Request_Reply_10_Sequential)] = benchmark.Request_Reply_10_Sequential();
        results[nameof(RequestReplyBenchmarks.Request_Reply_10_Parallel)] = benchmark.Request_Reply_10_Parallel();
        results[nameof(RequestReplyBenchmarks.Request_Reply_With_Timeout)] = benchmark.Request_Reply_With_Timeout();
        results[nameof(RequestReplyBenchmarks.Request_Reply_Large_Payload)] = benchmark.Request_Reply_Large_Payload();

        return results;
    }

    /// <summary>
    /// Runs a specified number of sequential request-reply operations with configurable batch size.
    /// Useful for measuring throughput scaling with different batch sizes.
    /// </summary>
    /// <param name="benchmark">The benchmarks instance</param>
    /// <param name="batchSize">Number of requests to send sequentially</param>
    /// <returns>Task representing the batch execution</returns>
    public static async Task Request_Reply_Batch_Sequential(this RequestReplyBenchmarks benchmark, int batchSize)
    {
        _ = benchmark ?? throw new ArgumentNullException(nameof(benchmark));
        _ = batchSize > 0 ? batchSize : throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be positive");

        for (int i = 0; i < batchSize; i++)
        {
            var request = new RequestReplyBenchmarks.RequestEvent
            {
                RequestId = $"req-batch-seq-{i}",
                Payload = $"BatchTest{i}",
                Value = i * 10
            };

            await benchmark.RequestAsync<RequestReplyBenchmarks.RequestEvent, RequestReplyBenchmarks.ResponseEvent>(request);
        }
    }

    /// <summary>
    /// Runs a specified number of parallel request-reply operations with configurable concurrency level.
    /// Useful for measuring maximum throughput and identifying saturation points.
    /// </summary>
    /// <param name="benchmark">The benchmarks instance</param>
    /// <param name="concurrencyLevel">Number of concurrent requests to send</param>
    /// <returns>Task representing the parallel execution</returns>
    public static async Task Request_Reply_Batch_Parallel(this RequestReplyBenchmarks benchmark, int concurrencyLevel)
    {
        _ = benchmark ?? throw new ArgumentNullException(nameof(benchmark));
        _ = concurrencyLevel > 0 ? concurrencyLevel : throw new ArgumentOutOfRangeException(nameof(concurrencyLevel), "Concurrency level must be positive");

        var tasks = new List<Task<RequestReplyBenchmarks.ResponseEvent>>(concurrencyLevel);

        for (int i = 0; i < concurrencyLevel; i++)
        {
            var request = new RequestReplyBenchmarks.RequestEvent
            {
                RequestId = $"req-batch-par-{i}",
                Payload = $"BatchParallelTest{i}",
                Value = i * 100
            };

            tasks.Add(benchmark.RequestAsync<RequestReplyBenchmarks.RequestEvent, RequestReplyBenchmarks.ResponseEvent>(request));
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Validates that the benchmark infrastructure is properly configured by running a quick smoke test.
    /// Checks that request IDs are properly propagated and responses are received correctly.
    /// </summary>
    /// <param name="benchmark">The benchmarks instance</param>
    /// <param name="testRequestId">Custom request ID to use for validation</param>
    /// <returns>Task representing the validation</returns>
    public static async Task ValidateBenchmarkInfrastructureAsync(this RequestReplyBenchmarks benchmark, string testRequestId = "validation-test")
    {
        _ = benchmark ?? throw new ArgumentNullException(nameof(benchmark));
        _ = !string.IsNullOrWhiteSpace(testRequestId) ? testRequestId : throw new ArgumentException("Request ID cannot be null or whitespace", nameof(testRequestId));

        var request = new RequestReplyBenchmarks.RequestEvent
        {
            RequestId = testRequestId,
            Payload = "ValidationPayload",
            Value = 12345
        };

        var response = await benchmark.RequestAsync<RequestReplyBenchmarks.RequestEvent, RequestReplyBenchmarks.ResponseEvent>(request);

        if (response.RequestId != testRequestId)
        {
            throw new InvalidOperationException($"Request ID mismatch: expected '{testRequestId}', got '{response.RequestId}'");
        }

        if (response.ProcessedValue != request.Value * 2)
        {
            throw new InvalidOperationException($"Value processing failed: expected {request.Value * 2}, got {response.ProcessedValue}");
        }

        if (response.Result != "Processed")
        {
            throw new InvalidOperationException($"Unexpected result: expected 'Processed', got '{response.Result}'");
        }
    }
}