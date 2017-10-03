// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ControllersFromServicesClassLibrary;
using ControllersFromServicesWebSite.Components;
using ControllersFromServicesWebSite.TagHelpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace ControllersFromServicesWebSite
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var builder = services
                .AddMvc()
                .ConfigureApplicationPartManager(manager => manager.ApplicationParts.Clear())
                .AddApplicationPart(typeof(TimeScheduleController).GetTypeInfo().Assembly)
                .ConfigureApplicationPartManager(manager =>
                {
                    manager.ApplicationParts.Add(new TypesPart(
                      typeof(AnotherController),
                      typeof(ComponentFromServicesViewComponent),
                      typeof(InServicesTagHelper)));

                    manager.FeatureProviders.Add(new AssemblyMetadataReferenceFeatureProvider());
                })
                .AddControllersAsServices()
                .AddViewComponentsAsServices()
                .AddTagHelpersAsServices();

            services.AddTransient<QueryValueService>();
            services.AddTransient<ValueService>();
            services.AddHttpContextAccessor();
        }

        private class TypesPart : ApplicationPart, IApplicationPartTypeProvider
        {
            public TypesPart(params Type[] types)
            {
                Types = types.Select(t => t.GetTypeInfo());
            }

            public override string Name => string.Join(", ", Types.Select(t => t.FullName));

            public IEnumerable<TypeInfo> Types { get; }
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{controller}/{action}/{id}");
            });
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseKestrel()
                .UseIISIntegration()
                .Build();

            host.Run();
        }
    }
}

