// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing.Constraints;

namespace RoutingWebSite;

public class UseRouterStartup
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
            routes.DefaultHandler = new RouteHandler((httpContext) =>
            {
                var request = httpContext.Request;
                return httpContext.Response.WriteAsync($"Verb =  {request.Method.ToUpperInvariant()} - Path = {request.Path} - Route values - {string.Join(", ", httpContext.GetRouteData().Values)}");
            });

            routes.MapGet("api/get/{id}", (request, response, routeData) => response.WriteAsync($"API Get {routeData.Values["id"]}"))
                  .MapMiddlewareRoute("api/middleware", (appBuilder) => appBuilder.Run(httpContext => httpContext.Response.WriteAsync("Middleware!")))
                  .MapRoute(
                    name: "AllVerbs",
                    template: "api/all/{name}/{lastName?}",
                    defaults: new { lastName = "Doe" },
                    constraints: new { lastName = new RegexRouteConstraint(new Regex("[a-zA-Z]{3}", RegexOptions.CultureInvariant, RegexMatchTimeout)) });
        });

        app.Map("/Branch1", branch => SetupBranch(branch, "Branch1"));
        app.Map("/Branch2", branch => SetupBranch(branch, "Branch2"));
    }

    private void SetupBranch(IApplicationBuilder app, string name)
    {
        app.UseRouter(routes =>
        {
            routes.MapGet("api/get/{id}", (request, response, routeData) => response.WriteAsync($"{name} - API Get {routeData.Values["id"]}"));
        });
    }
}
