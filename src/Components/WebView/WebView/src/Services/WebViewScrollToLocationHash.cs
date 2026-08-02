// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebView.Services;

internal sealed class WebViewScrollToLocationHash : IScrollToLocationHash
{
    private IJSRuntime _jsRuntime;

    public void AttachJSRuntime(IJSRuntime jsRuntime)
    {
        if (HasAttachedJSRuntime)
        {
            throw new InvalidOperationException("JSRuntime has already been initialized.");
        }

        _jsRuntime = jsRuntime;
    }

    public bool HasAttachedJSRuntime => _jsRuntime != null;

    public async Task RefreshScrollPositionForHash(string locationAbsolute)
    {
        if (!HasAttachedJSRuntime)
        {
            throw new InvalidOperationException("JSRuntime has not been attached.");
        }

        var hashIndex = locationAbsolute.IndexOf("#", StringComparison.Ordinal);

        if (hashIndex > -1 && locationAbsolute.Length > hashIndex + 1)
        {
            var elementId = locationAbsolute[(hashIndex + 1)..];

            await _jsRuntime.InvokeVoidAsync("Blazor._internal.navigationManager.scrollToElement", elementId).AsTask();
        }
    }
}
