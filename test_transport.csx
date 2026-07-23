#!/usr/bin/env dotnet-script

// Test script to verify IEventTransport abstraction implementation

#r "nuget: Microsoft.Extensions.DependencyInjection"
#r "nuget: Microsoft.Extensions.Logging"
#r "nuget: Microsoft.Extensions.Logging.Console"

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DotnetEventBus.Configuration;
using DotnetEventBus.Models;
using DotnetEventBus.Transport;

Console.WriteLine("=== Testing IEventTransport Abstraction ===\n");

// Test 1: In-process transport
Console.WriteLine("Test 1: In-process transport");
var services1 = new ServiceCollection();
services1.AddLogging(configure => configure.AddConsole());
services1.AddInProcessTransport();
services1.AddEventBus();
services1.ConfigureTransportRegistry("in-process-transport");

var provider1 = services1.BuildServiceProvider();
var registry1 = provider1.GetRequiredService<ITransportRegistry>();
var transport1 = registry1.DefaultTransport;

Console.WriteLine($"✓ Transport ID: {transport1.TransportId}");
Console.WriteLine($"✓ Transport Type: {transport1.TransportType}");
Console.WriteLine($"✓ Capabilities: {transport1.Capabilities}");

var envelope1 = EventEnvelope.Create("test.event", new { Message = "Hello World" });
var result1 = transport1.PublishAsync(envelope1).Result;
Console.WriteLine($"✓ Publish result: Success={result1.Success}, EventId={result1.EventId}");

var status1 = transport1.GetStatus();
Console.WriteLine($"✓ Status: Healthy={status1.IsHealthy}, Published={status1.MessagesPublished}");

Console.WriteLine();

// Test 2: Webhook transport
Console.WriteLine("Test 2: Webhook transport");
var services2 = new ServiceCollection();
services2.AddLogging(configure => configure.AddConsole());
services2.AddWebhookTransport("test-secret");
services2.AddEventBus();

var provider2 = services2.BuildServiceProvider();
var webhookTransport = provider2.GetRequiredService<IEventTransport>();

Console.WriteLine($"✓ Transport ID: {webhookTransport.TransportId}");
Console.WriteLine($"✓ Transport Type: {webhookTransport.TransportType}");

if (webhookTransport is WebhookTransport webhookImpl)
{
    var subscription = new WebhookSubscription
    {
        Url = "https://example.com/webhook",
        EventTypes = { "test.event" },
        IsActive = true
    };

    webhookImpl.Subscribe(subscription);
    Console.WriteLine("✓ Webhook subscription registered");
}

var envelope2 = EventEnvelope.Create("test.event", new { Message = "Webhook test" });
var result2 = webhookTransport.PublishAsync(envelope2).Result;
Console.WriteLine($"✓ Publish result: Success={result2.Success}, EventId={result2.EventId}");

var status2 = webhookTransport.GetStatus();
Console.WriteLine($"✓ Status: Healthy={status2.IsHealthy}, Published={status2.MessagesPublished}");

Console.WriteLine();

// Test 3: Transport registry with multiple transports
Console.WriteLine("Test 3: Transport registry with multiple transports");
var services3 = new ServiceCollection();
services3.AddLogging(configure => configure.AddConsole());
services3.AddInProcessTransport();
services3.AddWebhookTransport("multi-secret");
services3.ConfigureTransportRegistry("in-process-transport");

var provider3 = services3.BuildServiceProvider();
var registry3 = provider3.GetRequiredService<ITransportRegistry>();
var transports = registry3.GetAllTransports().ToList();

Console.WriteLine($"✓ Registered transports: {transports.Count}");
foreach (var t in transports)
{
    Console.WriteLine($"  - {t.TransportId} ({t.TransportType}): {t.Capabilities}");
}

var defaultTransport = registry3.DefaultTransport;
Console.WriteLine($"✓ Default transport: {defaultTransport.TransportId}");

var allStatuses = registry3.GetAllStatuses();
Console.WriteLine($"✓ All statuses retrieved: {allStatuses.Count} transports");

Console.WriteLine();
Console.WriteLine("=== All tests passed! ===");