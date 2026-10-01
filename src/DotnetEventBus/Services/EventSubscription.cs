#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetEventBus.Services;

/// <summary>
/// Represents a disposable subscription token returned by Subscribe methods.
/// Disposing this token removes the subscription from the event bus.
/// </summary>
public sealed class EventSubscription : IDisposable
{
    private Action? _unsubscribeAction;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSubscription"/> class.
    /// </summary>
    /// <param name="unsubscribeAction">The action to invoke when this subscription is disposed.</param>
    public EventSubscription(Action unsubscribeAction)
    {
        _unsubscribeAction = unsubscribeAction ?? throw new ArgumentNullException(nameof(unsubscribeAction));
    }

    /// <summary>
    /// Gets whether this subscription has been disposed (unsubscribed).
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Creates a no-op subscription that does nothing on dispose.
    /// Useful for testing or conditional subscription scenarios.
    /// </summary>
    /// <returns>An <see cref="EventSubscription"/> that performs no action on dispose.</returns>
    public static EventSubscription Empty() => new(() => { });

    /// <summary>
    /// Combines multiple subscriptions into a single disposable token.
    /// Disposing the composite disposes all inner subscriptions.
    /// </summary>
    /// <param name="subscriptions">The subscriptions to combine.</param>
    /// <returns>A composite <see cref="EventSubscription"/>.</returns>
    public static EventSubscription Composite(params IDisposable[] subscriptions)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);
        return new EventSubscription(() =>
        {
            foreach (var sub in subscriptions)
            {
                sub.Dispose();
            }
        });
    }

    /// <summary>
    /// Removes the subscription from the event bus by invoking the unsubscribe action.
    /// Safe to call multiple times; only the first call has an effect.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        var action = Interlocked.Exchange(ref _unsubscribeAction, null);
        action?.Invoke();
    }
}
