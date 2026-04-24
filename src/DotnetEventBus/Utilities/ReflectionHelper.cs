#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DotnetEventBus.Utilities;

/// <summary>
/// Utility class for reflection operations on event handlers and event types.
/// Provides safe runtime type inspection and instantiation.
/// Why: Event bus needs to dynamically discover and invoke handlers at runtime.
/// </summary>
public static class ReflectionHelper
{
    /// <summary>
    /// Finds all types in a specified assembly that implement a given interface.
    /// </summary>
    public static IEnumerable<Type> FindImplementationsOf<TInterface>(Assembly assembly) where TInterface : class
    {
        return assembly.GetTypes()
            .Where(t => typeof(TInterface).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
    }

    /// <summary>
    /// Finds all types in the current domain that implement a given interface.
    /// Searches all loaded assemblies.
    /// </summary>
    public static IEnumerable<Type> FindImplementationsOfInAllAssemblies<TInterface>() where TInterface : class
    {
        var interfaceType = typeof(TInterface);
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => FindImplementationsOf<TInterface>(a));
    }

    /// <summary>
    /// Gets all methods of a type that match a specific signature.
    /// Useful for finding event handlers with specific method names.
    /// </summary>
    public static IEnumerable<MethodInfo> GetMethodsBySignature(
        Type type,
        string methodName,
        Type? returnType = null,
        Type[]? parameterTypes = null)
    {
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
        var methods = type.GetMethods(bindingFlags).Where(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));

        if (returnType is not null)
            methods = methods.Where(m => m.ReturnType == returnType);

        if (parameterTypes is not null)
            methods = methods.Where(m =>
            {
                var parameters = m.GetParameters();
                return parameters.Length == parameterTypes.Length &&
                       parameters.Zip(parameterTypes, (p, t) => p.ParameterType == t).All(x => x);
            });

        return methods;
    }

    /// <summary>
    /// Attempts to create an instance of a type using the default constructor.
    /// </summary>
    public static T? TryCreateInstance<T>(Type type) where T : class
    {
        try
        {
            var instance = Activator.CreateInstance(type);
            return instance as T;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets all attributes of a specified type from a member.
    /// </summary>
    public static IEnumerable<TAttribute> GetCustomAttributes<TAttribute>(ICustomAttributeProvider member)
        where TAttribute : Attribute
    {
        return member.GetCustomAttributes(typeof(TAttribute), false).Cast<TAttribute>();
    }

    /// <summary>
    /// Determines if a type has a specific attribute.
    /// </summary>
    public static bool HasAttribute<TAttribute>(Type type) where TAttribute : Attribute
    {
        return type.GetCustomAttributes(typeof(TAttribute), false).Length > 0;
    }

    /// <summary>
    /// Gets a property value from an object using reflection.
    /// Handles both public and private properties.
    /// </summary>
    public static object? GetPropertyValue(object obj, string propertyName)
    {
        var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.IgnoreCase);
        return property?.GetValue(obj);
    }

    /// <summary>
    /// Sets a property value on an object using reflection.
    /// Handles both public and private properties.
    /// </summary>
    public static void SetPropertyValue(object obj, string propertyName, object? value)
    {
        var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.IgnoreCase);
        property?.SetValue(obj, value);
    }

    /// <summary>
    /// Gets all public properties and their values from an object.
    /// Returns a dictionary of property names to values.
    /// </summary>
    public static Dictionary<string, object?> GetAllPropertyValues(object obj)
    {
        return obj.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(p => p.Name, p => p.GetValue(obj));
    }

    /// <summary>
    /// Invokes a method on an object using reflection.
    /// Returns the method's return value if applicable.
    /// </summary>
    public static object? InvokeMethod(object obj, string methodName, params object[] parameters)
    {
        var parameterTypes = parameters.Select(p => p?.GetType() ?? typeof(object)).ToArray();
        var method = obj.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, null, parameterTypes, null);

        if (method is null)
            throw new MethodAccessException($"Method {methodName} not found on type {obj.GetType().Name}");

        return method.Invoke(obj, parameters);
    }

    /// <summary>
    /// Gets all generic type arguments if a type is generic.
    /// </summary>
    public static Type[] GetGenericArguments(Type type)
    {
        return type.IsGenericType ? type.GetGenericArguments() : Type.EmptyTypes;
    }
}
