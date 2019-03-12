// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
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
                .AddNewtonsoftJson()
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

        public virtual void Configure(IApplicationBuilder app)
        {
            app.UseRouting(routes =>
            {
                routes.MapControllerRoute(
                    "NonParameterConstraintRoute",
                    "NonParameterConstraintRoute/{controller}/{action}",
                    defaults: null,
                    constraints: new { controller = "NonParameterConstraint", nonParameter = new QueryStringConstraint() });

                routes.MapControllerRoute(
                    "DataTokensRoute",
                    "DataTokensRoute/{controller}/{action}",
                    defaults: null,
                    constraints: new { controller = "DataTokens" },
                    dataTokens: new { hasDataTokens = true });

                routes.MapControllerRoute(
                    "ConventionalTransformerRoute",
                    "ConventionalTransformerRoute/{controller:slugify}/{action=Index}/{param:slugify?}",
                    defaults: null,
                    constraints: new { controller = "ConventionalTransformer" });

                routes.MapControllerRoute(
                    "DefaultValuesRoute_OptionalParameter",
                    "DefaultValuesRoute/Optional/{controller=DEFAULTVALUES}/{action=OPTIONALPARAMETER}/{id?}/{**catchAll}",
                    defaults: null,
                    constraints: new { controller = "DefaultValues", action = "OptionalParameter" });

                routes.MapControllerRoute(
                    "DefaultValuesRoute_DefaultParameter",
                    "DefaultValuesRoute/Default/{controller=DEFAULTVALUES}/{action=DEFAULTPARAMETER}/{id=17}/{**catchAll}",
                    defaults: null,
                    constraints: new { controller = "DefaultValues", action = "DefaultParameter" });

                routes.MapAreaControllerRoute(
                    "flightRoute",
                    "adminRoute",
                    "{area:exists}/{controller}/{action}",
                    defaults: new { controller = "Home", action = "Index" },
                    constraints: new { area = "Travel" });

                routes.MapControllerRoute(
                    "PageRoute",
                    "{controller}/{action}/{page}",
                    defaults: null,
                    constraints: new { controller = "PageRoute" });

                routes.MapControllerRoute(
                    "ActionAsMethod",
                    "{controller}/{action}",
                    defaults: new { controller = "Home", action = "Index" });

                routes.MapControllerRoute(
                    "RouteWithOptionalSegment",
                    "{controller}/{action}/{path?}");

                routes.MapRazorPages();
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
}
