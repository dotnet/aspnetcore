// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace LocalizationWebsite
{
    public class StartupResourcesInClassLibrary
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLocalization(options => options.ResourcesPath = "Resources");
        }

        public void Configure(
            IApplicationBuilder app,
            ILoggerFactory loggerFactory,
            IStringLocalizerFactory stringLocalizerFactory)
        {
            var supportedCultures = new List<CultureInfo>()
            {
                new CultureInfo("en-US"),
                new CultureInfo("fr-FR")
            };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-US"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });

            var noAttributeStringLocalizer = stringLocalizerFactory.Create(typeof(ResourcesClassLibraryNoAttribute.Model));
            var withAttributeStringLocalizer = stringLocalizerFactory.Create(typeof(Alternate.Namespace.Model));

            var noAttributeAssembly = typeof(ResourcesClassLibraryNoAttribute.Model).GetTypeInfo().Assembly;
            var noAttributeName = new AssemblyName(noAttributeAssembly.FullName).Name;
            var noAttributeNameStringLocalizer = stringLocalizerFactory.Create(
                nameof(ResourcesClassLibraryNoAttribute.Model),
                noAttributeName);

            var withAttributeAssembly = typeof(Alternate.Namespace.Model).GetTypeInfo().Assembly;
            var withAttributeName = new AssemblyName(withAttributeAssembly.FullName).Name;
            var withAttributeNameStringLocalizer = stringLocalizerFactory.Create(
                nameof(Alternate.Namespace.Model),
                withAttributeName);

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync(noAttributeNameStringLocalizer["Hello"]);
                await context.Response.WriteAsync(" ");
                await context.Response.WriteAsync(noAttributeStringLocalizer["Hello"]);
                await context.Response.WriteAsync(" ");
                await context.Response.WriteAsync(withAttributeNameStringLocalizer["Hello"]);
                await context.Response.WriteAsync(" ");
                await context.Response.WriteAsync(withAttributeStringLocalizer["Hello"]);
            });
        }
    }
}
