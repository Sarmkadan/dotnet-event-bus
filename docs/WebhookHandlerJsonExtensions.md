# WebhookHandlerJsonExtensions

Provides extension methods for serializing and deserializing `WebhookHandler` and `WebhookSubscription` objects to and from JSON strings.

## API

### `ToJson(WebhookHandler handler)`

Serializes a `WebhookHandler` instance into a JSON string.

- **Parameters**
  - `handler` – The `WebhookHandler` instance to serialize.
- **Returns**
  - A JSON string representation of the handler.
- **Throws**
  - `ArgumentNullException` if `handler` is `null`.

### `FromJson(string json)`

Deserializes a JSON string into a `WebhookHandler` instance.

- **Parameters**
  - `json` – The JSON string to deserialize.
- **Returns**
  - The deserialized `WebhookHandler` instance, or `null` if deserialization fails.
- **Throws**
  - `ArgumentNullException` if `json` is `null`.

### `TryFromJson(string json, out WebhookHandler? handler)`

Attempts to deserialize a JSON string into a `WebhookHandler` instance without throwing exceptions.

- **Parameters**
  - `json` – The JSON string to deserialize.
  - `handler` – Output parameter receiving the deserialized `WebhookHandler` instance, or `null` if deserialization fails.
- **Returns**
  - `true` if deserialization succeeds; otherwise, `false`.
- **Throws**
  - `ArgumentNullException` if `json` is `null`.

### `ToJson(WebhookSubscription subscription)`

Serializes a `WebhookSubscription` instance into a JSON string.

- **Parameters**
  - `subscription` – The `WebhookSubscription` instance to serialize.
- **Returns**
  - A JSON string representation of the subscription.
- **Throws**
  - `ArgumentNullException` if `subscription` is `null`.

### `FromJsonToSubscription(string json)`

Deserializes a JSON string into a `WebhookSubscription` instance.

- **Parameters**
  - `json` – The JSON string to deserialize.
- **Returns**
  - The deserialized `WebhookSubscription` instance, or `null` if deserialization fails.
- **Throws**
  - `ArgumentNullException` if `json` is `null`.

### `TryFromJsonToSubscription(string json, out WebhookSubscription? subscription)`

Attempts to deserialize a JSON string into a `WebhookSubscription` instance without throwing exceptions.

- **Parameters**
  - `json` – The JSON string to deserialize.
  - `subscription` – Output parameter receiving the deserialized `WebhookSubscription` instance, or `null` if deserialization fails.
- **Returns**
  - `true` if deserialization succeeds; otherwise, `false`.
- **Throws**
  - `ArgumentNullException` if `json` is `null`.

## Usage
