// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using ApiExplorerWebSite.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiExplorerWebSite
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ILoggerFactory, LoggerFactory>();
            services.AddMvc(options =>
            {
                options.Filters.AddService(typeof(ApiExplorerDataFilter));

                options.Conventions.Add(new ApiExplorerVisibilityEnabledConvention());
                options.Conventions.Add(new ApiExplorerVisibilityDisabledConvention(
                    typeof(ApiExplorerVisbilityDisabledByConventionController)));
                options.Conventions.Add(new ApiExplorerInboundOutboundConvention(
                    typeof(ApiExplorerInboundOutBoundController)));

                var jsonOutputFormatter = options.OutputFormatters.OfType<JsonOutputFormatter>().First();

                options.OutputFormatters.Clear();
                options.OutputFormatters.Add(jsonOutputFormatter);
                options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
            });

            services.AddSingleton<ApiExplorerDataFilter>();
            services.AddSingleton<IActionDescriptorChangeProvider>(ActionDescriptorChangeProvider.Instance);
            services.AddSingleton(ActionDescriptorChangeProvider.Instance);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCultureReplacer();

            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{controller}/{action}");
            });
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}

