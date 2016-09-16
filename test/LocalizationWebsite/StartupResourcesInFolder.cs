// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using LocalizationWebsite.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace LocalizationWebsite
{
    public class StartupResourcesInFolder
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLocalization(options => options.ResourcesPath = "Resources");
        }

        public void Configure(
            IApplicationBuilder app,
            ILoggerFactory loggerFactory,
            IStringLocalizerFactory stringLocalizerFactory,
            IStringLocalizer<StartupResourcesInFolder> startupStringLocalizer,
            IStringLocalizer<Customer> custromerStringLocalizer)
        {
            loggerFactory.AddConsole(minLevel: LogLevel.Warning);

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-US"),
                SupportedCultures = new List<CultureInfo>()
                {
                    new CultureInfo("fr-FR")
                },
                SupportedUICultures = new List<CultureInfo>()
                {
                    new CultureInfo("fr-FR")
                }
            });

            var stringLocalizer = stringLocalizerFactory.Create("Test", location: null);
            var assembly = typeof(StartupResourcesInFolder).GetTypeInfo().Assembly;
            var assemblyName = new AssemblyName(assembly.FullName).Name;
            var stringLocalizerExplicitLocation = stringLocalizerFactory.Create("Test", assemblyName);

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync(startupStringLocalizer["Hello"]);
                await context.Response.WriteAsync(" ");
                await context.Response.WriteAsync(stringLocalizer["Hello"]);
                await context.Response.WriteAsync(" ");
                await context.Response.WriteAsync(custromerStringLocalizer["Hello"]);
                await context.Response.WriteAsync(" ");
                await context.Response.WriteAsync(stringLocalizerExplicitLocation["Hello"]);
            });
        }
    }
}
