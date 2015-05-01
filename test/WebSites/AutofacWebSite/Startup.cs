// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Autofac;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Autofac;

namespace AutofacWebSite
{
    public class Startup
    {
        // Set up application services
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddTransient<HelloWorldBuilder>();

            var builder = new ContainerBuilder();
            AutofacRegistration.Populate(builder,
                                         services);

            var container = builder.Build();

            return container.Resolve<IServiceProvider>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCultureReplacer();

            app.UseMvc(routes =>
            {
                // This default route is for running the project directly.
                routes.MapRoute("default", "{controller=DI}/{action=Index}");
            });
        }
    }
}
