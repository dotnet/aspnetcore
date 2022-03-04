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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace ControllersFromServicesWebSite
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var builder = services
                .AddControllersWithViews()
                .ConfigureApplicationPartManager(manager => manager.ApplicationParts.Clear())
                .AddApplicationPart(typeof(TimeScheduleController).GetTypeInfo().Assembly)
                .ConfigureApplicationPartManager(manager =>
                {
                    manager.ApplicationParts.Add(new TypesPart(
                      typeof(AnotherController),
                      typeof(ComponentFromServicesViewComponent),
                      typeof(InServicesTagHelper)));

                    var relatedAssenbly = RelatedAssemblyAttribute
                        .GetRelatedAssemblies(GetType().Assembly, throwOnError: true)
                        .SingleOrDefault();
                    foreach (var part in CompiledRazorAssemblyApplicationPartFactory.GetDefaultApplicationParts(relatedAssenbly))
                    {
                        manager.ApplicationParts.Add(part);
                    }
                })
                .AddControllersAsServices()
                .AddViewComponentsAsServices()
                .AddTagHelpersAsServices()
                .SetCompatibilityVersion(CompatibilityVersion.Latest);

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
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }

        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args)
                .Build();

            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseKestrel()
                .UseIISIntegration();
    }
}

