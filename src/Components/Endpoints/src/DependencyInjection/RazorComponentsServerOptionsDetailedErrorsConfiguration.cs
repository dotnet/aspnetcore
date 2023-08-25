// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Endpoints.FormMapping;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

internal class RazorComponentsServerOptionsDetailedErrorsConfiguration : IConfigureOptions<RazorComponentsServerOptions>
{
    private readonly ILoggerFactory _loggerFactory;

    public RazorComponentsServerOptionsDetailedErrorsConfiguration(
        IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        Configuration = configuration;
        _loggerFactory = loggerFactory;
    }

    public IConfiguration Configuration { get; }

    public void Configure(RazorComponentsServerOptions options)
    {
        var value = Configuration[WebHostDefaults.DetailedErrorsKey];
        options.DetailedErrors = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);

        options._formMappingOptions = new FormDataMapperOptions(_loggerFactory);
    }
}
