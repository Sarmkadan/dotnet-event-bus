#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetEventBus.Utilities;

/// <summary>
/// Extension methods for Type reflection and analysis.
/// Provides utilities for runtime type inspection and inheritance checks.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Gets the friendly name of a type for display purposes.
    /// Removes namespace and generic type arguments for readability.
    /// </summary>
    /// <param name="type">The type to get the friendly name for.</param>
    /// <returns>The friendly name without namespace and with simplified generic arguments.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    public static string GetFriendlyName(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!type.IsGenericType)
            return type.Name;

        var genericArgs = type.GetGenericArguments();
        var genericArgNames = string.Join(", ", genericArgs.Select(t => t.GetFriendlyName()));
        return $"{type.Name.Split('`')[0]}<{genericArgNames}>";
    }

    /// <summary>
    /// Determines if a type is assignable from another type (including null handling).
    /// </summary>
    /// <param name="other">The type to check assignability against.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    /// <returns>True if the type is assignable from other; otherwise false.</returns>
    public static bool IsAssignableFromNullable(this Type type, Type? other)
    {
        ArgumentNullException.ThrowIfNull(type);
        return other is not null && type.IsAssignableFrom(other);
    }

    /// <summary>
    /// Checks if a type implements a specific interface.
    /// </summary>
    /// <typeparam name="TInterface">The interface type to check for.</typeparam>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type implements the interface; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    public static bool Implements<TInterface>(this Type type) where TInterface : class
    {
        ArgumentNullException.ThrowIfNull(type);

        return typeof(TInterface).IsAssignableFrom(type);
    }

    /// <summary>
    /// Checks if a type is nullable (includes Nullable&lt;T&gt; and reference types).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is nullable; otherwise false.</returns>
    public static bool IsNullableType(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return Nullable.GetUnderlyingType(type) is not null || !type.IsValueType;
    }

    /// <summary>
    /// Gets all interfaces implemented by a type, including generic versions.
    /// </summary>
    /// <param name="type">The type to get interfaces for.</param>
    /// <returns>Collection of all interfaces implemented by the type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    public static IEnumerable<Type> GetAllInterfaces(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.GetInterfaces();
    }

    /// <summary>
    /// Determines if a type can be instantiated (not abstract, not interface).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type can be instantiated; otherwise false.</returns>
    public static bool IsInstantiable(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return !type.IsAbstract && !type.IsInterface && type.IsClass;
    }

    /// <summary>
    /// Gets the full type name including generic arguments.
    /// Example: System.Collections.Generic.List`1[[System.String]]
    /// </summary>
    /// <param name="type">The type to get the full name for.</param>
    /// <returns>The full type name with generic arguments.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    public static string GetFullTypeNameWithGenerics(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!type.IsGenericType)
            return type.FullName ?? type.Name;

        var genericArgs = type.GetGenericArguments();
        var genericArgNames = string.Join(",", genericArgs.Select(t => $"[{t.GetFullTypeNameWithGenerics()}]"));
        var baseName = type.FullName?.Split('`')[0] ?? type.Name;
        return $"{baseName}`{genericArgs.Length}[{genericArgNames}]";
    }

    /// <summary>
    /// Checks if a type inherits from a base type (supports generics).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="baseType">The base type to check against.</param>
    /// <returns>True if the type inherits from baseType; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either <paramref name="type"/> or <paramref name="baseType"/> is null.</exception>
    public static bool InheritsFrom(this Type type, Type baseType)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(baseType);

        return baseType.IsGenericTypeDefinition
            ? type.BaseType?.IsGenericType == true && type.BaseType.GetGenericTypeDefinition() == baseType
            : baseType.IsAssignableFrom(type);
    }

    /// <summary>
    /// Gets all public properties of a type including inherited ones.
    /// </summary>
    /// <param name="type">The type to get properties for.</param>
    /// <returns>Collection of all public properties.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    public static IEnumerable<System.Reflection.PropertyInfo> GetAllPublicProperties(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.GetProperties(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.IgnoreCase);
    }
}