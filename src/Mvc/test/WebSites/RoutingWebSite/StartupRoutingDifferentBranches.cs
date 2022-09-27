// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;

namespace RoutingWebSite;

public class StartupRoutingDifferentBranches
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
        // This is used by test response generator
        services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        services.AddSingleton<BranchesTransformer>();
    }

    public virtual void Configure(IApplicationBuilder app)
    {
        app.Map("/subdir", branch =>
        {
            branch.UseRouting();

            branch.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllerRoute(null, "literal/{controller}/{action}/{subdir}");
                endpoints.MapDynamicControllerRoute<BranchesTransformer>("literal/dynamic/controller/{**slug}");
            });
        });

        app.Map("/common", branch =>
        {
            branch.UseRouting();

            branch.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(null, "{controller}/{action}/{common}/literal");
                endpoints.MapDynamicControllerRoute<BranchesTransformer>("dynamic/controller/literal/{**slug}");
            });
        });

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapDynamicControllerRoute<BranchesTransformer>("dynamicattributeorder/dynamic/route/{**slug}");
            endpoints.MapControllerRoute(null, "{controller}/literal/{action}/{default}");
        });

        app.Run(c =>
        {
            return c.Response.WriteAsync("Hello from middleware after routing");
        });
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

public class BranchesTransformer : DynamicRouteValueTransformer
{
    public override ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
    {
        return new ValueTask<RouteValueDictionary>(new RouteValueDictionary(new { controller = "Branches", action = "Index" }));
    }
}
