# ECommerceOrderProcessingExample

This type provides a set of models and handlers for processing e-commerce orders within an event-driven architecture. It encapsulates order details, line items, payment transactions, and shipment information, while exposing asynchronous handlers for order placement, payment processing, and shipment creation.

## API

### Properties

#### `OrderId` (string)
Identifier for the order. Used across order, payment, and shipment contexts to correlate related operations.

#### `CustomerId` (string)
Unique identifier for the customer who placed the order. Used for customer-specific processing and notifications.

#### `Items` (List<OrderItem>)
Collection of `OrderItem` objects representing the products and quantities ordered. Each item includes `ProductId`, `Quantity`, and `UnitPrice`.

#### `TotalPrice` (decimal)
The computed total price of the order, including taxes and discounts if applicable. Derived from the sum of all line items.

#### `PlacedAt` (DateTime)
Timestamp indicating when the order was placed. Used for tracking order lifecycle and SLA compliance.

#### `ProductId` (string)
Identifier for a product included in an order item. Used to reference product details during order processing.

#### `Quantity` (int)
Number of units of a product included in an order item. Must be a positive integer.

#### `UnitPrice` (decimal)
Price per unit of the product at the time of order placement. Used to calculate line item totals and order total.

#### `TransactionId` (string)
Identifier for a payment transaction associated with an order. Used to track payment status and correlate with financial systems.

#### `IsSuccessful` (bool)
Indicates whether a payment transaction was successful. `true` for approved payments, `false` otherwise.

#### `Amount` (decimal)
The monetary amount of a payment transaction. Must be a positive value.

#### `ShipmentId` (string)
Unique identifier for a shipment associated with an order. Used to track delivery status and logistics.

#### `EstimatedDelivery` (DateTime)
Estimated date and time when the order is expected to be delivered. Used for customer communication and inventory planning.

### Methods

#### `Handle` (async Task)
Overridable asynchronous handler invoked when an order placement event is received. Processes the order, validates data, and emits downstream events (e.g., payment request, inventory update).

#### `Handle` (async Task)
Overridable asynchronous handler invoked when a payment processing event is received. Validates payment details, processes the transaction, and emits a payment result event.

#### `Handle` (async Task)
Overridable asynchronous handler invoked when a shipment creation event is received. Validates shipment data, initiates logistics, and emits a shipment confirmation event.

## Usage

### Example 1: Placing an Order
