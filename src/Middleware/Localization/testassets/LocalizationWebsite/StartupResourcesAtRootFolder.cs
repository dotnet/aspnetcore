// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;
using LocalizationWebsite.Models;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;

namespace LocalizationWebsite;

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

        var location = typeof(LocalizationWebsite.StartupResourcesAtRootFolder).GetTypeInfo().Assembly.GetName().Name;
        var stringLocalizer = stringLocalizerFactory.Create("Test", location: location);

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
