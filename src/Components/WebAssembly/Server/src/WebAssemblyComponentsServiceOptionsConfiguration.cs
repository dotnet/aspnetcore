// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.WebAssembly.Server;

internal sealed class WebAssemblyComponentsServiceOptionsConfiguration(IConfiguration configuration) : IPostConfigureOptions<WebAssemblyComponentsServiceOptions>
{
    public void PostConfigure(string? name, WebAssemblyComponentsServiceOptions options)
    {
        var value = configuration["Components:UseCultureFromServer"];
        if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase))
        {
            options.UseCultureFromServer = true;
        }
    }
}
