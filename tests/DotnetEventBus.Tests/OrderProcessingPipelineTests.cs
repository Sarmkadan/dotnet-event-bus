#nullable enable

using FluentAssertions;
using Moq;
using Xunit;
using DotnetEventBus.Services;
using DotnetEventBus.Configuration;
using DotnetEventBus.Models;
using DotnetEventBus.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetEventBus.Tests;

/// <summary>
/// End-to-end scenario tests for order processing pipeline.
/// </summary>
public sealed class OrderProcessingPipelineTests
{
    [Fact]
    public async Task OrderProcessingPipeline_EndToEndScenario()
    {
        // Part 1: Happy path - chain completes and ordering preserved
        var services1 = new ServiceCollection();
        services1.AddEventBus(options =>
        {
            options.EnableDeadLetterQueue = true;
            options.AllowParallelHandling = false;
            options.MaxRetryAttempts = 2;
        });
        var provider1 = services1.BuildServiceProvider();
        var eventBus1 = provider1.GetRequiredService<IEventBus>();
        var deadLetterService1 = provider1.GetRequiredService<IDeadLetterService>();

        // Counters for part 1
        var orderCreatedHandlerACalled = 0;
        var orderCreatedHandlerBCalled = 0;
        var paymentRequestedHandlerCalled = 0;
        var paymentCompletedHandlerCalled = 0;
        var orderCreatedExecutionOrder = new List<string>();

        // Subscribe to OrderCreated with two handlers: A (high priority) and B (low priority)
        eventBus1.Subscribe<OrderCreated>(async (e, ct) =>
        {
            orderCreatedHandlerACalled++;
            orderCreatedExecutionOrder.Add("A");
            // Chain handler: publish PaymentRequested
            await eventBus1.PublishAsync(new PaymentRequested { OrderId = e.OrderId, Amount = 100 });
            await Task.CompletedTask;
        }, handlerName: "OrderCreatedHandlerA", priority: (int)HandlerPriority.High);

        eventBus1.Subscribe<OrderCreated>(async (e, ct) =>
        {
            orderCreatedHandlerBCalled++;
            orderCreatedExecutionOrder.Add("B");
            // Independent handler: just record
            await Task.CompletedTask;
        }, handlerName: "OrderCreatedHandlerB", priority: (int)HandlerPriority.Low);

        // Subscribe to PaymentRequested: handler that publishes PaymentCompleted
        eventBus1.Subscribe<PaymentRequested>(async (e, ct) =>
        {
            paymentRequestedHandlerCalled++;
            // Publish PaymentCompleted
            await eventBus1.PublishAsync(new PaymentCompleted { OrderId = e.OrderId, Success = true });
            await Task.CompletedTask;
        }, handlerName: "PaymentRequestedHandler");

        // Subscribe to PaymentCompleted: handler that records
        eventBus1.Subscribe<PaymentCompleted>(async (e, ct) =>
        {
            paymentCompletedHandlerCalled++;
            await Task.CompletedTask;
        }, handlerName: "PaymentCompletedHandler");

        // Act: Publish OrderCreated
        var orderCreatedEvent = new OrderCreated { OrderId = Guid.NewGuid(), Product = "TestProduct", Quantity = 1 };
        var result1 = await eventBus1.PublishAsync(orderCreatedEvent);

        // Assert part 1
        result1.Success.Should().BeTrue();
        orderCreatedHandlerACalled.Should().Be(1);
        orderCreatedHandlerBCalled.Should().Be(1);
        orderCreatedExecutionOrder.Should().ContainInOrder(new[] { "A", "B" });
        paymentRequestedHandlerCalled.Should().Be(1);
        paymentCompletedHandlerCalled.Should().Be(1);

        // Part 2: Failing handler scenario
        var services2 = new ServiceCollection();
        services2.AddEventBus(options =>
        {
            options.EnableDeadLetterQueue = true;
            options.AllowParallelHandling = false;
            options.MaxRetryAttempts = 2;
        });
        var provider2 = services2.BuildServiceProvider();
        var eventBus2 = provider2.GetRequiredService<IEventBus>();
        var deadLetterService2 = provider2.GetRequiredService<IDeadLetterService>();

        // Counters for part 2
        var orderCreatedHandlerACalled2 = 0;
        var orderCreatedHandlerBCalled2 = 0;
        var paymentRequestedHandlerCalled2 = 0;
        var paymentCompletedHandlerCalled2 = 0;

        // Subscribe to OrderCreated with two handlers (same as before)
        eventBus2.Subscribe<OrderCreated>(async (e, ct) =>
        {
            orderCreatedHandlerACalled2++;
            // Chain handler: publish PaymentRequested
            await eventBus2.PublishAsync(new PaymentRequested { OrderId = e.OrderId, Amount = 100 });
            await Task.CompletedTask;
        }, handlerName: "OrderCreatedHandlerA", priority: (int)HandlerPriority.High);

        eventBus2.Subscribe<OrderCreated>(async (e, ct) =>
        {
            orderCreatedHandlerBCalled2++;
            // Independent handler: just record
            await Task.CompletedTask;
        }, handlerName: "OrderCreatedHandlerB", priority: (int)HandlerPriority.Low);

        // Subscribe to PaymentRequested: handler that always throws
        eventBus2.Subscribe<PaymentRequested>(async (e, ct) =>
        {
            paymentRequestedHandlerCalled2++;
            await Task.CompletedTask;
            throw new InvalidOperationException("Simulated failure");
        }, handlerName: "FailingPaymentRequestedHandler");

        // Subscribe to PaymentCompleted: handler that records (should not be called)
        eventBus2.Subscribe<PaymentCompleted>(async (e, ct) =>
        {
            paymentCompletedHandlerCalled2++;
            await Task.CompletedTask;
        }, handlerName: "PaymentCompletedHandler2");

        // Act: Publish OrderCreated
        var orderCreatedEvent2 = new OrderCreated { OrderId = Guid.NewGuid(), Product = "TestProduct2", Quantity = 2 };
        var result2 = await eventBus2.PublishAsync(orderCreatedEvent2);

        // Assert part 2
        result2.Success.Should().BeTrue();
        orderCreatedHandlerACalled2.Should().Be(1);
        orderCreatedHandlerBCalled2.Should().Be(1);
        paymentRequestedHandlerCalled2.Should().Be(3); // initial + 2 retries
        paymentCompletedHandlerCalled2.Should().Be(0); // because the PaymentRequested handler always fails

        // Check dead letter queue
        var deadLetterEntries = await deadLetterService2.GetPendingEntriesAsync();
        deadLetterEntries.Should().ContainSingle();
        var entry = deadLetterEntries.First();
        entry.Message.EventType.Should().Be(typeof(PaymentRequested).FullName);
    }

    // Define the events used in the test
    public record OrderCreated
    {
        public Guid OrderId { get; init; }
        public string Product { get; set; } = default!;
        public int Quantity { get; set; }
    }

    public record PaymentRequested
    {
        public Guid OrderId { get; init; }
        public decimal Amount { get; init; }
    }

    public record PaymentCompleted
    {
        public Guid OrderId { get; init; }
        public bool Success { get; init; }
    }
}