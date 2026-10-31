// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RoutingWebSite;

public class StartupWithoutEndpointRouting : Startup
{
    public override void Configure(IApplicationBuilder app)
    {
        app.UseMvc(routes =>
        {
            routes.MapRoute(
                "NonParameterConstraintRoute",
                "NonParameterConstraintRoute/{controller}/{action}",
                defaults: null,
                constraints: new { controller = "NonParameterConstraint", nonParameter = new QueryStringConstraint() });

            routes.MapRoute(
                "DataTokensRoute",
                "DataTokensRoute/{controller}/{action}",
                defaults: null,
                constraints: new { controller = "DataTokens" },
                dataTokens: new { hasDataTokens = true });

            routes.MapRoute(
                "DefaultValuesRoute_OptionalParameter",
                "DefaultValuesRoute/Optional/{controller=DEFAULTVALUES}/{action=OPTIONALPARAMETER}/{id?}/{**catchAll}",
                defaults: null,
                constraints: new { controller = "DefaultValues", action = "OptionalParameter" });

            routes.MapRoute(
                "DefaultValuesRoute_DefaultParameter",
                "DefaultValuesRoute/Default/{controller=DEFAULTVALUES}/{action=DEFAULTPARAMETER}/{id=17}/{**catchAll}",
                defaults: null,
                constraints: new { controller = "DefaultValues", action = "DefaultParameter" });

            routes.MapAreaRoute(
                "flightRoute",
                "adminRoute",
                "{area:exists}/{controller}/{action}",
                defaults: new { controller = "Home", action = "Index" },
                constraints: new { area = "Travel" });

            routes.MapRoute(
                "ActionAsMethod",
                "{controller}/{action}",
                defaults: new { controller = "Home", action = "Index" });

            routes.MapRoute(
                "RouteWithOptionalSegment",
                "{controller}/{action}/{path?}");
        });

        app.Map("/afterrouting", b => b.Run(c =>
        {
            return c.Response.WriteAsync("Hello from middleware after routing");
        }));
    }

    // Do not call base implementations of these methods. Those are specific to endpoint routing.
    protected override void ConfigureMvcOptions(MvcOptions options)
    {
        options.EnableEndpointRouting = false;
    }

    protected override void ConfigureRoutingServices(IServiceCollection services)
    {
        // EndpointRoutingController is not compatible with old routing
        // Remove its action to avoid errors
        var actionDescriptorProvider = new RemoveControllerActionDescriptorProvider(
            new ControllerToRemove
            {
                ControllerType = typeof(EndpointRoutingController),
                Actions = null, // remove all
            },
            new ControllerToRemove
            {
                ControllerType = typeof(PageRouteController),
                Actions = new[] { nameof(PageRouteController.AttributeRoute) }
            });

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IActionDescriptorProvider>(actionDescriptorProvider));
    }
}
