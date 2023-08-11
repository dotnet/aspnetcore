// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

internal class RazorComponentsEndpointsDetailedErrorsConfiguration : IConfigureOptions<RazorComponentsEndpointOptions>
{
    public RazorComponentsEndpointsDetailedErrorsConfiguration(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void Configure(RazorComponentsEndpointOptions options)
    {
        var value = Configuration[WebHostDefaults.DetailedErrorsKey];
        options.DetailedErrors = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
    }
}
