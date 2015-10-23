// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Localization;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using MvcSample.Web.Filters;
using MvcSample.Web.Services;

namespace MvcSample.Web
{
    public class Startup
    {
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddCaching();
            services.AddSession();

            services.AddMvc(options =>
            {
                options.Filters.Add(typeof(PassThroughAttribute), order: 17);
                options.Filters.Add(new FormatFilterAttribute());
            })
            .AddXmlDataContractSerializerFormatters()
            .AddViewLocalization(LanguageViewLocationExpanderFormat.SubFolder);

            services.AddSingleton<PassThroughAttribute>();
            services.AddSingleton<UserNameService>();
            services.AddTransient<ITestService, TestService>();

            return services.BuildServiceProvider();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseStatusCodePages();
            app.UseFileServer();

            app.UseRequestLocalization(new RequestCulture("en-US"));

            app.UseSession();
            app.UseMvc(routes =>
            {
                routes.MapRoute("areaRoute", "{area:exists}/{controller}/{action}");
                routes.MapRoute(
                    "controllerActionRoute",
                    "{controller}/{action}",
                    new { controller = "Home", action = "Index" },
                    constraints: null,
                    dataTokens: new { NameSpace = "default" });

                routes.MapRoute(
                    "controllerRoute",
                    "{controller}",
                    new { controller = "Home" });
            });
        }
    }
}
