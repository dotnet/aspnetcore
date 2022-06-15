// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.OutputCaching;

internal sealed class OutputCacheOptionsSetup : IConfigureOptions<OutputCacheOptions>
{
    private readonly IServiceProvider _services;

    public OutputCacheOptionsSetup(IServiceProvider services)
    {
        _services = services;
    }

    public void Configure(OutputCacheOptions options)
    {
        options.ApplicationServices = _services;
    }
}
