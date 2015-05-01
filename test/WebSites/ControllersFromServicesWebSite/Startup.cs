// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using ControllersFromServicesClassLibrary;

#if DNX451
using Autofac;
using Microsoft.Framework.DependencyInjection.Autofac;
#endif

namespace ControllersFromServicesWebSite
{
    public class Startup
    {
        // Set up application services
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                    .WithControllersAsServices(
                     new[]
                     {
                            typeof(TimeScheduleController).GetTypeInfo().Assembly
                     });

            services.AddTransient<QueryValueService>();

#if DNX451
            // Create the autofac container
            var builder = new ContainerBuilder();

            // Create the container and use the default application services as a fallback
            AutofacRegistration.Populate(
                builder,
                services);

            return builder.Build()
                          .Resolve<IServiceProvider>();
#else
            return services.BuildServiceProvider();
#endif
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
