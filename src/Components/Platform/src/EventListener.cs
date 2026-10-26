// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// One listener added with <c>addEventListener</c>. Disposing it removes the listener.
/// </summary>
internal sealed class EventListener<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TEvent> : IAsyncDisposable
{
    private readonly EventListenerCollection _owner;
    private readonly IJSObjectReference _target;
    private readonly string _type;
    private readonly Func<TEvent, Task> _listener;
    private readonly ILogger _logger;
    private IJSObjectReference? _function;
    private IDisposable? _callbackReference;
    private bool _removed;

    public EventListener(EventListenerCollection owner, IJSObjectReference target, string type, Func<TEvent, Task> listener, ILogger logger)
    {
        _owner = owner;
        _target = target;
        _type = type;
        _listener = listener;
        _logger = logger;
    }

    public async Task AddAsync(JSCallbackFactory callbackFactory, string shapesJson)
    {
        var callback = new JSCallback<TEvent> { Handler = InvokeListenerAsync };
        (_function, _callbackReference) = await callbackFactory.CreateAsync(callback, shapesJson);

        // Disposal can run while the function is being created, and then nothing else releases it.
        if (_removed)
        {
            await ReleaseAsync();
            throw new ObjectDisposedException(GetType().Name);
        }

        await _target.InvokeVoidAsync("addEventListener", _type, _function);
    }

    public async ValueTask DisposeAsync()
    {
        if (_removed)
        {
            return;
        }

        _removed = true;
        _owner.Remove(this);

        await ReleaseAsync();
    }

    private async Task InvokeListenerAsync(TEvent value)
    {
        // A call that left the browser before removal can still arrive.
        if (_removed)
        {
            return;
        }

        try
        {
            await _listener(value);
        }
        catch (Exception ex)
        {
            EventListenerCollection.Log.ListenerThrew(_logger, _type, ex);
        }
    }

    private async ValueTask ReleaseAsync()
    {
        var function = _function;
        var callbackReference = _callbackReference;
        _function = null;
        _callbackReference = null;

        // A failed removal still releases both references, and then reaches the caller.
        try
        {
            if (function is not null)
            {
                try
                {
                    await _target.InvokeVoidAsync("removeEventListener", _type, function);
                }
                catch (JSDisconnectedException)
                {
                    // The page is already gone, so nothing can call the listener.
                }
                finally
                {
                    await function.DisposeAsync();
                }
            }
        }
        finally
        {
            callbackReference?.Dispose();
        }
    }
}
