// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// A factory for creating <see cref="EventCallback"/> and <see cref="EventCallback{T}"/>
/// instances.
/// </summary>
public sealed class EventCallbackFactory
{
    /// <summary>
    /// Returns the provided <paramref name="callback"/>. For internal framework use only.
    /// </summary>
    /// <param name="receiver"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public EventCallback Create(object receiver, EventCallback callback)
    {
        ArgumentNullException.ThrowIfNull(receiver);

        return callback;
    }

    /// <summary>
    /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
    /// <paramref name="callback"/>.
    /// </summary>
    /// <param name="receiver">The event receiver.</param>
    /// <param name="callback">The event callback.</param>
    /// <returns>The <see cref="EventCallback"/>.</returns>
    public EventCallback Create(object receiver, Action callback)
    {
        ArgumentNullException.ThrowIfNull(receiver);

        return CreateCore(receiver, callback);
    }

    /// <summary>
    /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
    /// <paramref name="callback"/>.
    /// </summary>
    /// <param name="receiver">The event receiver.</param>
    /// <param name="callback">The event callback.</param>
    /// <returns>The <see cref="EventCallback"/>.</returns>
    public EventCallback Create(object receiver, Action<object> callback)
    {
        ArgumentNullException.ThrowIfNull(receiver);

        return CreateCore(receiver, callback);
    }

    /// <summary>
    /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
    /// <paramref name="callback"/>.
    /// </summary>
    /// <param name="receiver">The event receiver.</param>
    /// <param name="callback">The event callback.</param>
    /// <returns>The <see cref="EventCallback"/>.</returns>
    public EventCallback Create(object receiver, Func<Task> callback)
    {
        ArgumentNullException.ThrowIfNull(receiver);

        return CreateCore(receiver, callback);
    }

    /// <summary>
    /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
    /// <paramref name="callback"/>.
    /// </summary>
    /// <param name="receiver">The event receiver.</param>
    /// <param name="callback">The event callback.</param>
    /// <returns>The <see cref="EventCallback"/>.</returns>
    public EventCallback Create(object receiver, Func<object, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(receiver);

        return CreateCore(receiver, callback);
    }

    /// <summary>
    /// Returns the provided <paramref name="callback"/>. For internal framework use only.
    /// </summary>
    /// <param name="receiver"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public EventCallback<TValue> Create<TValue>(object receiver, EventCallback callback)
    {
        ArgumentNullException.ThrowIfNull(receiver);

        return new EventCallback<TValue>(callback.Receiver, callback.Delegate);
    }

    /// <summary>
    /// Returns the provided <paramref name="callback"/>. For internal framework use only.
    /// </summary>
    /// <param name="receiver"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public EventCallback<TValue> Create<TValue>(object receiver, EventCallback<TValue> callback)
    {
        ArgumentNullException.ThrowIfNull(receiver);

        return callback;
    }

    /// <summary>
    /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
    /// <paramref name="callback"/>.
    /// </summary>
    /// <param name="receiver">The event receiver.</param>
    /// <param name="callback">The event callback.</param>
    /// <returns>The <see cref="EventCallback"/>.</returns>
    public EventCallback<TValue> Create<TValue>(object receiver, Action callback)
    {
        ArgumentNullException.ThrowIfNull(receiver);

        return CreateCore<TValue>(receiver, callback);
    }

    /// <summary>
    /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
    /// <paramref name="callback"/>.
    /// </summary>
    /// <param name="receiver">The event receiver.</param>
    /// <param name="callback">The event callback.</param>
    /// <returns>The <see cref="EventCallback"/>.</returns>
    public EventCallback<TValue> Create<TValue>(object receiver, Action<TValue> callback)
    {
        ArgumentNullException.ThrowIfNull(receiver);

        return CreateCore<TValue>(receiver, callback);
    }

    /// <summary>
    /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
    /// <paramref name="callback"/>.
    /// </summary>
    /// <param name="receiver">The event receiver.</param>
    /// <param name="callback">The event callback.</param>
    /// <returns>The <see cref="EventCallback"/>.</returns>
    public EventCallback<TValue> Create<TValue>(object receiver, Func<Task> callback)
    {
        ArgumentNullException.ThrowIfNull(receiver);

        return CreateCore<TValue>(receiver, callback);
    }

    /// <summary>
    /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
    /// <paramref name="callback"/>.
    /// </summary>
    /// <param name="receiver">The event receiver.</param>
    /// <param name="callback">The event callback.</param>
    /// <returns>The <see cref="EventCallback"/>.</returns>
    public EventCallback<TValue> Create<TValue>(object receiver, Func<TValue, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(receiver);

        return CreateCore<TValue>(receiver, callback);
    }

    /// <summary>
    /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
    /// <paramref name="callback"/>. For internal framework use only.
    /// </summary>
    /// <param name="receiver"></param>
    /// <param name="callback"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public EventCallback<TValue> CreateInferred<TValue>(object receiver, Action<TValue> callback, TValue value)
    {
        return Create(receiver, callback);
    }

    /// <summary>
    /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
    /// <paramref name="callback"/>. For internal framework use only.
    /// </summary>
    /// <param name="receiver"></param>
    /// <param name="callback"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public EventCallback<TValue> CreateInferred<TValue>(object receiver, Func<TValue, Task> callback, TValue value)
    {
        return Create(receiver, callback);
    }

    private static EventCallback CreateCore(object receiver, MulticastDelegate callback)
    {
        return new EventCallback(callback?.Target as IHandleEvent ?? receiver as IHandleEvent, callback);
    }

    private static EventCallback<TValue> CreateCore<TValue>(object receiver, MulticastDelegate callback)
    {
        return new EventCallback<TValue>(callback?.Target as IHandleEvent ?? receiver as IHandleEvent, callback);
    }
}
