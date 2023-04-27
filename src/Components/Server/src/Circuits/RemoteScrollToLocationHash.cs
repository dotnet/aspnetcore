// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Interop = Microsoft.AspNetCore.Components.Web.BrowserNavigationManagerInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

internal sealed class RemoteScrollToLocationHash : IScrollToLocationHash
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
            // We should generally never get here in the ordinary case. Router will only call this API once pre-rendering is complete.
            // This would guard any unusual usage of this API.
            throw new InvalidOperationException("Navigation commands can not be issued at this time. This is because the component is being " +
                "prerendered and the page has not yet loaded in the browser or because the circuit is currently disconnected. " +
                "Components must wrap any navigation calls in conditional logic to ensure those navigation calls are not " +
                "attempted during prerendering or while the client is disconnected.");
        }

        var hashIndex = locationAbsolute.IndexOf("#", StringComparison.Ordinal);

        if (hashIndex > -1 && locationAbsolute.Length > hashIndex + 1)
        {
            var elementId = locationAbsolute[(hashIndex + 1)..];

            await _jsRuntime.InvokeVoidAsync(Interop.ScrollToElement, elementId);
        }
    }
}
