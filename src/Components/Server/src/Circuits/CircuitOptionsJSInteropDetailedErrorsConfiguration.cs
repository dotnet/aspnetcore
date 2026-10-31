// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Server;

internal sealed class CircuitOptionsJSInteropDetailedErrorsConfiguration : IConfigureOptions<CircuitOptions>
{
    public CircuitOptionsJSInteropDetailedErrorsConfiguration(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void Configure(CircuitOptions options)
    {
        var value = Configuration[WebHostDefaults.DetailedErrorsKey];
        options.DetailedErrors = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
    }
}
