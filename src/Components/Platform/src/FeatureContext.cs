// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Provides what a projection needs when it is created, shared by all projections of the scope.
/// </summary>
internal sealed class FeatureContext : IInternalAsyncDisposal
{
    private readonly ILoggerFactory _loggerFactory;
    // Typed as the interface, so that disposal does not keep the type in an app that never adds a listener.
    private IInternalAsyncDisposal? _callbackFactory;
    private List<IInternalAsyncDisposal>? _listenerCollections;
    private bool _disposed;

    public FeatureContext(IJSRuntime jsRuntime, ILoggerFactory loggerFactory)
    {
        JSRuntime = jsRuntime;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Gets the JS runtime of the scope.
    /// </summary>
    public IJSRuntime JSRuntime { get; }

    /// <summary>
    /// Creates the collection that a projection uses to add event listeners to its JavaScript object.
    /// </summary>
    public EventListenerCollection CreateEventListenerCollection()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var listeners = new EventListenerCollection(this, (JSCallbackFactory)(_callbackFactory ??= new JSCallbackFactory(JSRuntime)), _loggerFactory.CreateLogger<EventListenerCollection>());
        (_listenerCollections ??= []).Add(listeners);

        return listeners;
    }

    internal void RemoveEventListenerCollection(IInternalAsyncDisposal listeners) => _listenerCollections?.Remove(listeners);

    // Window calls this last; remaining collections belong to caller-owned targets.
    async ValueTask IInternalAsyncDisposal.InternalDisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_listenerCollections is { } collections)
        {
            foreach (var listeners in collections.ToArray())
            {
                await listeners.InternalDisposeAsync();
            }
        }

        if (_callbackFactory is { } callbackFactory)
        {
            await callbackFactory.InternalDisposeAsync();
        }
    }
}
