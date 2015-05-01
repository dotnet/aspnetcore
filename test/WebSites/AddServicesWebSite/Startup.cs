// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;

namespace AddServicesWebSite
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseCultureReplacer();

            // Not calling AddMvc() here.
            // The purpose of the Website is to demonstrate that it throws
            // when AddMvc() is not called.

            // Add MVC to the request pipeline
            app.UseMvc(routes =>
            {
                routes.MapRoute("areaRoute",
                                "{area:exists}/{controller}/{action}",
                                new { controller = "Home", action = "Index" });

                routes.MapRoute("ActionAsMethod", "{controller}/{action}",
                    defaults: new { controller = "Home", action = "Index" });

            });
        }
    }
}
