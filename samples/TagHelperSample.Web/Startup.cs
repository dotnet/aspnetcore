// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Logging.Console;
using TagHelperSample.Web.Services;

namespace TagHelperSample.Web
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.UseServices(services =>
            {
                services.AddMvc();

                // Setup services with a test AssemblyProvider so that only the sample's assemblies are loaded. This
                // prevents loading controllers from other assemblies when the sample is used in functional tests.
                services.AddTransient<IAssemblyProvider, TestAssemblyProvider<Startup>>();
                services.AddSingleton<MoviesService>();
            });

            loggerFactory.AddConsole((name, logLevel) =>
                name.StartsWith("Microsoft.AspNet.Mvc.TagHelpers", StringComparison.OrdinalIgnoreCase)
                || (name.StartsWith("Microsoft.Net.Http.Server.WebListener", StringComparison.OrdinalIgnoreCase)
                    && logLevel >= LogLevel.Information));

            app.UseErrorPage();

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
