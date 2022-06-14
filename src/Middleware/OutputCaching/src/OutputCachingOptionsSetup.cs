// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.OutputCaching;

internal sealed class OutputCachingOptionsSetup : IConfigureOptions<OutputCachingOptions>
{
    private readonly IServiceProvider _services;

    public OutputCachingOptionsSetup(IServiceProvider services)
    {
        _services = services;
    }

    public void Configure(OutputCachingOptions options)
    {
        options.ApplicationServices = _services;
    }
}
