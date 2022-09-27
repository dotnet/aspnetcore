// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Interop = Microsoft.AspNetCore.Components.Web.BrowserNavigationManagerInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

internal sealed class WebAssemblyNavigationInterception : INavigationInterception
{
    public static readonly WebAssemblyNavigationInterception Instance = new WebAssemblyNavigationInterception();

    public Task EnableNavigationInterceptionAsync()
    {
        DefaultWebAssemblyJSRuntime.Instance.InvokeVoid(Interop.EnableNavigationInterception);
        return Task.CompletedTask;
    }
}
