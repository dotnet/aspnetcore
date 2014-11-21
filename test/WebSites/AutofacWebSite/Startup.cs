// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Autofac;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Autofac;

namespace AutofacWebSite
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            app.UseServices(services =>
            {
                services.AddMvc(configuration);
                services.AddTransient<HelloWorldBuilder>();

                var builder = new ContainerBuilder();
                AutofacRegistration.Populate(builder,
                                             services);

                var container = builder.Build();

                return container.Resolve<IServiceProvider>();
            });

            app.UseMvc(routes =>
            {
                // This default route is for running the project directly.
                routes.MapRoute("default", "{controller=DI}/{action=Index}");
            });
        }
    }
}
