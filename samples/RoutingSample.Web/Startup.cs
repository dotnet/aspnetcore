// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.DependencyInjection;

namespace RoutingSample.Web
{
    public class Startup
    {
        private static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(10);

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouter(routes =>
            {
                routes.DefaultHandler = new RouteHandler((c) =>
                {
                    return c.Response.WriteAsync($"Verb =  {c.Request.Method.ToUpperInvariant()} - Path = {c.Request.Path} - Route values - {string.Join(", ", c.GetRouteData().Values)}");
                });

                routes.MapGet("api/get/{id}", (c) => c.Response.WriteAsync($"API Get {c.GetRouteData().Values["id"]}"));

                routes.MapRoute("api/middleware", (IApplicationBuilder fork) => fork.Use((c, n) => c.Response.WriteAsync("Middleware!")));

                routes.MapRoute(
                    name: "AllVerbs",
                    template: "api/all/{name}/{lastName?}",
                    defaults: new { lastName = "Doe" },
                    constraints: new { lastName = new RegexRouteConstraint(new Regex("[a-zA-Z]{3}", RegexOptions.CultureInvariant, RegexMatchTimeout))});
            });
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}