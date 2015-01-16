// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
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
        public void Configure(IApplicationBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            app.UseServices(services =>
            {
                services.AddMvc(configuration);
                services.Configure<MvcOptions>(options =>
                {
                    var formatFilter = new FormatFilterAttribute();
                    options.Filters.Add(formatFilter);

                    var customFormatter = new CustomFormatter("application/custom");
                    options.OutputFormatters.Add(customFormatter);

                    options.FormatterMappings.SetFormatMapping(
                        "custom", 
                        MediaTypeHeaderValue.Parse("application/custom"));
                });
            });
            
            app.UseMvc(routes =>
            {
                routes.MapRoute("formatroute",
                                "{controller}/{action}/{id}.{format?}",
                                new { controller = "Home", action = "Index" });
            });            
        }
    }
}