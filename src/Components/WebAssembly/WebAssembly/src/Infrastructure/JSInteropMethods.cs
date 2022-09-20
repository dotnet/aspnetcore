// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Infrastructure;

/// <summary>
/// Contains methods called by interop. Intended for framework use only, not supported for use in application
/// code.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class JSInteropMethods
{
    /// <summary>
    /// For framework use only.
    /// </summary>
    [Obsolete("This API is for framework use only and is no longer used in the current version")]
    public static void NotifyLocationChanged(string uri, bool isInterceptedLink)
        => WebAssemblyNavigationManager.Instance.SetLocation(uri, null, isInterceptedLink);

    /// <summary>
    /// For framework use only.
    /// </summary>
    [JSInvokable(nameof(NotifyLocationChanged))]
    public static void NotifyLocationChanged(string uri, string? state, bool isInterceptedLink)
    {
        WebAssemblyNavigationManager.Instance.SetLocation(uri, state, isInterceptedLink);
    }

    /// <summary>
    /// For framework use only.
    /// </summary>
    [JSInvokable(nameof(NotifyLocationChangingAsync))]
    public static async ValueTask<bool> NotifyLocationChangingAsync(string uri, string? state, bool isInterceptedLink)
    {
        return await WebAssemblyNavigationManager.Instance.HandleLocationChangingAsync(uri, state, isInterceptedLink);
    }
}
