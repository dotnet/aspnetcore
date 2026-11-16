// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Internal;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Identifies an event that a <typeparamref name="TTarget"/> fires, and the type of the event object that listeners receive.
/// </summary>
/// <typeparam name="TTarget">The type of the object that fires the event.</typeparam>
/// <typeparam name="TEvent">The type of the event object.</typeparam>
public sealed class EventType<TTarget, TEvent> where TTarget : class
{
    private readonly Func<EventListenerCollection, IJSObjectReference, TTarget, Func<TEvent, Task>, CancellationToken, Task<IAsyncDisposable>> _addListenerAsync;

    internal EventType(
        string name,
        Func<EventListenerCollection, IJSObjectReference, TTarget, Func<TEvent, Task>, CancellationToken, Task<IAsyncDisposable>> addListenerAsync)
    {
        Name = name;
        _addListenerAsync = addListenerAsync;
    }

    /// <summary>
    /// Gets the name of the event, such as <c>"change"</c>.
    /// </summary>
    public string Name { get; }

    internal Task<IAsyncDisposable> AddListenerAsync(EventListenerCollection listeners, IJSObjectReference target, TTarget owner, Func<TEvent, Task> listener, CancellationToken cancellationToken)
        => _addListenerAsync(listeners, target, owner, listener, cancellationToken);
}

/// <summary>
/// Creates <see cref="EventType{TTarget, TEvent}"/> instances. The event object types are known here,
/// so the public methods that add listeners need no trimming annotations.
/// </summary>
internal static class EventTypes
{
    /// <summary>
    /// Creates an event whose object crosses to .NET as <typeparamref name="TEvent"/> itself.
    /// </summary>
    /// <param name="name">The JavaScript event name.</param>
    /// <param name="shapesJson">The <c>ArgumentShape</c> list (see <c>js/Platform.ts</c>) that copies the event object.</param>
    public static EventType<TTarget, TEvent> Create<TTarget, [DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TEvent>(
        string name,
        string shapesJson)
        where TTarget : class
        => new(name, (listeners, target, _, listener, cancellationToken) => listeners.AddAsync(target, name, shapesJson, listener, cancellationToken));

    /// <summary>
    /// Creates an event whose object crosses to .NET as <typeparamref name="TPayload"/> and is then converted,
    /// for members such as <c>storageArea</c> that refer to an existing projection.
    /// </summary>
    /// <param name="name">The JavaScript event name.</param>
    /// <param name="shapesJson">The <c>ArgumentShape</c> list (see <c>js/Platform.ts</c>) that copies the event object.</param>
    /// <param name="convert">The function that creates the event object from the target and the payload.</param>
    public static EventType<TTarget, TEvent> Create<TTarget, [DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TPayload, TEvent>(
        string name,
        string shapesJson,
        Func<TTarget, TPayload, TEvent> convert)
        where TTarget : class
        => new(name, (listeners, target, owner, listener, cancellationToken) =>
            listeners.AddAsync<TPayload>(target, name, shapesJson, payload => listener(convert(owner, payload)), cancellationToken));
}
