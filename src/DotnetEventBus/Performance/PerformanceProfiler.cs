#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DotnetEventBus.Performance;

/// <summary>
/// Profiles event bus performance with timing and throughput metrics.
/// Helps identify bottlenecks and optimize event processing.
/// Why: Data-driven optimization requires accurate performance measurements.
/// </summary>
public sealed class PerformanceProfiler
{
    private readonly Dictionary<string, List<long>> _timings = [];
    private readonly Stopwatch _sessionStopwatch = Stopwatch.StartNew();
    private long _totalOperations = 0;

    /// <summary>
    /// Profiles a synchronous operation.
    /// </summary>
    public T Profile<T>(string operationName, Func<T> operation)
    {
        ArgumentNullException.ThrowIfNull(operationName);
        ArgumentNullException.ThrowIfNull(operation);

        var sw = Stopwatch.StartNew();
        try
        {
            return operation();
        }
        finally
        {
            sw.Stop();
            RecordTiming(operationName, sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Profiles an asynchronous operation.
    /// </summary>
    public async Task<T> ProfileAsync<T>(string operationName, Func<Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operationName);
        ArgumentNullException.ThrowIfNull(operation);

        var sw = Stopwatch.StartNew();
        try
        {
            return await operation();
        }
        finally
        {
            sw.Stop();
            RecordTiming(operationName, sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Profiles an operation that doesn't return a value.
    /// </summary>
    public void Profile(string operationName, Action operation)
    {
        ArgumentNullException.ThrowIfNull(operationName);
        ArgumentNullException.ThrowIfNull(operation);

        var sw = Stopwatch.StartNew();
        try
        {
            operation();
        }
        finally
        {
            sw.Stop();
            RecordTiming(operationName, sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Gets statistics for a specific operation.
    /// </summary>
    public OperationStats? GetStats(string operationName)
    {
        if (!_timings.TryGetValue(operationName, out var timings) || timings.Count == 0)
            return null;

        return new OperationStats
        {
            OperationName = operationName,
            ExecutionCount = timings.Count,
            TotalTimeMs = timings.Sum(),
            AverageTimeMs = (double)timings.Sum() / timings.Count,
            MinTimeMs = timings.Min(),
            MaxTimeMs = timings.Max(),
            MedianTimeMs = GetMedian(timings),
            P95TimeMs = GetPercentile(timings, 95),
            P99TimeMs = GetPercentile(timings, 99)
        };
    }

    /// <summary>
    /// Gets statistics for all profiled operations.
    /// </summary>
    public IEnumerable<OperationStats> GetAllStats()
    {
        return _timings.Keys.Select(op => GetStats(op)).Where(s => s is not null)!;
    }

    /// <summary>
    /// Gets a summary of profiling session.
    /// </summary>
    public ProfilingSessionSummary GetSummary()
    {
        var allTimings = _timings.Values.SelectMany(t => t).ToList();
        var sessionDuration = _sessionStopwatch.Elapsed;

        return new ProfilingSessionSummary
        {
            SessionDuration = sessionDuration,
            OperationCount = _timings.Count,
            TotalExecutions = allTimings.Count,
            TotalTimeMs = allTimings.Sum(),
            AverageTimeMs = allTimings.Count > 0 ? (double)allTimings.Sum() / allTimings.Count : 0,
            ThroughputPerSecond = allTimings.Count > 0 ? (allTimings.Count / sessionDuration.TotalSeconds) : 0
        };
    }

    /// <summary>
    /// Resets all profiling data.
    /// </summary>
    public void Reset()
    {
        _timings.Clear();
        _sessionStopwatch.Restart();
        _totalOperations = 0;
    }

    /// <summary>
    /// Generates a detailed performance report.
    /// </summary>
    public string GenerateReport()
    {
        var summary = GetSummary();
        var stats = GetAllStats().OrderByDescending(s => s.TotalTimeMs).ToList();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Performance Profiling Report ===");
        sb.AppendLine();
        sb.AppendLine($"Session Duration: {summary.SessionDuration:hh\\:mm\\:ss\\.fff}");
        sb.AppendLine($"Total Operations: {summary.OperationCount}");
        sb.AppendLine($"Total Executions: {summary.TotalExecutions}");
        sb.AppendLine($"Total Time: {summary.TotalTimeMs}ms");
        sb.AppendLine($"Average Time: {summary.AverageTimeMs:F2}ms");
        sb.AppendLine($"Throughput: {summary.ThroughputPerSecond:F2} ops/sec");
        sb.AppendLine();
        sb.AppendLine("=== Operation Statistics ===");

        foreach (var stat in stats)
        {
            sb.AppendLine();
            sb.AppendLine($"Operation: {stat.OperationName}");
            sb.AppendLine($"  Executions: {stat.ExecutionCount}");
            sb.AppendLine($"  Total Time: {stat.TotalTimeMs}ms");
            sb.AppendLine($"  Average: {stat.AverageTimeMs:F2}ms");
            sb.AppendLine($"  Min: {stat.MinTimeMs}ms");
            sb.AppendLine($"  Max: {stat.MaxTimeMs}ms");
            sb.AppendLine($"  Median: {stat.MedianTimeMs:F2}ms");
            sb.AppendLine($"  P95: {stat.P95TimeMs:F2}ms");
            sb.AppendLine($"  P99: {stat.P99TimeMs:F2}ms");
        }

        return sb.ToString();
    }

    private void RecordTiming(string operationName, long elapsedMs)
    {
        if (!_timings.ContainsKey(operationName))
        {
            _timings[operationName] = [];
        }

        _timings[operationName].Add(elapsedMs);
        Interlocked.Increment(ref _totalOperations);
    }

    private static double GetMedian(List<long> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        int count = sorted.Count;

        if (count % 2 == 0)
        {
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
        }

        return sorted[count / 2];
    }

    private static double GetPercentile(List<long> values, int percentile)
    {
        var sorted = values.OrderBy(v => v).ToList();
        int index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return index >= 0 ? sorted[index] : 0;
    }
}

public sealed class OperationStats
{
    public string? OperationName { get; set; }
    public int ExecutionCount { get; set; }
    public long TotalTimeMs { get; set; }
    public double AverageTimeMs { get; set; }
    public long MinTimeMs { get; set; }
    public long MaxTimeMs { get; set; }
    public double MedianTimeMs { get; set; }
    public double P95TimeMs { get; set; }
    public double P99TimeMs { get; set; }
}

public sealed class ProfilingSessionSummary
{
    public TimeSpan SessionDuration { get; set; }
    public int OperationCount { get; set; }
    public int TotalExecutions { get; set; }
    public long TotalTimeMs { get; set; }
    public double AverageTimeMs { get; set; }
    public double ThroughputPerSecond { get; set; }
}
