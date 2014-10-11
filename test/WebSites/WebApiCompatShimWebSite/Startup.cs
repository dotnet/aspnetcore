// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;

namespace WebApiCompatShimWebSite
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            app.UseServices(services =>
            {
                services.AddMvc(configuration);
                services.AddWebApiConventions();
            });

            app.UseMvc(routes =>
            {
                // This route can't access any of our webapi controllers
                routes.MapRoute("default", "{controller}/{action}/{id?}");

                // Tests include different styles of WebAPI conventional routing and action selection - the prefix keeps
                // them from matching too eagerly.
                routes.MapWebApiRoute("named-action", "api/Blog/{controller}/{action}/{id?}");
                routes.MapWebApiRoute("unnamed-action", "api/Admin/{controller}/{id?}");
                routes.MapWebApiRoute("name-as-parameter", "api/Store/{controller}/{name?}");
                routes.MapWebApiRoute("extra-parameter", "api/Support/{extra}/{controller}/{id?}");
            });
        }
    }
}
