#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using DotnetEventBus.Cli;
using DotnetEventBus.Models;
using DotnetEventBus.Services;

namespace DotnetEventBus.Benchmarks
{
    /// <summary>
    /// Benchmarks for the <see cref="PublishCommand"/> CLI command.
    /// </summary>
    [MemoryDiagnoser]
    public class PublishCommandBenchmarks
    {
        private PublishCommand? _command;

        // The size of the JSON payload (number of objects in an array)
        [Params(10, 100, 1000)]
        public int PayloadSize { get; set; }

        private string[]? _argsWithoutMetadata;
        private string[]? _argsWithMetadata;

        /// <summary>
        /// Sets up a <see cref="PublishCommand"/> instance with a lightweight test event bus
        /// and prepares argument arrays for the benchmarks.
        /// </summary>
        [GlobalSetup]
        public void GlobalSetup()
        {
            // Create a test event bus that always reports success.
            var testBus = new TestEventBus();
            _command = new PublishCommand(testBus);

            // Build a JSON payload consisting of an array of simple objects.
            var items = new List<Dictionary<string, object>>(PayloadSize);
            for (int i = 0; i < PayloadSize; i++)
            {
                items.Add(new Dictionary<string, object>
                {
                    { "id", i },
                    { "value", $"value{i}" }
                });
            }

            string jsonPayload = JsonSerializer.Serialize(items);
            _argsWithoutMetadata = new[] { "test.event", jsonPayload };

            // Build arguments that also include a few metadata key/value pairs.
            var argsWithMeta = new List<string> { "test.event", jsonPayload };
            for (int i = 0; i < 5; i++)
            {
                argsWithMeta.Add("--metadata");
                argsWithMeta.Add($"key{i}=value{i}");
            }

            _argsWithMetadata = argsWithMeta.ToArray();
        }

        /// <summary>
        /// Benchmarks the core execution path of the command without any metadata.
        /// </summary>
        [Benchmark]
        public async Task ExecuteAsync_NoMetadata()
        {
            if (_command == null || _argsWithoutMetadata == null) throw new InvalidOperationException();
            await _command.ExecuteAsync(_argsWithoutMetadata);
        }

        /// <summary>
        /// Benchmarks the execution path of the command when metadata arguments are supplied.
        /// </summary>
        [Benchmark]
        public async Task ExecuteAsync_WithMetadata()
        {
            if (_command == null || _argsWithMetadata == null) throw new InvalidOperationException();
            await _command.ExecuteAsync(_argsWithMetadata);
        }

        /// <summary>
        /// Benchmarks the generation of the help text (a trivial method, included for completeness).
        /// </summary>
        [Benchmark]
        public string GetHelpText()
        {
            if (_command == null) throw new InvalidOperationException();
            return _command.GetHelpText();
        }

        #region TestEventBus

        /// <summary>
        /// Minimal implementation of <see cref="IEventBus"/> used only for benchmarking.
        /// All members except <see cref="PublishAsync"/> throw <see cref="NotImplementedException"/>.
        /// </summary>
        private sealed class TestEventBus : IEventBus
        {
            public Task<PublishResult> PublishAsync(EventEnvelope envelope)
            {
                // Simulate a successful publish without any real processing.
                var result = new PublishResult
                {
                    Success = true,
                    HandlersInvoked = 0,
                    FailedHandlers = 0,
                    ErrorMessage = null
                };
                return Task.FromResult(result);
            }

            // The remaining members are not needed for the benchmarks.
            // They are implemented to satisfy the interface contract.

            public Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request, string? correlationId = null, TimeSpan? timeout = null)
                => throw new NotImplementedException();

            public IDisposable SubscribeSync<TEvent>(Action<TEvent> handler, string? handlerName = null)
                => throw new NotImplementedException();

            public IDisposable SubscribeRequest<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler, string? handlerName = null)
                => throw new NotImplementedException();

            public Task<PublishResult> ProcessRawDistributedEventAsync(string eventType, string rawPayload, string? correlationId = null)
                => throw new NotImplementedException();

            public Task<PublishResult> PublishAsync(EventMessage message)
                => throw new NotImplementedException();

            public Task<PublishResult> PublishAsync<TEvent>(TEvent @event, string? correlationId = null)
                => throw new NotImplementedException();

            public Task<PublishResult> PublishAsync<TEvent>(TEvent @event, IDictionary<string, string>? metadata, string? correlationId = null)
                => throw new NotImplementedException();

            public Task<PublishResult> PublishAsync<TEvent>(TEvent @event, string? correlationId = null, bool isDistributed = false)
                => throw new NotImplementedException();

            public Task<PublishResult> PublishAsync<TEvent>(TEvent @event, IDictionary<string, string>? metadata, string? correlationId = null, bool isDistributed = false)
                => throw new NotImplementedException();

            public Task<PublishResult> PublishAsync(EventEnvelope envelope, bool isDistributed = false)
                => throw new NotImplementedException();

            public Task<PublishResult> PublishAsync(EventEnvelope envelope, IDictionary<string, string>? metadata, bool isDistributed = false)
                => throw new NotImplementedException();

            public Task<PublishResult> PublishAsync(EventEnvelope envelope, string? correlationId, bool isDistributed = false)
                => throw new NotImplementedException();

            public Task<PublishResult> PublishAsync(EventEnvelope envelope, IDictionary<string, string>? metadata, string? correlationId, bool isDistributed = false)
                => throw new NotImplementedException();
        }

        #endregion
    }
}
