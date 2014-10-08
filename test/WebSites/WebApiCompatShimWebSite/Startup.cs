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

            app.UsePerRequestServices(services =>
            {
                services.AddMvc(configuration);

                services.AddWebApiConventions();
            });
            
            app.UseMvc(routes =>
            {
                // Tests include different styles of WebAPI conventional routing and action selection - the prefix keeps
                // them from matching too eagerly.
                routes.MapRoute("named-action", "api/Blog/{controller}/{action}/{id?}");
                routes.MapRoute("unnamed-action", "api/Admin/{controller}/{id?}");
                routes.MapRoute("name-as-parameter", "api/Store/{controller}/{name?}");
                routes.MapRoute("extra-parameter", "api/Support/{extra}/{controller}/{id?}");
            });
        }
    }
}
