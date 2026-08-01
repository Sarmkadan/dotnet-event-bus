using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DotnetEventBus.Integration;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotnetEventBus.Benchmarks
{
    [MemoryDiagnoser]
    public class WebhookHandlerBenchmarks
    {
        private WebhookHandler _handler = null!;
        private WebhookSubscription _subscription = null!;
        private List<WebhookSubscription> _subscriptions = null!;
        private string _payload = null!;
        private string _signature = null!;
        private const string SigningSecret = "test-secret-123";

        [GlobalSetup]
        public void GlobalSetup()
        {
            _handler = new WebhookHandler(SigningSecret, NullLogger<WebhookHandler>.Instance);
            _subscription = new WebhookSubscription
            {
                Url = "https://example.com/webhook",
                EventTypes = new List<string> { "order.created", "order.updated" }
            };
            _handler.Subscribe(_subscription);

            // Create multiple subscriptions for GetWebhooksForEvent benchmark
            _subscriptions = new List<WebhookSubscription>();
            var eventTypes = new[] { "order.created", "order.updated", "order.deleted", "user.signup", "user.login" };
            foreach (var eventType in eventTypes)
            {
                _subscriptions.Add(new WebhookSubscription
                {
                    Url = $"https://example.com/webhook/{eventType}",
                    EventTypes = new List<string> { eventType, "*" }
                });
                _handler.Subscribe(_subscriptions[_subscriptions.Count - 1]);
            }

            _payload = "{\"id\":123,\"name\":\"test\",\"data\":{\"key\":\"value\"}}";
            _signature = _handler.GenerateSignature(_payload);
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            // Clear subscriptions added during benchmarks to avoid interference
            _handler.GetAllSubscriptions().Clear();
            // Re-add the base subscription for next iteration
            _handler.Subscribe(_subscription);
            // Re-add the GetWebhooksForEvent subscriptions
            foreach (var sub in _subscriptions)
            {
                _handler.Subscribe(sub);
            }
        }

        [Benchmark]
        public void BenchmarkSubscribe()
        {
            var subscription = new WebhookSubscription
            {
                Url = $"https://example.com/webhook/{Guid.NewGuid()}",
                EventTypes = new List<string> { "test.event" }
            };
            _handler.Subscribe(subscription);
        }

        [Benchmark]
        public bool BenchmarkUnsubscribe()
        {
            // Create a subscription to remove in this iteration
            var subscription = new WebhookSubscription
            {
                Url = "https://example.com/webhook/unsubscribe-test",
                EventTypes = new List<string> { "test.event" }
            };
            _handler.Subscribe(subscription);
            var result = _handler.Unsubscribe(subscription.Id!);
            // Cleanup: if not removed (shouldn't happen), remove it to keep state clean
            if (!result && subscription.Id != null)
            {
                _handler.Unsubscribe(subscription.Id);
            }
            return result;
        }

        [Benchmark]
        public IEnumerable<WebhookSubscription> BenchmarkGetWebhooksForEvent()
        {
            // Return subscriptions for a specific event type
            return _handler.GetWebhooksForEvent("order.created");
        }

        [Benchmark]
        public string BenchmarkGenerateSignature()
        {
            return _handler.GenerateSignature(_payload);
        }

        [Benchmark]
        public bool BenchmarkVerifySignature()
        {
            return _handler.VerifySignature(_payload, _signature);
        }

        // Benchmark with varying payload sizes
        [Params(100, 1000, 10000)]
        public int PayloadSize { get; set; }

        private string _largePayload = null!;

        [IterationSetup]
        public void GeneratePayload()
        {
            _largePayload = new string('x', PayloadSize);
        }

        [Benchmark]
        public string BenchmarkGenerateSignatureLargePayload()
        {
            return _handler.GenerateSignature(_largePayload);
        }

        [Benchmark]
        public bool BenchmarkVerifySignatureLargePayload()
        {
            var signature = _handler.GenerateSignature(_largePayload);
            return _handler.VerifySignature(_largePayload, signature);
        }
    }
}