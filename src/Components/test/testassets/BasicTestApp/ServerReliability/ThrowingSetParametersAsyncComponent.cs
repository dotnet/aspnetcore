// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;

namespace BasicTestApp.ServerReliability;

public class ThrowingSetParametersAsyncComponent : IComponent
{
    public void Attach(RenderHandle renderHandle)
    {
    }

    public async Task SetParametersAsync(ParameterView parameters)
    {
        await Task.Yield();
        throw new InvalidTimeZoneException();
    }
}
