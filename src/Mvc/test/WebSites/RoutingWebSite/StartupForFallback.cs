// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace RoutingWebSite;

// For by tests for fallback routing to pages/controllers
public class StartupForFallback
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddMvc()
            .AddNewtonsoftJson();

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
            endpoints.MapFallbackToAreaController("admin/{*path:nonfile}", "Index", "Fallback", "Admin");
            endpoints.MapFallbackToPage("FallbackToPage/{*path:nonfile}", "/FallbackPage");
            endpoints.MapFallbackToFile("notfound.html");

            endpoints.MapControllerRoute("admin", "link_generation/{area}/{controller}/{action}/{id?}");
        });

        app.Map("/afterrouting", b => b.Run(c =>
        {
            return c.Response.WriteAsync("Hello from middleware after routing");
        }));
    }
}
