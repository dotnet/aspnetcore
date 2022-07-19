// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.WebAssembly.Infrastructure;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

internal sealed class WebAssemblyNavigationInterception : INavigationInterception
{
    private readonly IComponentsInternalCalls _internalCalls;
    public WebAssemblyNavigationInterception(IJSRuntime jsRuntime)
    {
        _internalCalls = ((IInternalCallsProvider)jsRuntime).GetInternalCalls<IComponentsInternalCalls>();

    }

    public Task EnableNavigationInterceptionAsync()
    {
        _internalCalls.EnableNavigationInterception();
        return Task.CompletedTask;
    }
}
