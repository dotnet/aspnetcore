// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace RoutingWebSite
{
    public class MapFallbackStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting(routes =>
            {
                routes.MapFallback("/prefix/{*path:nonfile}", (context) =>
                {
                    return context.Response.WriteAsync("FallbackCustomPattern");
                });

                routes.MapFallback((context) =>
                {
                    return context.Response.WriteAsync("FallbackDefaultPattern");
                });

                routes.MapHello("/helloworld", "World");
            });

            app.UseEndpoint();
        }
    }
}
