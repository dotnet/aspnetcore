// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Virtualization;

internal sealed class VirtualizeJsInterop : IAsyncDisposable
{
    private const string JsFunctionsPrefix = "Blazor._internal.Virtualize";

    private readonly IVirtualizeJsCallbacks _owner;

    private readonly IJSRuntime _jsRuntime;

    private DotNetObjectReference<VirtualizeJsInterop>? _selfReference;
    private bool _disposed;

    [DynamicDependency(nameof(OnSpacerBeforeVisible))]
    [DynamicDependency(nameof(OnSpacerAfterVisible))]
    public VirtualizeJsInterop(IVirtualizeJsCallbacks owner, IJSRuntime jsRuntime)
    {
        _owner = owner;
        _jsRuntime = jsRuntime;
    }

    public async ValueTask InitializeAsync(ElementReference spacerBefore, ElementReference spacerAfter, int anchorMode)
    {
        if (_disposed)
        {
            return;
        }

        _selfReference = DotNetObjectReference.Create(this);
        await InvokeVoidAsync($"{JsFunctionsPrefix}.init", _selfReference, spacerBefore, spacerAfter, anchorMode);
    }

    [JSInvokable]
    public void OnSpacerBeforeVisible(float spacerSize, float spacerSeparation, float containerSize, int reason)
    {
        _owner.OnBeforeSpacerVisible(spacerSize, spacerSeparation, containerSize, (SpacerVisibilityReason)reason);
    }

    [JSInvokable]
    public void OnSpacerAfterVisible(float spacerSize, float spacerSeparation, float containerSize, int reason)
    {
        _owner.OnAfterSpacerVisible(spacerSize, spacerSeparation, containerSize, (SpacerVisibilityReason)reason);
    }

    public ValueTask ScrollToBottomAsync()
    {
        return InvokeVoidAsync($"{JsFunctionsPrefix}.scrollToBottom", _selfReference);
    }

    public ValueTask RefreshObserversAsync(bool isLoading)
    {
        return InvokeVoidAsync($"{JsFunctionsPrefix}.refreshObservers", _selfReference, isLoading);
    }

    public ValueTask SetAnchorModeAsync(int anchorMode)
    {
        return InvokeVoidAsync($"{JsFunctionsPrefix}.setAnchorMode", _selfReference, anchorMode);
    }

    public ValueTask RestoreAnchorAsync(bool onNextMutation = false)
    {
        return _jsRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.restoreAnchor", _selfReference, onNextMutation);
    }

    public async ValueTask<ViewportFillDirection?> AlignToItemAsync(int localIndex, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return null;
        }

        try
        {
            return await _jsRuntime.InvokeAsync<ViewportFillDirection?>($"{JsFunctionsPrefix}.alignToItem", cancellationToken, _selfReference, localIndex);
        }
        catch (ObjectDisposedException) when (_disposed)
        {
            return null;
        }
    }

    public ValueTask BeginProgrammaticScrollAsync()
    {
        return InvokeVoidAsync($"{JsFunctionsPrefix}.beginProgrammaticScroll", _selfReference);
    }

    public async ValueTask<bool> IsFollowingBottomAsync()
    {
        if (_disposed)
        {
            return false;
        }

        try
        {
            return await _jsRuntime.InvokeAsync<bool>($"{JsFunctionsPrefix}.isFollowingBottom", _selfReference);
        }
        catch (ObjectDisposedException) when (_disposed)
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_selfReference != null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.dispose", _selfReference);
            }
            catch (JSDisconnectedException)
            {
                // If the browser is gone, we don't need it to clean up any browser-side state
            }
            catch (ObjectDisposedException)
            {
                // The reference was already released while disposal was in progress.
            }
        }
    }

    private async ValueTask InvokeVoidAsync(string identifier, params object?[]? args)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            await _jsRuntime.InvokeVoidAsync(identifier, args);
        }
        catch (ObjectDisposedException) when (_disposed)
        {
        }
    }
}
