// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;

namespace InlineConstraints
{
    public class Startup
    {
        public Action<IRouteBuilder> RouteCollectionProvider { get; set; }

        public void Configure(IApplicationBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            app.UseServices(services =>
            {
                services.AddMvc(configuration);
            });

            app.UseMvc(routes =>
            {
                // Used by tests for the 'exists' constraint.
                routes.MapRoute("areaExists-area", "area-exists/{area:exists}/{controller=Home}/{action=Index}");
                routes.MapRoute("areaExists", "area-exists/{controller=Home}/{action=Index}");
                routes.MapRoute("areaWithoutExists-area", "area-withoutexists/{area}/{controller=Home}/{action=Index}");
                routes.MapRoute("areaWithoutExists", "area-withoutexists/{controller=Home}/{action=Index}");
            });
        }
    }
}
