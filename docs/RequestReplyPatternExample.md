# RequestReplyPatternExample

Example implementation demonstrating the Request-Reply pattern using the dotnet-event-bus library. This class shows how to send commands and receive responses in a decoupled, asynchronous manner using message brokers and event buses.

## API

### `public string UserId`
Identifies the user initiating the request. Used as a correlation identifier in message headers to correlate requests with responses. Must not be null or empty.

### `public string Name`
User's full name. Optional field used in order placement requests. If provided, it will be included in the order confirmation payload.

### `public string Email`
User's contact email. Required for order placement and customer tier updates. Must be a valid email format or the request will be rejected by the handler.

### `public string Status`
Current status of the order or inventory check. Used in inventory update commands to indicate whether units are being added or removed. Accepts values "Available" or "Reserved".

### `public string ProductId`
Unique identifier of the product being ordered, checked, or updated. Must match the format used by the inventory service (typically a GUID). Required in all request types.

### `public int AvailableUnits`
Number of units available in the specified warehouse. Used in inventory status responses. Will be zero if the product is not tracked in the queried warehouse.

### `public string Warehouse`
Identifier of the warehouse to check inventory for. Must correspond to a known warehouse code in the system. If omitted, the default warehouse will be used.

### `public decimal Price`
Unit price of the product. Used in order placement requests to calculate the total order value. Must be a positive decimal value.

### `public int Quantity`
Number of units being ordered. Used in order placement requests. Must be a positive integer. If the requested quantity exceeds available inventory, the order will be rejected.

### `public string CustomerTier`
Customer's membership tier (e.g., "Standard", "Premium"). Used to calculate applicable discounts during order processing. If not provided, defaults to "Standard".

### `public decimal UnitPrice`
Price per unit at the time of order. Returned in order confirmation responses. Reflects the price used for billing, which may differ from the current catalog price due to promotions.

### `public decimal TotalPrice`
Total amount charged for the order. Calculated as `UnitPrice * Quantity - Discount`. Returned in order confirmation responses.

### `public decimal Discount`
Monetary discount applied to the order. Returned in order confirmation responses. Will be zero if no discount was applicable.

### `public string DiscountReason`
Explanation for the applied discount. Returned in order confirmation responses. May be null if no discount was applied.

### `public static async Task Main(string[] args)`
Entry point for console demonstration. Initializes the event bus, subscribes to response topics, sends sample requests (inventory check, order placement, customer tier update), and prints responses. Does not throw exceptions—errors are logged to console.

## Usage

### Example 1: Inventory Check with Request-Reply
