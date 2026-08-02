// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Provides extension methods for <see cref="EventCallbackFactory"/> and <see cref="EventArgs"/> types.
/// </summary>
public static class EventCallbackFactoryEventArgsExtensions
{
    /// <summary>
    /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
    /// <paramref name="callback"/>.
    /// </summary>
    /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
    /// <param name="receiver">The event receiver.</param>
    /// <param name="callback">The event callback.</param>
    /// <returns>The <see cref="EventCallback"/>.</returns>
    public static EventCallback<EventArgs> Create(this EventCallbackFactory factory, object receiver, Action<EventArgs> callback)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return factory.Create<EventArgs>(receiver, callback);
    }

    /// <summary>
    /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
    /// <paramref name="callback"/>.
    /// </summary>
    /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
    /// <param name="receiver">The event receiver.</param>
    /// <param name="callback">The event callback.</param>
    /// <returns>The <see cref="EventCallback"/>.</returns>
    public static EventCallback<EventArgs> Create(this EventCallbackFactory factory, object receiver, Func<EventArgs, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return factory.Create<EventArgs>(receiver, callback);
    }

    /// <summary>
    /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
    /// <paramref name="callback"/>.
    /// </summary>
    /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
    /// <param name="receiver">The event receiver.</param>
    /// <param name="callback">The event callback.</param>
    /// <returns>The <see cref="EventCallback"/>.</returns>
    public static EventCallback<ChangeEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<ChangeEventArgs> callback)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return factory.Create<ChangeEventArgs>(receiver, callback);
    }

    /// <summary>
    /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
    /// <paramref name="callback"/>.
    /// </summary>
    /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
    /// <param name="receiver">The event receiver.</param>
    /// <param name="callback">The event callback.</param>
    /// <returns>The <see cref="EventCallback"/>.</returns>
    public static EventCallback<ChangeEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<ChangeEventArgs, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return factory.Create<ChangeEventArgs>(receiver, callback);
    }
}
