// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using LocalizationWebsite.Models;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;

namespace LocalizationWebsite;

public class StartupGetAllStrings
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");
    }

    public void Configure(
        IApplicationBuilder app,
        ILoggerFactory loggerFactory,
        IStringLocalizer<Customer> customerStringLocalizer)
    {
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

        app.Run(async (context) =>
        {
            var strings = customerStringLocalizer.GetAllStrings();

            await context.Response.WriteAsync(strings.Count().ToString(CultureInfo.InvariantCulture));
            await context.Response.WriteAsync(" ");
            await context.Response.WriteAsync(string.Join(" ", strings));
        });
    }
}
