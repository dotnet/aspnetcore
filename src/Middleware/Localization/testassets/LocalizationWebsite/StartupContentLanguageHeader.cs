// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace LocalizationWebsite
{
    public class StartupContentLanguageHeader
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
                    new CultureInfo("ar-YE")
                },
                SupportedUICultures = new List<CultureInfo>()
                {
                    new CultureInfo("ar-YE")
                },
                ApplyCurrentCultureToResponseHeaders = true
            });

            app.Run(async (context) =>
            {
                var hasContentLanguageHeader = context.Response.Headers.ContainsKey(HeaderNames.ContentLanguage);
                var contentLanguage = context.Response.Headers[HeaderNames.ContentLanguage].ToString();

                await context.Response.WriteAsync(hasContentLanguageHeader.ToString());
                await context.Response.WriteAsync(" ");
                await context.Response.WriteAsync(contentLanguage);
            });
        }
    }
}