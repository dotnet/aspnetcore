// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

internal sealed class WebAssemblyHostEnvironment : IWebAssemblyHostEnvironment
{
    public WebAssemblyHostEnvironment(string environment, string baseAddress)
    {
        Environment = environment;
        BaseAddress = baseAddress;
    }

    public string Environment { get; }

    public string BaseAddress { get; }
}
