// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
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
                .AddMvc(ConfigureMvcOptions)
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AddPageRoute("/PageRouteTransformer/PageWithConfiguredRoute", "/PageRouteTransformer/NewConventionRoute/{id?}");
                    options.Conventions.AddFolderRouteModelConvention("/PageRouteTransformer", model =>
                    {
                        pageRouteTransformerConvention.Apply(model);
                    });
                })
                .SetCompatibilityVersion(CompatibilityVersion.Latest);

            ConfigureRoutingServices(services);

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

                ConfigureConventionalTransformerRoute(routes);

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

                ConfigurePageRoute(routes);

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

        protected virtual void ConfigureConventionalTransformerRoute(IRouteBuilder routes)
        {
            routes.MapRoute(
                "ConventionalTransformerRoute",
                "ConventionalTransformerRoute/{controller:slugify}/{action=Index}/{param:slugify?}",
                defaults: null,
                constraints: new { controller = "ConventionalTransformer" });
        }

        protected virtual void ConfigurePageRoute(IRouteBuilder routes)
        {
            routes.MapRoute(
                "PageRoute",
                "{controller}/{action}/{page}",
                defaults: null,
                constraints: new { controller = "PageRoute" });
        }
    }
}
