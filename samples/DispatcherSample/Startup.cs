// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DispatcherSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<UrlGenerator>();
            services.AddSingleton<RouteValueAddressTable>();
            services.AddDispatcher();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.Use(async (context, next) =>
            {
                await context.Response.WriteAsync("<p>Middleware 1</p>");
                await next.Invoke();
            });

            app.UseDispatcher();

            app.Use(async (context, next) =>
            {
                await context.Response.WriteAsync("<p>Middleware 2</p>");
                await next.Invoke();
            });

            app.Use(async (context, next) =>
            {
                var urlGenerator = app.ApplicationServices.GetService<UrlGenerator>();
                var url = urlGenerator.GenerateURL(new RouteValueDictionary(new { Movie = "The Lion King", Character = "Mufasa" }), context);
                await context.Response.WriteAsync($"<p>Generated url: {url}</p>");
                await next.Invoke();
            });
        }
    }
}
