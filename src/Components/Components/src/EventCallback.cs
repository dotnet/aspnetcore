// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// A bound event handler delegate.
/// </summary>
public readonly struct EventCallback : IEventCallback
{
    /// <summary>
    /// Gets a reference to the <see cref="EventCallbackFactory"/>.
    /// </summary>
    public static readonly EventCallbackFactory Factory = new EventCallbackFactory();

    /// <summary>
    /// Gets an empty <see cref="EventCallback"/>.
    /// </summary>
    public static readonly EventCallback Empty = new EventCallback(null, (Action)(() => { }));

    internal readonly MulticastDelegate? Delegate;
    internal readonly IHandleEvent? Receiver;

    /// <summary>
    /// Creates the new <see cref="EventCallback"/>.
    /// </summary>
    /// <param name="receiver">The event receiver.</param>
    /// <param name="delegate">The delegate to bind.</param>
    public EventCallback(IHandleEvent? receiver, MulticastDelegate? @delegate)
    {
        Receiver = receiver;
        Delegate = @delegate;
    }

    /// <summary>
    /// Gets a value that indicates whether the delegate associated with this event dispatcher is non-null.
    /// </summary>
    public bool HasDelegate => Delegate != null;

    // This is a hint to the runtime that Receiver is a different object than what
    // Delegate.Target points to. This allows us to avoid boxing the command object
    // when building the render tree. See logic where this is used.
    internal bool RequiresExplicitReceiver => Receiver != null && !object.ReferenceEquals(Receiver, Delegate?.Target);

    /// <summary>
    /// Invokes the delegate associated with this binding and dispatches an event notification to the
    /// appropriate component.
    /// </summary>
    /// <param name="arg">The argument.</param>
    /// <returns>A <see cref="Task"/> which completes asynchronously once event processing has completed.</returns>
    public Task InvokeAsync(object? arg)
    {
        if (Receiver == null)
        {
            return EventCallbackWorkItem.InvokeAsync<object?>(Delegate, arg);
        }

        return Receiver.HandleEventAsync(new EventCallbackWorkItem(Delegate), arg);
    }

    /// <summary>
    /// Invokes the delegate associated with this binding and dispatches an event notification to the
    /// appropriate component.
    /// </summary>
    /// <returns>A <see cref="Task"/> which completes asynchronously once event processing has completed.</returns>
    public Task InvokeAsync() => InvokeAsync(null!);

    object? IEventCallback.UnpackForRenderTree()
    {
        return RequiresExplicitReceiver ? (object)this : Delegate;
    }

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(RuntimeHelpers.GetHashCode(Receiver), RuntimeHelpers.GetHashCode(Delegate));

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is EventCallback other
        && ReferenceEquals(Receiver, other.Receiver)
        && (Delegate?.Equals(other.Delegate) ?? (other.Delegate == null));
}
