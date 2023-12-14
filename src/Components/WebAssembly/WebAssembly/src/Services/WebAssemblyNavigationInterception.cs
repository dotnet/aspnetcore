// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

internal sealed class WebAssemblyNavigationInterception : INavigationInterception
{
    public static readonly WebAssemblyNavigationInterception Instance = new WebAssemblyNavigationInterception();

    public Task EnableNavigationInterceptionAsync()
    {
        InternalJSImportMethods.Instance.NavigationManager_EnableNavigationInterception((int)WebRendererId.WebAssembly);
        return Task.CompletedTask;
    }
}
