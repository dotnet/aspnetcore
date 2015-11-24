// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace UrlHelperSample.Web
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppOptions>(optionsSetup =>
            {
                optionsSetup.ServeCDNContent = true;
                optionsSetup.CDNServerBaseUrl = "http://cdn.contoso.com";
                optionsSetup.GenerateLowercaseUrls = true;
            });

            // Add MVC services to the services container
            services.AddMvc();

            services.AddSingleton<IUrlHelperFactory, CustomUrlHelperFactory>();
        }

        public void Configure(IApplicationBuilder app)
        {
            // Add MVC to the request pipeline
            app.UseMvc(routes =>
            {
                routes.MapRoute("Default", "{controller=Home}/{action=Index}");
            });
        }
    }
}
