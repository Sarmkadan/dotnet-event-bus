# Formatters

The `DotnetEventBus.Formatters` namespace turns event payloads into wire or export formats. Every formatter implements `IEventFormatter`. `EventFormatterFactory` keeps a registry of formatters keyed by format name, and `EventFormatterFactoryValidation` checks that a registry is consistent.

| Type | Format name | Content type | Deserialization | Typical use |
|------|-------------|--------------|-----------------|-------------|
| `JsonEventFormatter` | `json` | `application/json` | Yes | Default payload format, REST APIs |
| `XmlEventFormatter` | `xml` | `application/xml` | Yes (errors return `null`) | Legacy/enterprise integration |
| `CsvEventFormatter` | `csv` | `text/csv` | No (throws `NotSupportedException`) | Bulk export, reporting |
| `EventFormatterFactory` | — | — | — | Registry: lookup by format name or content type |
| `EventFormatterFactoryValidation` | — | — | — | Validation extensions for the factory |

The `EventBus` uses `JsonEventFormatter` when no formatter is supplied. DI registration (`AddEventBus`) also registers `JsonEventFormatter` as the `IEventFormatter` singleton.

## IEventFormatter

```csharp
public interface IEventFormatter
{
    string Format { get; }        // e.g. "json"
    string ContentType { get; }   // e.g. "application/json"

    string Serialize(object data, bool prettyPrint = false);
    T? Deserialize<T>(string data) where T : class;
    object? Deserialize(string data, Type targetType);
    string FormatEvent(object eventData, bool includePrettyPrint = false);
    string FormatEventWithMetadata(object eventData, Dictionary<string, object> metadata, bool includePrettyPrint = false);
}
```

- **`Format`**: lowercase name used as the registry key.
- **`ContentType`**: MIME type. Used by `GetFormatterByContentType`.
- **`Serialize`**: converts an object to the format. Throws `ArgumentNullException` for `null`.
- **`Deserialize<T>` / `Deserialize(string, Type)`**: converts text back to an object. Implementations decide how to handle empty input and malformed input (see each formatter below).
- **`FormatEvent`**: formats an event for output. Usually equivalent to `Serialize`.
- **`FormatEventWithMetadata`**: formats an event together with a metadata dictionary. Each formatter defines its own envelope (see below).

## JsonEventFormatter

Serializes with `System.Text.Json`. The class holds two pre-built option sets:

- Compact (`prettyPrint: false`, the default).
- Indented (`prettyPrint: true`).

Both sets use `PropertyNameCaseInsensitive = true`, `ReferenceHandler.IgnoreCycles`, and `DefaultIgnoreCondition = WhenWritingNull`. Null properties are omitted, and object graphs with cycles do not throw.

| Member | Behavior |
|--------|----------|
| `Serialize(data, prettyPrint)` | Throws `ArgumentNullException` if `data` is `null`. |
| `Deserialize<T>(json)` | Returns `null` for null or empty input. Malformed JSON throws `JsonException`. |
| `Deserialize(json, targetType)` | Same as above, for a runtime `Type`. |
| `FormatEvent(eventData, pretty)` | Returns the string `"null"` when `eventData` is `null`. Otherwise same as `Serialize`. |
| `FormatEventWithMetadata(eventData, metadata, pretty)` | Writes an envelope: `{"event": ..., "metadata": {...}, "timestamp": "<UTC, ISO 8601 'o'>"}`. |

The examples below use a hypothetical `OrderPlaced(int OrderId, decimal Total)` event type.

```csharp
var json = new JsonEventFormatter();

string compact = json.Serialize(new OrderPlaced(42, 99.5m));
string pretty = json.Serialize(new OrderPlaced(42, 99.5m), prettyPrint: true);

string withMeta = json.FormatEventWithMetadata(
    new OrderPlaced(42, 99.5m),
    new Dictionary<string, object> { ["correlationId"] = "abc-123" });
// {"event":{"OrderId":42,"Total":99.5},"metadata":{"correlationId":"abc-123"},"timestamp":"2026-10-25T..."}
```

## XmlEventFormatter

Serializes with `System.Xml.Serialization.XmlSerializer`. The payload type must be serializable by `XmlSerializer` (public type, public parameterless constructor for deserialization, public read/write members).

