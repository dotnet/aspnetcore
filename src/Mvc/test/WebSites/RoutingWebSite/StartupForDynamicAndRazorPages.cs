// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Routing;

namespace RoutingWebSite;

// For by tests for a mix of dynamic routing + Razor Pages
public class StartupForDynamicAndRazorPages
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddMvc();

        services.AddTransient<Transformer>();

        // Used by some controllers defined in this project.
        services.Configure<RouteOptions>(options => options.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer));
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorPages();
            endpoints.MapDynamicControllerRoute<Transformer>("{language}/{**slug}");
        });
    }

    private class Transformer : DynamicRouteValueTransformer
    {
        // Turns a format like `controller=Home,action=Index` into an RVD
        public override ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
        {
            if (!(values["slug"] is string slug))
            {
                return new ValueTask<RouteValueDictionary>(values);
            }

            var kvps = slug.Split(",");

            var results = new RouteValueDictionary();
            foreach (var kvp in kvps)
            {
                var split = kvp.Replace("%2F", "/", StringComparison.OrdinalIgnoreCase).Split("=");
                results[split[0]] = split[1];
            }

            return new ValueTask<RouteValueDictionary>(results);
        }
    }
}
