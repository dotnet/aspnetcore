// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace LocalizationSample.Web
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc()
                .AddViewLocalization(options => options.ResourcesPath = "Resources")
                .AddDataAnnotationsLocalization();
        }

        public void Configure(IApplicationBuilder app)
        {
            var options = new RequestLocalizationOptions
            {
                SupportedCultures = new List<CultureInfo>
                {
                    new CultureInfo("fr"),
                    new CultureInfo("en-GB")
                },
                SupportedUICultures = new List<CultureInfo>
                {
                    new CultureInfo("fr"),
                    new CultureInfo("en-GB")
                }
            };
            app.UseRequestLocalization(options, new RequestCulture("en-US"));

            app.UseMvcWithDefaultRoute();
        }
    }
}
