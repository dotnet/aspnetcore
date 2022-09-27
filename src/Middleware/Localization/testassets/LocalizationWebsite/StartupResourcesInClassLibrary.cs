// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;

namespace LocalizationWebsite;

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
