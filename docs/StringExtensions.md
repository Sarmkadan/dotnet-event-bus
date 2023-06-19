# StringExtensions

The `StringExtensions` class provides a collection of utility methods for common string manipulation operations, particularly focused on formatting, validation, and transformation of strings used in event-driven architectures. These extensions are designed to simplify tasks such as case conversion, slug generation, truncation, and event type name validation.

## API

### `ToPascalCase`
Converts a string to PascalCase (upper camel case), where the first letter of each word is capitalized and no separators are used.

**Parameters:**
- `input` (`string`): The string to convert. If `null` or whitespace, returns an empty string.

**Returns:**
- (`string`): The PascalCase representation of the input string.

**Throws:**
- None.

---

### `ToSnakeCase`
Converts a string to snake_case, where words are separated by underscores and all letters are lowercase.

**Parameters:**
- `input` (`string`): The string to convert. If `null` or whitespace, returns an empty string.

**Returns:**
- (`string`): The snake_case representation of the input string.

**Throws:**
- None.

---

### `ToKebabCase`
Converts a string to kebab-case, where words are separated by hyphens and all letters are lowercase.

**Parameters:**
- `input` (`string`): The string to convert. If `null` or whitespace, returns an empty string.

**Returns:**
- (`string`): The kebab-case representation of the input string.

**Throws:**
- None.

---

### `IsValidEventTypeName`
Validates whether a string conforms to expected event type naming conventions. The method checks for null, whitespace, and basic structural requirements (e.g., no leading/trailing separators, valid characters).

**Parameters:**
- `eventTypeName` (`string`): The event type name to validate.

**Returns:**
- (`bool`): `true` if the string is a valid event type name; otherwise, `false`.

**Throws:**
- None.

---

### `Truncate`
Truncates a string to a specified maximum length, appending an ellipsis (`...`) if the string exceeds the limit. If the string is shorter than the maximum length, it is returned unchanged.

**Parameters:**
- `input` (`string`): The string to truncate. If `null`, returns `null`.
- `maxLength` (`int`): The maximum allowed length of the string, including the ellipsis if truncation occurs. Must be greater than 3; otherwise, throws `ArgumentOutOfRangeException`.

**Returns:**
- (`string`): The truncated string, or the original string if no truncation is needed.

**Throws:**
- `ArgumentOutOfRangeException`: If `maxLength` is less than or equal to 3.

---

### `IsNullOrWhitespace`
Determines whether a string is `null`, empty, or consists only of whitespace characters. This is a convenience method equivalent to `string.IsNullOrWhiteSpace`.

**Parameters:**
- `input` (`string`): The string to check.

**Returns:**
- (`bool`): `true` if the string is `null`, empty, or whitespace; otherwise, `false`.

**Throws:**
- None.

---

### `ToSlug`
Converts a string into a URL-friendly slug by converting it to lowercase, replacing spaces and special characters with hyphens, and removing invalid characters. Consecutive hyphens are collapsed into a single hyphen.

**Parameters:**
- `input` (`string`): The string to convert. If `null` or whitespace, returns an empty string.

**Returns:**
- (`string`): The slugified version of the input string.

**Throws:**
- None.

---

### `GetEventCategory`
Extracts the category portion of an event type name, assuming the event type follows a `{Category}.{EventName}` format. If no separator is found, the entire string is treated as the category.

**Parameters:**
- `eventTypeName` (`string`): The event type name to parse. If `null` or whitespace, returns an empty string.

**Returns:**
- (`string`): The category portion of the event type name.

**Throws:**
- None.

---

### `Repeat`
Repeats a string a specified number of times and concatenates the results.

**Parameters:**
- `input` (`string`): The string to repeat. If `null`, returns `null`.
- `count` (`int`): The number of times to repeat the string. Must be non-negative; otherwise, throws `ArgumentOutOfRangeException`.

**Returns:**
- (`string`): The concatenated result of repeating the input string.

**Throws:**
- `ArgumentOutOfRangeException`: If `count` is negative.

## Usage

### Example 1: Event Type Name Formatting
