// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore;

internal sealed class ForwardedHeadersStartupFilter : IStartupFilter
{
    private readonly IConfiguration _configuration;

    public ForwardedHeadersStartupFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        if (!string.Equals("true", _configuration["ForwardedHeaders_Enabled"], StringComparison.OrdinalIgnoreCase))
        {
            return next;
        }

        return app =>
        {
            app.UseForwardedHeaders();
            next(app);
        };
    }
}
