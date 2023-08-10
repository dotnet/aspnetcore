// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

internal sealed class WebAssemblyNavigationInterception : INavigationInterception
{
    private readonly NavigationManager _navigationManager;

    public WebAssemblyNavigationInterception(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    public Task EnableNavigationInterceptionAsync()
    {
        InternalJSImportMethods.Instance.NavigationManager_EnableNavigationInterception(_navigationManager.Uri);
        return Task.CompletedTask;
    }

    public Task DisableNavigationInterceptionAsync()
    {
        InternalJSImportMethods.Instance.NavigationManager_DisableNavigationInterception();
        return Task.CompletedTask;
    }
}
