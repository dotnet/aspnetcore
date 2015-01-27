// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using ControllersFromServicesClassLibrary;

#if ASPNET50
using Autofac;
using Microsoft.Framework.DependencyInjection.Autofac;
#endif

namespace ControllersFromServicesWebSite
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            app.UseServices(services =>
            {
                services.AddMvc(configuration)
                        .WithControllersAsServices(
                         new[]
                         {
                            typeof(TimeScheduleController).GetTypeInfo().Assembly
                         });

                services.AddTransient<QueryValueService>();

#if ASPNET50
                // Create the autofac container
                var builder = new ContainerBuilder();

                // Create the container and use the default application services as a fallback
                AutofacRegistration.Populate(
                    builder,
                    services);

                return builder.Build()
                              .Resolve<IServiceProvider>();
#endif
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{controller}/{action}/{id}");
            });
        }
    }
}