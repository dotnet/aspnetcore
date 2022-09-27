// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace RoutingWebSite;

public class Startup
{
    // Set up application services
    public void ConfigureServices(IServiceCollection services)
    {
        var pageRouteTransformerConvention = new PageRouteTransformerConvention(new SlugifyParameterTransformer());

        services
            .AddMvc(ConfigureMvcOptions)
            .AddNewtonsoftJson()
            .AddRazorPagesOptions(options =>
            {
                options.Conventions.AddPageRoute("/PageRouteTransformer/PageWithConfiguredRoute", "/PageRouteTransformer/NewConventionRoute/{id?}");
                options.Conventions.AddFolderRouteModelConvention("/PageRouteTransformer", model =>
                {
                    pageRouteTransformerConvention.Apply(model);
                });
            });

        ConfigureRoutingServices(services);

        services.AddScoped<TestResponseGenerator>();
        services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
    }

    public virtual void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                "NonParameterConstraintRoute",
                "NonParameterConstraintRoute/{controller}/{action}",
                defaults: null,
                constraints: new { controller = "NonParameterConstraint", nonParameter = new QueryStringConstraint() });

            endpoints.MapControllerRoute(
                "DataTokensRoute",
                "DataTokensRoute/{controller}/{action}",
                defaults: null,
                constraints: new { controller = "DataTokens" },
                dataTokens: new { hasDataTokens = true });

            endpoints.MapControllerRoute(
                "ConventionalTransformerRoute",
                "ConventionalTransformerRoute/{controller:slugify}/{action=Index}/{param:slugify?}",
                defaults: null,
                constraints: new { controller = "ConventionalTransformer" });

            endpoints.MapControllerRoute(
                "DefaultValuesRoute_OptionalParameter",
                "DefaultValuesRoute/Optional/{controller=DEFAULTVALUES}/{action=OPTIONALPARAMETER}/{id?}/{**catchAll}",
                defaults: null,
                constraints: new { controller = "DefaultValues", action = "OptionalParameter" });

            endpoints.MapControllerRoute(
                "DefaultValuesRoute_DefaultParameter",
                "DefaultValuesRoute/Default/{controller=DEFAULTVALUES}/{action=DEFAULTPARAMETER}/{id=17}/{**catchAll}",
                defaults: null,
                constraints: new { controller = "DefaultValues", action = "DefaultParameter" });

            endpoints.MapAreaControllerRoute(
                "flightRoute",
                "adminRoute",
                "{area:exists}/{controller}/{action}",
                defaults: new { controller = "Home", action = "Index" },
                constraints: new { area = "Travel" });

            endpoints.MapControllerRoute(
                "PageRoute",
                "{controller}/{action}/{page}",
                defaults: null,
                constraints: new { controller = "PageRoute" });

            endpoints.MapControllerRoute(
                "ActionAsMethod",
                "{controller}/{action}",
                defaults: new { controller = "Home", action = "Index" });

            endpoints.MapControllerRoute(
                "RouteWithOptionalSegment",
                "{controller}/{action}/{path?}");

            endpoints.MapRazorPages();
        });

        app.Map("/afterrouting", b => b.Run(c =>
        {
            return c.Response.WriteAsync("Hello from middleware after routing");
        }));
    }

    protected virtual void ConfigureMvcOptions(MvcOptions options)
    {
        // Add route token transformer to one controller
        options.Conventions.Add(new ControllerRouteTokenTransformerConvention(
            typeof(ParameterTransformerController),
            new SlugifyParameterTransformer()));
    }

    protected virtual void ConfigureRoutingServices(IServiceCollection services)
    {
        services.AddRouting(options => options.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer));
    }
}
