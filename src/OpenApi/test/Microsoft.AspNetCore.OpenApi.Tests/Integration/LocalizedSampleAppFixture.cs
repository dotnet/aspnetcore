// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

// Runs requests with a culture that uses commas to format decimals to
// verify the invariant culture is used to generate the OpenAPI document.

public sealed class LocalizedSampleAppFixture : SampleAppFixture
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.AddTransient<IStartupFilter, AddLocalizationMiddlewareFilter>();
            services.AddRequestLocalization((options) =>
            {
                options.DefaultRequestCulture = new("fr-FR");
                options.SupportedCultures = [new("fr-FR")];
                options.SupportedUICultures = [new("fr-FR")];
            });
        });
    }

    private sealed class AddLocalizationMiddlewareFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return (app) =>
            {
                app.UseRequestLocalization();
                next(app);
            };
        }
    }
}
