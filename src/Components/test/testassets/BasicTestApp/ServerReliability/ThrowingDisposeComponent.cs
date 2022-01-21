// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;

namespace BasicTestApp.ServerReliability;

public class ThrowingDisposeComponent : IComponent, IDisposable
{
    public void Attach(RenderHandle renderHandle)
    {
        renderHandle.Render(builder =>
        {
            // Do nothing.
        });
    }

    public void Dispose()
    {
        throw new InvalidTimeZoneException();
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        return Task.CompletedTask;
    }
}
