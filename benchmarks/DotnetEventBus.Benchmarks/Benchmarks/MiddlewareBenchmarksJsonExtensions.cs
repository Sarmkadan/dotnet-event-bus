using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetEventBus.Benchmarks;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="MiddlewareBenchmarks"/>.
/// </summary>
public static class MiddlewareBenchmarksJsonExtensions
{
	private static readonly JsonSerializerOptions _jsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	/// <summary>
	/// Serializes MiddlewareBenchmarks to a JSON string.
	/// </summary>
	/// <param name="value">The MiddlewareBenchmarks instance to serialize.</param>
	/// <param name="indented">Whether to format the JSON with indentation.</param>
	/// <returns>JSON string representation.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
	public static string ToJson(this MiddlewareBenchmarks value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);

		var options = indented
			? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
			: _jsonOptions;

		return JsonSerializer.Serialize(value, options);
	}

	/// <summary>
	/// Deserializes MiddlewareBenchmarks from a JSON string.
	/// </summary>
	/// <param name="json">JSON string to deserialize.</param>
	/// <returns>Deserialized MiddlewareBenchmarks instance, or <see langword="null"/> if parsing fails.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
	/// <exception cref="FormatException">Thrown when the JSON is malformed or cannot be deserialized.</exception>
	public static MiddlewareBenchmarks? FromJson(string json)
	{
		ArgumentNullException.ThrowIfNull(json);

		if (string.IsNullOrWhiteSpace(json))
		{
			return null;
		}

		try
		{
			return JsonSerializer.Deserialize<MiddlewareBenchmarks>(json, _jsonOptions);
		}
		catch (JsonException ex)
		{
			throw new FormatException("Failed to deserialize MiddlewareBenchmarks from JSON", ex);
		}
	}

	/// <summary>
	/// Attempts to deserialize MiddlewareBenchmarks from a JSON string.
	/// </summary>
	/// <param name="json">JSON string to deserialize.</param>
	/// <param name="value">Output parameter for the deserialized instance.</param>
	/// <returns><see langword="true"/> if deserialization succeeded; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
	public static bool TryFromJson(string json, out MiddlewareBenchmarks? value)
	{
		ArgumentNullException.ThrowIfNull(json);

		value = null;

		if (string.IsNullOrWhiteSpace(json))
		{
			return false;
		}

		try
		{
			value = JsonSerializer.Deserialize<MiddlewareBenchmarks>(json, _jsonOptions);
			return true;
		}
		catch (JsonException)
		{
			return false;
		}
	}
}