// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace RoutingWebSite;

public class MapFallbackStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapFallback("/prefix/{*path:nonfile}", (context) =>
            {
                return context.Response.WriteAsync("FallbackCustomPattern");
            });

            endpoints.MapFallback((context) =>
            {
                return context.Response.WriteAsync("FallbackDefaultPattern");
            });

            endpoints.MapHello("/helloworld", "World");
        });
    }
}
