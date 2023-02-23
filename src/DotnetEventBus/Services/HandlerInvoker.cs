// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetEventBus.Handlers;
using DotnetEventBus.Models;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DotnetEventBus.Services;

/// <summary>
/// Service responsible for invoking event handlers with reflection and type safety.
/// </summary>
public interface IHandlerInvoker
{
    /// <summary>
    /// Invokes a handler for a given event.
    /// </summary>
    Task InvokeAsync(IEventHandler handler, object @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes a handler and returns a response for request/reply pattern.
    /// </summary>
    Task<object?> InvokeRequestAsync(object handler, object request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a handler can handle a specific event type.
    /// </summary>
    bool CanHandle(IEventHandler handler, Type eventType);

    /// <summary>
    /// Gets the event types a handler supports.
    /// </summary>
    IEnumerable<Type> GetSupportedEventTypes(IEventHandler handler);
}

/// <summary>
/// Default implementation of handler invocation using reflection.
/// </summary>
public class HandlerInvoker : IHandlerInvoker
{
    private readonly ILogger<HandlerInvoker>? _logger;
    private readonly Dictionary<(Type, Type), MethodInfo> _methodCache = new();
    private readonly object _cacheLock = new();

    public HandlerInvoker(ILogger<HandlerInvoker>? logger = null)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(IEventHandler handler, object @event, CancellationToken cancellationToken = default)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        var eventType = @event.GetType();

        try
        {
            var method = GetHandleMethod(handler.GetType(), eventType);
            if (method == null)
                throw new InvalidOperationException(
                    $"Handler {handler.GetType().Name} does not have a compatible Handle method for {eventType.Name}");

            var task = method.Invoke(handler, new[] { @event, cancellationToken }) as Task
                ?? throw new InvalidOperationException("Handler method did not return a Task");

            await task;
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error invoking handler {HandlerType} for event {EventType}",
                handler.GetType().Name, eventType.Name);
            throw;
        }
    }

    public async Task<object?> InvokeRequestAsync(object handler, object request, CancellationToken cancellationToken = default)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var requestType = request.GetType();
        var handlerType = handler.GetType();

        try
        {
            // Look for Handle method that returns Task<TResponse>
            var method = handlerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                    m.Name == "Handle" &&
                    m.GetParameters().Length == 2 &&
                    m.GetParameters()[0].ParameterType == requestType &&
                    m.ReturnType.IsGenericType &&
                    m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));

            if (method == null)
                throw new InvalidOperationException(
                    $"Handler {handlerType.Name} does not have a compatible Handle method for request {requestType.Name}");

            var task = method.Invoke(handler, new[] { request, cancellationToken }) as Task
                ?? throw new InvalidOperationException("Handler method did not return a Task");

            await task;

            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty?.GetValue(task);
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error invoking request handler {HandlerType} for request {RequestType}",
                handlerType.Name, requestType.Name);
            throw;
        }
    }

    public bool CanHandle(IEventHandler handler, Type eventType)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));
        if (eventType == null)
            throw new ArgumentNullException(nameof(eventType));

        if (handler is IPolymorphicHandler polymorphic)
            return polymorphic.CanHandle(eventType);

        return GetHandleMethod(handler.GetType(), eventType) != null;
    }

    public IEnumerable<Type> GetSupportedEventTypes(IEventHandler handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var supportedTypes = new List<Type>();

        if (handler is IPolymorphicHandler polymorphic)
        {
            return polymorphic.GetSupportedEventTypes();
        }

        // Look for IEventHandler<TEvent> implementations
        var interfaces = handler.GetType().GetInterfaces();
        foreach (var @interface in interfaces)
        {
            if (@interface.IsGenericType &&
                @interface.GetGenericTypeDefinition() == typeof(IEventHandler<>))
            {
                var eventType = @interface.GetGenericArguments()[0];
                supportedTypes.Add(eventType);
            }
        }

        return supportedTypes;
    }

    private MethodInfo? GetHandleMethod(Type handlerType, Type eventType)
    {
        var cacheKey = (handlerType, eventType);

        lock (_cacheLock)
        {
            if (_methodCache.TryGetValue(cacheKey, out var cachedMethod))
                return cachedMethod;
        }

        var method = handlerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m =>
                m.Name == "Handle" &&
                m.GetParameters().Length == 2 &&
                m.GetParameters()[0].ParameterType == eventType &&
                m.GetParameters()[1].ParameterType == typeof(CancellationToken) &&
                m.ReturnType == typeof(Task));

        lock (_cacheLock)
        {
            _methodCache[cacheKey] = method!;
        }

        return method;
    }
}
