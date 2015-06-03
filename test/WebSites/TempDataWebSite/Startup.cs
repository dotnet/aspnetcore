// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;

namespace TempDataWebSite
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCaching();
            services.AddSession();
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCultureReplacer();

            app.UseSession();
            app.UseMvcWithDefaultRoute();
        }
    }
}
