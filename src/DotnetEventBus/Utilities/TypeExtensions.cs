#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

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
    public static string GetFriendlyName(this Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        if (!type.IsGenericType)
            return type.Name;

        var genericArgs = type.GetGenericArguments();
        var genericArgNames = string.Join(", ", genericArgs.Select(t => t.GetFriendlyName()));
        return $"{type.Name.Split('`')[0]}<{genericArgNames}>";
    }

    /// <summary>
    /// Determines if a type is assignable from another type (including null handling).
    /// </summary>
    public static bool IsAssignableFromNullable(this Type type, Type? other)
    {
        return other is not null && type.IsAssignableFrom(other);
    }

    /// <summary>
    /// Checks if a type implements a specific interface.
    /// </summary>
    public static bool Implements<TInterface>(this Type type) where TInterface : class
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        return typeof(TInterface).IsAssignableFrom(type);
    }

    /// <summary>
    /// Checks if a type is nullable (includes Nullable<T> and reference types).
    /// </summary>
    public static bool IsNullableType(this Type type)
    {
        if (type is null)
            return false;

        return Nullable.GetUnderlyingType(type) is not null || !type.IsValueType;
    }

    /// <summary>
    /// Gets all interfaces implemented by a type, including generic versions.
    /// </summary>
    public static IEnumerable<Type> GetAllInterfaces(this Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        return type.GetInterfaces().Concat(new[] { type }).Where(t => t.IsInterface);
    }

    /// <summary>
    /// Determines if a type can be instantiated (not abstract, not interface).
    /// </summary>
    public static bool IsInstantiable(this Type type)
    {
        if (type is null)
            return false;

        return !type.IsAbstract && !type.IsInterface && type.IsClass;
    }

    /// <summary>
    /// Gets the full type name including generic arguments.
    /// Example: System.Collections.Generic.List`1[[System.String]]
    /// </summary>
    public static string GetFullTypeNameWithGenerics(this Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

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
    public static bool InheritsFrom(this Type type, Type baseType)
    {
        if (type is null || baseType is null)
            return false;

        return baseType.IsGenericTypeDefinition
            ? type.BaseType?.IsGenericType == true && type.BaseType.GetGenericTypeDefinition() == baseType
            : baseType.IsAssignableFrom(type);
    }

    /// <summary>
    /// Gets all public properties of a type including inherited ones.
    /// </summary>
    public static IEnumerable<System.Reflection.PropertyInfo> GetAllPublicProperties(this Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        return type.GetProperties(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.IgnoreCase);
    }
}
