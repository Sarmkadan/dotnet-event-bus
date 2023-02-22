#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotnetEventBus;
using DotnetEventBus.Handlers;

namespace DotnetEventBus.Examples;

/// <summary>
/// Request-Reply Pattern: Synchronous request-response communication using events.
/// Demonstrates querying user data, product availability, and pricing.
/// </summary>
public static class RequestReplyPatternExample
{
    // Request/Response models
    public sealed class GetUserRequest
    {
        public string UserId { get; set; }
    }

    public sealed class UserResponse
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Status { get; set; }
    }

    public sealed class GetProductAvailabilityRequest
    {
        public string ProductId { get; set; }
    }

    public sealed class ProductAvailabilityResponse
    {
        public string ProductId { get; set; }
        public int AvailableUnits { get; set; }
        public string Warehouse { get; set; }
        public decimal Price { get; set; }
    }

    public sealed class GetPricingRequest
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
        public string CustomerTier { get; set; }
    }

    public sealed class PricingResponse
    {
        public string ProductId { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal Discount { get; set; }
        public string DiscountReason { get; set; }
    }

    // Sample data repositories
    private static readonly Dictionary<string, (string Name, string Email, string Status)> Users = new()
    {
        { "user-001", ("Alice Johnson", "alice@example.com", "Active") },
        { "user-002", ("Bob Smith", "bob@example.com", "Active") },
        { "user-003", ("Charlie Brown", "charlie@example.com", "Inactive") }
    };

    private static readonly Dictionary<string, (int Units, string Warehouse, decimal Price)> Products = new()
    {
        { "prod-001", (50, "Warehouse-A", 29.99m) },
        { "prod-002", (0, "Warehouse-B", 49.99m) },
        { "prod-003", (100, "Warehouse-A", 99.99m) }
    };

    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== DotnetEventBus: Request-Reply Pattern ===\n");

        var services = new ServiceCollection();
        services.AddEventBus(options =>
        {
            options.DefaultHandlerTimeout = TimeSpan.FromSeconds(10);
        });

        var serviceProvider = services.BuildServiceProvider();
        var eventBus = serviceProvider.GetRequiredService<IEventBus>();

        // Setup request handlers
        SetupUserRequestHandler(eventBus);
        SetupProductAvailabilityHandler(eventBus);
        SetupPricingHandler(eventBus);

        // Demonstrate various request-reply scenarios
        await DemonstrateLookupRequests(eventBus);
        await DemonstrateInventoryQueries(eventBus);
        await DemonstratePricingCalculation(eventBus);

        Console.WriteLine("\n=== Example completed successfully ===");
    }

    private static void SetupUserRequestHandler(IEventBus eventBus)
    {
        eventBus.Subscribe<GetUserRequest>(
            async (request, ct) =>
            {
                Console.WriteLine($"   [Handler] Looking up user: {request.UserId}");
                await Task.Delay(50); // Simulate DB query

                if (Users.TryGetValue(request.UserId, out var userData))
                {
                    return new UserResponse
                    {
                        UserId = request.UserId,
                        Name = userData.Name,
                        Email = userData.Email,
                        Status = userData.Status
                    };
                }

                throw new KeyNotFoundException($"User {request.UserId} not found");
            },
            handlerName: "UserLookupHandler"
        );
    }

    private static void SetupProductAvailabilityHandler(IEventBus eventBus)
    {
        eventBus.Subscribe<GetProductAvailabilityRequest>(
            async (request, ct) =>
            {
                Console.WriteLine($"   [Handler] Checking availability: {request.ProductId}");
                await Task.Delay(75); // Simulate inventory system query

                if (Products.TryGetValue(request.ProductId, out var productData))
                {
                    return new ProductAvailabilityResponse
                    {
                        ProductId = request.ProductId,
                        AvailableUnits = productData.Units,
                        Warehouse = productData.Warehouse,
                        Price = productData.Price
                    };
                }

                throw new KeyNotFoundException($"Product {request.ProductId} not found");
            },
            handlerName: "InventoryHandler"
        );
    }

    private static void SetupPricingHandler(IEventBus eventBus)
    {
        eventBus.Subscribe<GetPricingRequest>(
            async (request, ct) =>
            {
                Console.WriteLine($"   [Handler] Calculating pricing: {request.ProductId} x {request.Quantity}");
                await Task.Delay(100); // Simulate pricing engine

                if (!Products.TryGetValue(request.ProductId, out var productData))
                    throw new KeyNotFoundException($"Product {request.ProductId} not found");

                var basePrice = productData.Price * request.Quantity;
                var discount = CalculateDiscount(request.CustomerTier, request.Quantity);
                var discountAmount = basePrice * discount;

                return new PricingResponse
                {
                    ProductId = request.ProductId,
                    UnitPrice = productData.Price,
                    TotalPrice = basePrice - discountAmount,
                    Discount = discount * 100,
                    DiscountReason = GetDiscountReason(request.CustomerTier, request.Quantity)
                };
            },
            handlerName: "PricingEngine"
        );
    }

    private static async Task DemonstrateLookupRequests(IEventBus eventBus)
    {
        Console.WriteLine("\n--- User Lookup Requests ---\n");

        var userIds = new[] { "user-001", "user-003" };

        foreach (var userId in userIds)
        {
            try
            {
                Console.WriteLine($"Requesting user data for: {userId}");
                var response = await eventBus.RequestAsync<GetUserRequest, UserResponse>(
                    new GetUserRequest { UserId = userId },
                    timeout: TimeSpan.FromSeconds(5)
                );

                Console.WriteLine($"✓ Response received:");
                Console.WriteLine($"  - Name: {response.Name}");
                Console.WriteLine($"  - Email: {response.Email}");
                Console.WriteLine($"  - Status: {response.Status}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}\n");
            }
        }
    }

    private static async Task DemonstrateInventoryQueries(IEventBus eventBus)
    {
        Console.WriteLine("--- Product Availability Queries ---\n");

        var productIds = new[] { "prod-001", "prod-002", "prod-003" };

        foreach (var productId in productIds)
        {
            try
            {
                Console.WriteLine($"Checking availability: {productId}");
                var response = await eventBus.RequestAsync<GetProductAvailabilityRequest, ProductAvailabilityResponse>(
                    new GetProductAvailabilityRequest { ProductId = productId },
                    timeout: TimeSpan.FromSeconds(5)
                );

                var status = response.AvailableUnits > 0 ? "✓ In Stock" : "✗ Out of Stock";
                Console.WriteLine($"{status}:");
                Console.WriteLine($"  - Available: {response.AvailableUnits} units");
                Console.WriteLine($"  - Warehouse: {response.Warehouse}");
                Console.WriteLine($"  - Price: ${response.Price:F2}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}\n");
            }
        }
    }

    private static async Task DemonstratePricingCalculation(IEventBus eventBus)
    {
        Console.WriteLine("--- Pricing Calculation ---\n");

        var pricingRequests = new[]
        {
            new GetPricingRequest { ProductId = "prod-001", Quantity = 5, CustomerTier = "Bronze" },
            new GetPricingRequest { ProductId = "prod-003", Quantity = 10, CustomerTier = "Gold" },
            new GetPricingRequest { ProductId = "prod-001", Quantity = 50, CustomerTier = "Platinum" }
        };

        foreach (var request in pricingRequests)
        {
            try
            {
                Console.WriteLine($"Calculating price: {request.ProductId} x {request.Quantity} ({request.CustomerTier})");
                var response = await eventBus.RequestAsync<GetPricingRequest, PricingResponse>(
                    request,
                    timeout: TimeSpan.FromSeconds(5)
                );

                Console.WriteLine($"✓ Pricing calculated:");
                Console.WriteLine($"  - Unit Price: ${response.UnitPrice:F2}");
                Console.WriteLine($"  - Discount: {response.Discount:F1}% ({response.DiscountReason})");
                Console.WriteLine($"  - Total Price: ${response.TotalPrice:F2}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}\n");
            }
        }
    }

    private static decimal CalculateDiscount(string tier, int quantity)
    {
        var tierDiscount = tier switch
        {
            "Bronze" => 0.05m,
            "Silver" => 0.10m,
            "Gold" => 0.15m,
            "Platinum" => 0.20m,
            _ => 0m
        };

        var volumeDiscount = quantity > 10 ? 0.05m : 0m;

        return Math.Min(tierDiscount + volumeDiscount, 0.30m); // Max 30% discount
    }

    private static string GetDiscountReason(string tier, int quantity)
    {
        var reasons = new List<string>();

        if (tier != "Standard")
            reasons.Add($"{tier} customer");

        if (quantity > 10)
            reasons.Add("Volume discount");

        return reasons.Any() ? string.Join(" + ", reasons) : "No discount";
    }
}
