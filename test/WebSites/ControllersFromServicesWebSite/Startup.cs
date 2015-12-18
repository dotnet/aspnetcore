// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using ControllersFromServicesClassLibrary;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace ControllersFromServicesWebSite
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc()
                .AddControllersAsServices(new[]
                {
                    typeof(TimeScheduleController).GetTypeInfo().Assembly
                });

            services.AddTransient<QueryValueService>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCultureReplacer();

            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{controller}/{action}/{id}");
            });
        }

        public static void Main(string[] args)
        {
            var application = new WebApplicationBuilder()
                .UseConfiguration(WebApplicationConfiguration.GetDefault(args))
                .UseStartup<Startup>()
                .Build();

            application.Run();
        }
    }
}
