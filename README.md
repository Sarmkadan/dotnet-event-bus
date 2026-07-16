## EventFormatterFactory

The `EventFormatterFactory` class provides a registry for event formatters within the event bus. It allows registering formatters for specific data formats (JSON, XML, CSV), negotiating the appropriate formatter based on content type or format name, and managing the lifecycle of formatters.

Example usage:

```csharp
using DotnetEventBus.Formatters;

// Create a default factory with pre-configured formatters
var factory = EventFormatterFactory.CreateDefault();

// Register a custom formatter
factory.Register(new CustomEventFormatter());

// Get a formatter by format name
var jsonFormatter = factory.GetFormatter("json");

// Get a formatter by MIME type
var jsonMimeTypeFormatter = factory.GetFormatterByContentType("application/json");

// Get all registered formatters
var allFormatters = factory.GetAllFormatters();

// Check if a format is supported
bool isJsonSupported = factory.IsFormatSupported("json");

// Unregister a formatter
bool unregistered = factory.Unregister("csv");
```
