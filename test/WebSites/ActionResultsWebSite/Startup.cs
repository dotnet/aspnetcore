// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ActionResultsWebSite
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc()
                .AddXmlDataContractSerializerFormatters();

            services.AddSingleton(new GuidLookupService());
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCultureReplacer();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action}/{id?}",
                    defaults: new { controller = "ActionResultsVerification", action = "Index" });

                routes.MapRoute(
                    name: "custom-route",
                    template: "foo/{controller}/{action}/{id?}");
            });
        }
    }
}