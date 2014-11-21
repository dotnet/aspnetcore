// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;

namespace UrlHelperWebSite
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            // Set up application services
            app.UseServices(services =>
            {
                services.Configure<AppOptions>(optionsSetup =>
                {
                    optionsSetup.ServeCDNContent = true;
                    optionsSetup.CDNServerBaseUrl = "http://cdn.contoso.com";
                    optionsSetup.GenerateLowercaseUrls = true;
                });

                // Add MVC services to the services container
                services.AddMvc(configuration);

                services.AddScoped<IUrlHelper, CustomUrlHelper>();
            });

            // Add MVC to the request pipeline
            app.UseMvc(routes =>
            {
                routes.MapRoute("Default", "{controller=Home}/{action=Index}");
            });
        }
    }
}
