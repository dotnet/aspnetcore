// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;

namespace BasicTestApp.RouteViewLayoutTransition;

public abstract class TrackedPageBase : ComponentBase, IDisposable
{
    private readonly string _instanceId = Guid.NewGuid().ToString("N")[..8];

    protected TrackedPageBase()
    {
        Log("constructor");
    }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        Log("SetParametersAsync");
        await base.SetParametersAsync(parameters);
    }

    protected override void OnInitialized()
    {
        Log("OnInitialized");
    }

    protected override void OnParametersSet()
    {
        Log("OnParametersSet");
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            Log("OnAfterRender(firstRender)");
        }
    }

    public void Dispose()
    {
        Log("Dispose");
    }

    private void Log(string lifecycleEvent)
    {
        InstantiationTrace.Add($"{GetType().Name}[{_instanceId}] {lifecycleEvent}");
    }
}
