using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using DotnetEventBus.Cli;

namespace DotnetEventBus.Benchmarks;

[MemoryDiagnoser]
public class StatsCommandBenchmarks
{
    private StatsCommand _command = null!;

    // --------------------------------------------------------------------
    // Global setup – creates a StatsCommand instance using the parameter‑less
    // constructor. This avoids the need for a real MetricsCollector and keeps
    // the benchmark self‑contained.
    // --------------------------------------------------------------------
    [GlobalSetup]
    public void GlobalSetup()
    {
        _command = new StatsCommand();
    }

    // --------------------------------------------------------------------
    // Benchmark the help‑text generation. This is a pure string operation
    // but still useful to measure allocation patterns.
    // --------------------------------------------------------------------
    [Benchmark]
    public string GetHelpText()
    {
        return _command.GetHelpText();
    }

    // --------------------------------------------------------------------
    // Benchmark ExecuteAsync when no arguments are supplied. This follows
    // the early‑exit path (no MetricsCollector attached) and measures the
    // async plumbing overhead.
    // --------------------------------------------------------------------
    [Benchmark]
    public async Task ExecuteAsync_NoArgs()
    {
        // The result is ignored – we only care about the execution time.
        await _command.ExecuteAsync(Array.Empty<string>());
    }

    // --------------------------------------------------------------------
    // Parameterised benchmark for ExecuteAsync with different command
    // arguments. The Params attribute drives the variation.
    // --------------------------------------------------------------------
    [Params("system", "events", "handlers", "health", "unknown")]
    public string Arg { get; set; } = "system";

    [Benchmark]
    public async Task ExecuteAsync_WithArg()
    {
        await _command.ExecuteAsync(new[] { Arg });
    }
}
