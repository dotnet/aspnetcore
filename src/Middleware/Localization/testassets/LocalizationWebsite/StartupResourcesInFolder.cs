// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;
using LocalizationWebsite.Models;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;

namespace LocalizationWebsite;

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
        IStringLocalizer<Customer> custromerStringLocalizer,
        // This localizer is used in tests to prevent a regression of https://github.com/aspnet/Localization/issues/293
        // Namely that english was always being returned if it existed.
        IStringLocalizer<StartupCustomCulturePreserved> customCultureLocalizer)
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

        var assemblyName = typeof(StartupResourcesInFolder).GetTypeInfo().Assembly.GetName().Name;
        var stringLocalizer = stringLocalizerFactory.Create("Test", assemblyName);

        app.Run(async (context) =>
        {
            await context.Response.WriteAsync(startupStringLocalizer["Hello"]);
            await context.Response.WriteAsync(" ");
            await context.Response.WriteAsync(stringLocalizer["Hello"]);
            await context.Response.WriteAsync(" ");
            await context.Response.WriteAsync(custromerStringLocalizer["Hello"]);
            await context.Response.WriteAsync(" ");
            await context.Response.WriteAsync(customCultureLocalizer["Hello"]);
        });
    }
}
