// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace LocalizationWebsite
{
    public class StartupCustomCulturePreserved
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLocalization();
        }

        public void Configure(
            IApplicationBuilder app)
        {
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-US"),
                SupportedCultures = new List<CultureInfo>()
                {
                    new CultureInfo("en-US") { NumberFormat= { CurrencySymbol = "kr" } }
                },
                SupportedUICultures = new List<CultureInfo>()
                {
                    new CultureInfo("en-US") { NumberFormat= { CurrencySymbol = "kr" } }
                }
            });

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync(10.ToString("C"));
            });
        }
    }
}
