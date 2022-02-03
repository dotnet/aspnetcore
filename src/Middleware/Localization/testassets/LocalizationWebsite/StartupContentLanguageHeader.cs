// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Net.Http.Headers;

namespace LocalizationWebsite;

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
            var contentLanguage = context.Response.Headers.ContentLanguage.ToString();

            await context.Response.WriteAsync(hasContentLanguageHeader.ToString());
            await context.Response.WriteAsync(" ");
            await context.Response.WriteAsync(contentLanguage);
        });
    }
}
