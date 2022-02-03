// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace LocalizationWebsite;

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
            await context.Response.WriteAsync(10.ToString("C", CultureInfo.CurrentCulture));
        });
    }
}
