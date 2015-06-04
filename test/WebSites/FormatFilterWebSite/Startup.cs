// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace FormatFilterWebSite
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services to the services container
            services.AddMvc();
            services.ConfigureMvc(options =>
            {
                var formatFilter = new FormatFilterAttribute();
                options.Filters.Add(formatFilter);

                var customFormatter = new CustomFormatter("application/custom");
                options.OutputFormatters.Add(customFormatter);
            });
            services.ConfigureMvcFormatterMappings(options => 
            {
                options.FormatterMappings.SetMediaTypeMappingForFormat(
                    "custom",
                    MediaTypeHeaderValue.Parse("application/custom"));
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCultureReplacer();

            app.UseMvc(routes =>
            {
                routes.MapRoute("formatroute",
                                "{controller}/{action}/{id}.{format?}",
                                new { controller = "Home", action = "Index" });

                routes.MapRoute("optionalroute",
                                "{controller}/{action}.{format?}",
                                new { controller = "Home", action = "Index" });
            });            
        }
    }
}