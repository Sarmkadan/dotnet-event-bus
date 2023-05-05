#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetEventBus.Exceptions;

/// <summary>
/// Thrown when event bus configuration is invalid or missing.
/// </summary>
public sealed class ConfigurationException : EventBusException
{
    public ConfigurationException(string message) : base(message) { }
    public ConfigurationException(string message, Exception innerException) : base(message, innerException) { }
}
