// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid.Infrastructure;

/// <summary>
/// Represents an event that you may subscribe to. This differs from normal C# events in that the handlers
/// are EventCallback<typeparamref name="T"/>, and so may have async behaviors and cause component re-rendering
/// while retaining error flow.
/// </summary>
/// <typeparam name="T">A type for the eventargs.</typeparam>
internal sealed class EventCallbackSubscribable<T>
{
    private readonly Dictionary<EventCallbackSubscriber<T>, EventCallback<T>> _callbacks = new();

    /// <summary>
    /// Invokes all the registered callbacks sequentially, in an undefined order.
    /// </summary>
    public async Task InvokeCallbacksAsync(T eventArg)
    {
        foreach (var callback in _callbacks.Values)
        {
            await callback.InvokeAsync(eventArg);
        }
    }

    // Don't call this directly - it gets called by EventCallbackSubscription
    public void Subscribe(EventCallbackSubscriber<T> owner, EventCallback<T> callback)
        => _callbacks.Add(owner, callback);

    // Don't call this directly - it gets called by EventCallbackSubscription
    public void Unsubscribe(EventCallbackSubscriber<T> owner)
        => _callbacks.Remove(owner);
}
