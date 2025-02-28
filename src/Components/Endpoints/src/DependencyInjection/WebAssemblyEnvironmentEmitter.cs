// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class WebAssemblyEnvironmentEmitter(IHostEnvironment hostEnvironment)
{
    private bool wasEmittedAlready;

    public string? GetEnvironmentOnce()
    {
        if (wasEmittedAlready)
        {
            return null;
        }

        wasEmittedAlready = true;
        return hostEnvironment.EnvironmentName;
    }
}
