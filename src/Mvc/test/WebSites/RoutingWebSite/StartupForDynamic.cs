// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;

namespace RoutingWebSite;

// For by tests for dynamic routing to pages/controllers
public class StartupForDynamic
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddMvc()
            .AddNewtonsoftJson();

        services.AddTransient<Transformer>();
        services.AddScoped<TestResponseGenerator>();
        services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

        // Used by some controllers defined in this project.
        services.Configure<RouteOptions>(options => options.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer));
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDynamicControllerRoute<Transformer>("v1/dynamic/{**slug}", new DynamicVersion { Version = "V1" });
            endpoints.MapDynamicControllerRoute<Transformer>("v2/dynamic/{**slug}", new DynamicVersion { Version = "V2" });
            endpoints.MapDynamicPageRoute<Transformer>("v1/dynamicpage/{**slug}", new DynamicVersion { Version = "V1" });
            endpoints.MapDynamicPageRoute<Transformer>("v2/dynamicpage/{**slug}", new DynamicVersion { Version = "V2" });

            endpoints.MapControllerRoute("link", "link_generation/{controller}/{action}/{id?}");
        });

        app.Map("/afterrouting", b => b.Run(c =>
        {
            return c.Response.WriteAsync("Hello from middleware after routing");
        }));
    }

    private class Transformer : DynamicRouteValueTransformer
    {
        // Turns a format like `controller=Home,action=Index` into an RVD
        public override ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
        {
            var kvps = ((string)values["slug"]).Split(",");

            var results = new RouteValueDictionary();
            foreach (var kvp in kvps)
            {
                var split = kvp.Replace("%2F", "/", StringComparison.OrdinalIgnoreCase).Split("=");
                results[split[0]] = split[1];
            }

            results["version"] = ((DynamicVersion)State).Version;

            return new ValueTask<RouteValueDictionary>(results);
        }

        public override ValueTask<IReadOnlyList<Endpoint>> FilterAsync(HttpContext httpContext, RouteValueDictionary values, IReadOnlyList<Endpoint> endpoints)
        {
            var version = ((DynamicVersion)State).Version;
            if (version == "V2" && version == (string)values["version"])
            {
                // For v1 routes this transformer will work fine, for v2 routes, it will filter them.
                return new ValueTask<IReadOnlyList<Endpoint>>(Array.Empty<Endpoint>());
            }
            return base.FilterAsync(httpContext, values, endpoints);
        }
    }
}
