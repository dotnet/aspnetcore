// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.WebAssembly.Server;

internal sealed class WebAssemblyComponentsServiceOptionsConfiguration(IConfiguration configuration, IServiceProviderIsService serviceProviderIsService) : IPostConfigureOptions<WebAssemblyComponentsServiceOptions>
{
    public void PostConfigure(string? name, WebAssemblyComponentsServiceOptions options)
    {
        var value = configuration["Components:UseCultureFromServer"];
        options.UseCultureFromServer = value?.ToLowerInvariant() switch
        {
            "true" or "1" => true,
            "false" or "0" => false,
            _ => serviceProviderIsService.IsService(typeof(IStringLocalizerFactory)),
        };
    }
}
