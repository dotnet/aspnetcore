// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// One listener added with <c>addEventListener</c>. Disposing it or canceling its token removes the listener.
/// </summary>
internal sealed class EventListener<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TEvent> : IAsyncDisposable
{
    private readonly EventListenerCollection _owner;
    private readonly IJSObjectReference _target;
    private readonly string _type;
    private readonly Func<TEvent, Task> _listener;
    private readonly CancellationToken _cancellationToken;
    private readonly ILogger _logger;
    private IJSObjectReference? _function;
    private IDisposable? _callbackReference;
    private SynchronizationContext? _synchronizationContext;
    private CancellationTokenRegistration _cancellationRegistration;
    private bool _removed;

    public EventListener(EventListenerCollection owner, IJSObjectReference target, string type, Func<TEvent, Task> listener, CancellationToken cancellationToken, ILogger logger)
    {
        _owner = owner;
        _target = target;
        _type = type;
        _listener = listener;
        _cancellationToken = cancellationToken;
        _logger = logger;
    }

    public async Task AddAsync(JSCallbackFactory callbackFactory, string shapesJson)
    {
        var callback = new JSCallback<TEvent> { Handler = InvokeListenerAsync };
        (_function, _callbackReference) = await callbackFactory.CreateAsync(callback, shapesJson);

        // Disposal can run while the function is being created, and then nothing else releases it.
        if (_removed || _cancellationToken.IsCancellationRequested)
        {
            await ReleaseAsync();
            _cancellationToken.ThrowIfCancellationRequested();
            throw new ObjectDisposedException(GetType().Name);
        }

        await _target.InvokeVoidAsync("addEventListener", _type, _function);
    }

    /// <summary>
    /// Removes the listener when its token is canceled. Call it once the listener is added.
    /// </summary>
    public void RemoveWhenCanceled()
    {
        // Not useSynchronizationContext: its Send blocks forever when Cancel runs on Blazor's renderer context.
        _synchronizationContext = SynchronizationContext.Current;
        _cancellationRegistration = _cancellationToken.Register(static state => ((EventListener<TEvent>)state!).OnCanceled(), this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_removed)
        {
            return;
        }

        _removed = true;
        _cancellationRegistration.Dispose();
        _owner.Remove(this);

        await ReleaseAsync();
    }

    private void OnCanceled()
    {
        // Removal changes unlocked state, so it runs on the context that added the listener.
        if (_synchronizationContext is { } context)
        {
            context.Post(static state => _ = ((EventListener<TEvent>)state!).RemoveAfterCancellationAsync(), this);
        }
        else
        {
            _ = RemoveAfterCancellationAsync();
        }
    }

    private async Task RemoveAfterCancellationAsync() => await _owner.TryDisposeAsync(this);

    private async Task InvokeListenerAsync(TEvent value)
    {
        // A call that left the browser before removal can still arrive.
        if (_removed || _cancellationToken.IsCancellationRequested)
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
