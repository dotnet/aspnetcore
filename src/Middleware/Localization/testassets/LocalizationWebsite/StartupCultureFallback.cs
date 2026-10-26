// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace LocalizationWebsite;

public class StartupCultureFallback
{
    public void Configure(IApplicationBuilder app)
    {
        app.UseRequestLocalization(options =>
        {
            options.SetDefaultCulture("fr-FR")
                .AddSupportedCultures("en")
                .AddSupportedUICultures("en");
        });

        app.Run(context => context.Response.WriteAsync($"{CultureInfo.CurrentCulture.Name} {CultureInfo.CurrentUICulture.Name}"));
    }
}
