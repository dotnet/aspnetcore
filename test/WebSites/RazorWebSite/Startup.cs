// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Framework.DependencyInjection;

namespace RazorWebSite
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            // Set up application services
            app.UseServices(services =>
            {
                // Add MVC services to the services container
                services.AddMvc(configuration);
                services.AddTransient<InjectedHelper>();
                services.AddTransient<TaskReturningService>();
                services.AddTransient<FrameworkSpecificHelper>();
                services.Configure<RazorViewEngineOptions>(options =>
                {
                    var expander = new LanguageViewLocationExpander(
                            context => context.HttpContext.Request.Query["language-expander-value"]);
                    options.ViewLocationExpanders.Add(expander);
                    options.ViewLocationExpanders.Add(new CustomPartialDirectoryViewLocationExpander());
                });
            });

            // Add MVC to the request pipeline
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
