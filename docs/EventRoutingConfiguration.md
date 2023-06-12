# EventRoutingConfiguration

`EventRoutingConfiguration` is a builder-style class that defines how events are routed to handlers based on configurable rules. It allows defining routing conditions, priorities, and target handlers for event messages, enabling flexible event-driven architectures.

## API

### `AddRoute`
Adds a new routing rule to the configuration.

- **Parameters**:
  - `rule` (`RoutingRule`): The routing rule to add.
- **Return value**: `void`
- **Throws**: `ArgumentNullException` if `rule` is `null`.

### `GetRoutes`
Retrieves all configured routing rules.

- **Return value**: `IEnumerable<RoutingRule>` – an enumerable of all routing rules in the configuration.
- **Throws**: None.

### `ShouldRoute`
Determines whether an event should be routed based on the configured rules.

- **Parameters**:
  - `eventData` (`Dictionary<string, object>`): The event data to evaluate.
  - `eventType` (`string`): The type of the event.
- **Return value**: `bool` – `true` if the event should be routed; otherwise, `false`.
- **Throws**: `ArgumentNullException` if `eventData` or `eventType` is `null`.

### `GetConfiguredEventTypes`
Retrieves all event types that have been explicitly configured for routing.

- **Return value**: `IEnumerable<string>` – an enumerable of event type strings.
- **Throws**: None.

### `Clear`
Removes all configured routing rules from the configuration.

- **Return value**: `void`
- **Throws**: None.

### `TargetHandler`
Gets or sets the target handler for the current routing rule being configured.

- **Type**: `required string`
- **Remarks**: Must be set before calling `Build` or adding routes.

### `Condition`
Gets or sets an optional condition function that determines whether the current routing rule should be applied.

- **Type**: `Func<Dictionary<string, object>, bool>?`
- **Remarks**: If `null`, the rule is always applied. The function receives the event data and returns `true` if the rule should apply.

### `Priority`
Gets or sets the priority of the current routing rule being configured.

- **Type**: `int`
- **Remarks**: Higher values indicate higher priority. Rules are evaluated in descending order of priority.

### `ContinueEvaluation`
Gets or sets whether evaluation should continue to the next rule after this one if the current rule matches.

- **Type**: `bool`
- **Remarks**: If `true`, subsequent rules are evaluated even after a match. If `false`, evaluation stops after the first match.

### `RouteEvent`
Configures a routing rule to route events of a specific type to a target handler.

- **Parameters**:
  - `eventType` (`string`): The event type to match.
  - `targetHandler` (`string`): The handler to route matching events to.
- **Return value**: `EventRoutingBuilder` – the builder for method chaining.
- **Throws**: `ArgumentNullException` if `eventType` or `targetHandler` is `null`.

### `RouteEventIf`
Configures a conditional routing rule that routes events to a target handler only if a condition is met.

- **Parameters**:
  - `eventType` (`string`): The event type to match.
  - `targetHandler` (`string`): The handler to route matching events to.
  - `condition` (`Func<Dictionary<string, object>, bool>`): The condition to evaluate.
- **Return value**: `EventRoutingBuilder` – the builder for method chaining.
- **Throws**: `ArgumentNullException` if `eventType`, `targetHandler`, or `condition` is `null`.

### `RouteByMetadata`
Configures a routing rule that routes events based on metadata matching.

- **Parameters**:
  - `metadataKey` (`string`): The metadata key to match.
  - `metadataValue` (`string`): The metadata value to match.
  - `targetHandler` (`string`): The handler to route matching events to.
- **Return value**: `EventRoutingBuilder` – the builder for method chaining.
- **Throws**: `ArgumentNullException` if any parameter is `null`.

### `Build`
Finalizes the configuration and returns an immutable `EventRoutingConfiguration` instance.

- **Return value**: `EventRoutingConfiguration` – the configured routing configuration.
- **Throws**: `InvalidOperationException` if `TargetHandler` is not set or if no routes have been added.

## Usage

### Example 1: Basic Event Routing
