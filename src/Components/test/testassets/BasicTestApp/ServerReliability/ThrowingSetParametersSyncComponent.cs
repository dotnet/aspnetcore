// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;

namespace BasicTestApp.ServerReliability;

public class ThrowingSetParametersSyncComponent : IComponent
{
    public void Attach(RenderHandle renderHandle)
    {
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        throw new InvalidTimeZoneException();
    }
}
