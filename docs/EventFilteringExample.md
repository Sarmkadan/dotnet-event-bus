# EventFilteringExample

A utility class demonstrating event filtering patterns in a .NET event bus system. It provides a structured event representation with filtering capabilities, typically used to route or process events based on specific criteria such as order, region, amount, or severity.

## API

### `public string OrderId`
Gets or sets the unique identifier for the order associated with the event. Used to correlate events with specific business transactions.

### `public string Region`
Gets or sets the geographic region where the event originated. Used for regional filtering or routing in distributed systems.

### `public decimal Amount`
Gets or sets the monetary amount related to the event. Can be used for filtering high-value transactions or applying thresholds.

### `public string CustomerSegment`
Gets or sets the customer segment classification (e.g., "Premium", "Standard", "Guest"). Enables segment-based event handling or analytics.

### `public DateTime Timestamp`
Gets or sets the UTC timestamp when the event was generated. Essential for time-based filtering, ordering, or windowed processing.

### `public string AlertId`
Gets or sets a unique identifier for the alert or notification. Used to track and deduplicate alert processing.

### `public string Severity`
Gets or sets the severity level of the event (e.g., "Critical", "Warning", "Info"). Used to prioritize or filter events by urgency.

### `public string Source`
Gets or sets the system or component that originated the event (e.g., "PaymentService", "InventoryManager"). Enables source-based routing or filtering.

### `public string Message`
Gets or sets the human-readable description or payload of the event. Contains contextual details about the event occurrence.

### `public static async Task Main`
Entry point for demonstration or integration testing. Executes an example workflow that filters and processes events based on configurable criteria.

## Usage
