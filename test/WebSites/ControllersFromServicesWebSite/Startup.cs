// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using ControllersFromServicesClassLibrary;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;

namespace ControllersFromServicesWebSite
{
    public class Startup
    {
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc()
                .AddControllersAsServices(new[]
                {
                    typeof(TimeScheduleController).GetTypeInfo().Assembly
                });

            services.AddTransient<QueryValueService>();

            return services.BuildServiceProvider();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCultureReplacer();

            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{controller}/{action}/{id}");
            });
        }
    }
}