**Constructor**: `XmlEventFormatter(bool omitXmlDeclaration = false, Encoding? encoding = null)`.
- The default encoding is UTF-8 without a byte order mark.
- Namespaces are suppressed: the root element has no `xmlns` attributes.

| Member | Behavior |
|--------|----------|
| `Serialize(data, prettyPrint)` | Throws `ArgumentNullException` if `data` is `null`. `prettyPrint` sets `XmlWriterSettings.Indent`. |
| `Deserialize<T>(xml)` / `Deserialize(xml, targetType)` | Returns `null` for null or empty input. **Any exception during deserialization is caught and `null` is returned.** Check for `null` if you need to detect bad input. |
| `FormatEvent(eventData, pretty)` | Same as `Serialize`. |
| `FormatEventWithMetadata(eventData, metadata, pretty)` | Writes `<Event timestamp="..."><Data>...</Data><Metadata><key>value</key>...</Metadata></Event>`. The event XML is embedded in `<Data>` without its XML declaration. |

Metadata keys become element names. Characters outside `[a-zA-Z0-9_-]` are replaced with `_`, and a leading digit gets a `_` prefix. Values are written as text using `ToString()`, with `null` written as an empty string. Keys must be non-empty; an empty key currently throws `IndexOutOfRangeException`.

```csharp
var xml = new XmlEventFormatter(omitXmlDeclaration: true);
string doc = xml.Serialize(new OrderPlaced(42, 99.5m), prettyPrint: true);

OrderPlaced? parsed = xml.Deserialize<OrderPlaced>(doc); // null if doc is not valid
```

## CsvEventFormatter

Writes a header line and one value line, using the public readable instance properties of the event type in reflection order.

**Constructor**: `CsvEventFormatter(string delimiter = ",", bool includeHeaders = true)`.

| Member | Behavior |
|--------|----------|
| `Serialize(data, _)` | Throws `ArgumentNullException` if `data` is `null`. The `prettyPrint` flag has no effect. Output is `header` + newline + `values` + newline (or only the values when `includeHeaders` is `false`). |
| `Deserialize<T>` / `Deserialize(string, Type)` | Always throws `NotSupportedException`. |
| `FormatEvent(eventData, _)` | Same as `Serialize`. |
| `FormatEventWithMetadata(eventData, metadata, _)` | Appends the metadata keys to the header and the metadata values to the value line. |

Field rules:

- A property value is written with `ToString()`. `null` values and property getters that throw become an empty field.
- A field is wrapped in double quotes when it contains the delimiter, a `"`, or a `\n`. Embedded quotes are doubled.
- Only `\n` triggers quoting. A field containing a bare `\r` is written unquoted.

```csharp
var csv = new CsvEventFormatter(delimiter: ";");
string text = csv.Serialize(new OrderPlaced(42, 99.5m));
// OrderId;Total
// 42;99.5
```

## EventFormatterFactory

A registry of `IEventFormatter` instances keyed by `Format` (case-insensitive).

| Member | Behavior |
|--------|----------|
| `static CreateDefault()` | New factory with `JsonEventFormatter`, `CsvEventFormatter`, and `XmlEventFormatter` registered, using their default constructors. |
| `Register(formatter)` | Throws `ArgumentNullException` for `null`. Replaces any formatter with the same `Format`. |
| `GetFormatter(format)` | Returns the formatter for a format name, or `null` for null, empty, or unknown names. |
| `GetFormatterByContentType(contentType)` | Tries an exact (case-insensitive) match on `ContentType` first. Then it takes the subtype after `/`, strips parameters such as `; charset=utf-8`, and looks that up as a format name. Returns `null` if nothing matches. |
| `GetAllFormatters()` | Snapshot list of registered formatters. |
| `GetSupportedFormats()` | Snapshot list of registry keys (format names, lowercase). |
| `IsFormatSupported(format)` | `true` if a formatter is registered under that format name. |
| `Unregister(format)` | Removes the formatter. Returns `true` if one was removed. |

Content-type lookup examples:

- `"application/json; charset=utf-8"` → `json` (subtype fallback).
- `"text/csv"` → `csv` (exact match on `ContentType`).
- `"text/plain"` → `null` (no formatter registered).

The factory uses a plain `Dictionary`. Register formatters during startup, before the factory is shared across threads. Concurrent reads are safe once registration is finished.

