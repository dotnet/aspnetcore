// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;

namespace RoutingWebSite
{
    public class Startup
    {
        public void Configure(IBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            app.UseServices(services =>
            {
                services.AddMvc(configuration);

                services.AddScoped<TestResponseGenerator>();
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute("areaRoute",
                                "{area:exists}/{controller}/{action}",
                                new { controller = "Home", action = "Index" });

                routes.MapRoute("ActionAsMethod", "{controller}/{action}",
                    defaults: new { controller = "Home", action = "Index" });

                routes.MapRoute(
                    "products",
                    "api/Products/{country}/{action}",
                    defaults: new { controller = "Products" });
            });
        }
    }
}
