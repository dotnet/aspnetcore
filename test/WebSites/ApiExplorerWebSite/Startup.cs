// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.Formatters;
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

                options.OutputFormatters.Clear();
                options.OutputFormatters.Add(new JsonOutputFormatter());
                options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
            });

            services.AddSingleton<ApiExplorerDataFilter>();
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
            var application = new WebApplicationBuilder()
                .UseConfiguration(WebApplicationConfiguration.GetDefault(args))
                .UseStartup<Startup>()
                .Build();

            application.Run();
        }
    }
}
