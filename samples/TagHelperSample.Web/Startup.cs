// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using TagHelperSample.Web.Services;

namespace TagHelperSample.Web
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddSingleton<MoviesService>();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole((name, logLevel) =>
                name.StartsWith("Microsoft.AspNet.Mvc.TagHelpers", StringComparison.OrdinalIgnoreCase)
                || (name.StartsWith("Microsoft.Net.Http.Server.WebListener", StringComparison.OrdinalIgnoreCase)
                    && logLevel >= LogLevel.Information));

            app.UseErrorPage();

            app.UseStaticFiles();

            app.UseMvcWithDefaultRoute();
        }
    }
}
