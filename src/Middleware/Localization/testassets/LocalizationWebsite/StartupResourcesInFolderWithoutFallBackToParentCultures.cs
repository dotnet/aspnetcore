// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace LocalizationWebsite
{
    public class StartupResourcesInFolderWithoutFallBackToParentCultures
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLocalization(options => options.ResourcesPath = "Resources");
        }

        public void Configure(
            IApplicationBuilder app,
            IStringLocalizer<StartupResourcesInFolderWithoutFallBackToParentCultures> startupStringLocalizer)
        {
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-US"),
                SupportedCultures = new List<CultureInfo>()
                {
                    new CultureInfo("zh-TW"),
                    new CultureInfo("zh-Hant"),
                    new CultureInfo("zh")
                },
                SupportedUICultures = new List<CultureInfo>()
                {
                    new CultureInfo("zh-TW"),
                    new CultureInfo("zh-Hant"),
                    new CultureInfo("zh")
                },
                FallBackToParentCultures = false,
                FallBackToParentUICultures = false
            });

            app.Run(async (context) =>
            {
                var list = startupStringLocalizer.GetAllStrings(false);
                await context.Response.WriteAsync(startupStringLocalizer["Hello"]);
            });
        }
    }
}
