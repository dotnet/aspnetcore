// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Collections.Generic;
using System.Globalization;
using LocalizationWebsite.Models;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace LocalizationWebsite
{
    public class StartupResourcesAtRootFolder
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLocalization();
        }

        public void Configure(
            IApplicationBuilder app,
            ILoggerFactory loggerFactory,
            IStringLocalizerFactory stringLocalizerFactory,
            IStringLocalizer<StartupResourcesAtRootFolder> startupStringLocalizer,
            IStringLocalizer<Customer> customerStringLocalizer)
        {
            loggerFactory.AddConsole(minLevel: LogLevel.Warning);

            var options = new RequestLocalizationOptions
            {
                SupportedCultures = new List<CultureInfo>()
                {
                    new CultureInfo("fr-FR")
                },
                SupportedUICultures = new List<CultureInfo>()
                {
                    new CultureInfo("fr-FR")
                }
            };

            app.UseRequestLocalization(options, defaultRequestCulture: new RequestCulture("en-US"));

            var stringLocalizer = stringLocalizerFactory.Create("Test", location: null);

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync(startupStringLocalizer["Hello"]);
                await context.Response.WriteAsync(" ");
                await context.Response.WriteAsync(stringLocalizer["Hello"]);
                await context.Response.WriteAsync(" ");
                await context.Response.WriteAsync(customerStringLocalizer["Hello"]);
            });
        }
    }
}
