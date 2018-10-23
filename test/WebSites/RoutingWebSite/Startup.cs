// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace RoutingWebSite
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            var pageRouteTransformerConvention = new PageRouteTransformerConvention(new SlugifyParameterTransformer());

            services
                .AddMvc(options =>
                {
                    // Add route token transformer to one controller
                    options.Conventions.Add(new ControllerRouteTokenTransformerConvention(
                        typeof(ParameterTransformerController),
                        new SlugifyParameterTransformer()));
                })
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AddPageRoute("/PageRouteTransformer/PageWithConfiguredRoute", "/PageRouteTransformer/NewConventionRoute/{id?}");
                    options.Conventions.AddFolderRouteModelConvention("/PageRouteTransformer", model =>
                    {
                        pageRouteTransformerConvention.Apply(model);
                    });
                })
                .SetCompatibilityVersion(CompatibilityVersion.Latest);
            services
                .AddRouting(options =>
                {
                    options.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer);
                });

            services.AddScoped<TestResponseGenerator>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvc(routes =>
            {
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