```csharp
var factory = EventFormatterFactory.CreateDefault();
factory.Register(new MyBinaryFormatter());

IEventFormatter? formatter = factory.GetFormatterByContentType("application/json; charset=utf-8");
string payload = formatter?.Serialize(new OrderPlaced(42, 99.5m)) ?? string.Empty;
```

## EventFormatterFactoryValidation

Extension methods on `EventFormatterFactory?` that check the registry.

| Method | Behavior |
|--------|----------|
| `IReadOnlyList<string> Validate()` | Returns a list of problems, empty if valid. Throws `ArgumentNullException` for a `null` factory. |
| `bool IsValid()` | `true` if the factory is non-null and `Validate()` returns no problems. Never throws. |
| `void EnsureValid()` | Throws `ArgumentNullException` for `null`, or `ArgumentException` listing every problem. |

`Validate` reports:

- A `null` entry in the registry.
- A formatter with a null, empty, or whitespace `Format` or `ContentType`.
- A factory with no formatters.
- A format name that is empty or contains whitespace or control characters.
- Duplicate format names and duplicate content types, compared case-insensitively.

```csharp
factory.Validate();   // e.g. [] for CreateDefault()
factory.EnsureValid(); // throws ArgumentException if a custom formatter is misconfigured
```

## Adding a new formatter

1. **Create the class** in `src/DotnetEventBus/Formatters/`, named `XxxEventFormatter.cs`. Make it `sealed`, with the file header and `#nullable enable` used by the other formatters.
2. **Implement `IEventFormatter`**:
   - `Format` returns a lowercase name with no whitespace (for example `"yaml"`).
   - `ContentType` returns the MIME type (for example `"application/yaml"`).
   - `Serialize` and `FormatEvent` throw `ArgumentNullException` for `null` input (use `ArgumentNullException.ThrowIfNull`).
   - Decide how `Deserialize` handles empty and malformed input, and document it. Throw `NotSupportedException` if the format is write-only, as `CsvEventFormatter` does.
   - Define the `FormatEventWithMetadata` envelope and document it.
3. **Do not add dependencies.** Use the framework libraries already referenced by the project. Do not edit `.csproj` or `.sln` for a formatter.
4. **Register it** where it is needed:

   ```csharp
   var factory = EventFormatterFactory.CreateDefault();
   factory.Register(new YamlEventFormatter());
   ```

   Do not add the formatter to `CreateDefault()` unless every consumer should receive it. That changes the default factory for all callers.
5. **Add tests** in `tests/DotnetEventBus.Tests/XxxEventFormatterTests.cs`, following `CsvEventFormatterTests.cs` and `XmlEventFormatterTests.cs`. Cover round-trip, `null` input, empty input, metadata output, and the content-type lookup.
6. **Add a reference page** at `docs/XxxEventFormatter.md`, and link it from this page.

Skeleton:

```csharp
#nullable enable

using System;
using System.Collections.Generic;

namespace DotnetEventBus.Formatters;

/// <summary>
/// Formats events as <description of the format>.
/// </summary>
public sealed class YamlEventFormatter : IEventFormatter
{
    public string Format => "yaml";
    public string ContentType => "application/yaml";

    public string Serialize(object data, bool prettyPrint = false)
    {
        ArgumentNullException.ThrowIfNull(data);
        throw new NotImplementedException(); // replace with real serialization
    }

    public T? Deserialize<T>(string data) where T : class
    {
        throw new NotSupportedException("YAML deserialization is not supported");
    }

    public object? Deserialize(string data, Type targetType)
    {
        throw new NotSupportedException("YAML deserialization is not supported");
    }

    public string FormatEvent(object eventData, bool includePrettyPrint = false)
    {
        return Serialize(eventData, includePrettyPrint);
    }

    public string FormatEventWithMetadata(object eventData, Dictionary<string, object> metadata, bool includePrettyPrint = false)
    {
        throw new NotImplementedException(); // replace with real envelope
    }
}
```

## Related

- [EventFormatterFactory](EventFormatterFactory.md)
- [JsonEventFormatter](JsonEventFormatter.md)
- [XmlEventFormatter](XmlEventFormatter.md)
- [CsvEventFormatter](CsvEventFormatter.md)
