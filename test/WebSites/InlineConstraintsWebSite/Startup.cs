// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Constraints;
using Microsoft.Framework.DependencyInjection;

namespace InlineConstraints
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            app.UseServices(services =>
            {
                services.AddMvc(configuration);
            });

            app.UseErrorReporter();

            app.UseMvc(routes =>
            {
                routes.MapRoute("StoreId",
                        "store/{action}/{id:guid?}",
                        defaults: new { controller = "InlineConstraints_Store" });

                routes.MapRoute("StoreLocation",
                        "store/{action}/{location:minlength(3):maxlength(10)}",
                        defaults: new { controller = "InlineConstraints_Store" },
                        constraints: new { location = new AlphaRouteConstraint() });

                // Used by tests for the 'exists' constraint.
                routes.MapRoute("areaExists-area", "area-exists/{area:exists}/{controller=Home}/{action=Index}");
                routes.MapRoute("areaExists", "area-exists/{controller=Home}/{action=Index}");
                routes.MapRoute("areaWithoutExists-area", "area-withoutexists/{area}/{controller=Home}/{action=Index}");
                routes.MapRoute("areaWithoutExists", "area-withoutexists/{controller=Home}/{action=Index}");
            });
        }
    }
}
