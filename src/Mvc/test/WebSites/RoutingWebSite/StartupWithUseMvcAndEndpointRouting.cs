// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RoutingWebSite
{
    public class StartupWithUseMvcAndEndpointRouting : Startup
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
                    "ConventionalTransformerRoute",
                    "ConventionalTransformerRoute/{controller:slugify}/{action=Index}/{param:slugify?}",
                    defaults: null,
                    constraints: new { controller = "ConventionalTransformer" });

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
                    "PageRoute",
                    "{controller}/{action}/{page}",
                    defaults: null,
                    constraints: new { controller = "PageRoute" });

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
    }
}
