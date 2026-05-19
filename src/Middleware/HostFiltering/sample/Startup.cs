// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.HostFiltering;
using Microsoft.Extensions.Options;

namespace HostFilteringSample;

public class Startup
{
    public IConfiguration Config { get; }

    public Startup(IConfiguration config)
    {
        Config = config;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHostFiltering(options =>
        {

        });

        // Fallback
        services.PostConfigure<HostFilteringOptions>(options =>
        {
            if (options.AllowedHosts == null || options.AllowedHosts.Count == 0)
            {
                // "AllowedHosts": "localhost;127.0.0.1;[::1]"
                var hosts = Config["AllowedHosts"]?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                // Fall back to "*" to disable.
                options.AllowedHosts = (hosts?.Length > 0 ? hosts : new[] { "*" });
            }
        });
        // Change notification
        services.AddSingleton<IOptionsChangeTokenSource<HostFilteringOptions>>(new ConfigurationChangeTokenSource<HostFilteringOptions>(Config));
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseHostFiltering();

        app.Run(context =>
        {
            return context.Response.WriteAsync("Hello World! " + context.Request.Host);
        });
    }
}
