// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace RazorWebSite;

public class StartupDataAnnotations
{
    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");

        services
            .AddMvc()
            .AddViewLocalization()
            .AddDataAnnotationsLocalization((options) =>
            {
                options.DataAnnotationLocalizerProvider =
                    (modelType, stringLocalizerFactory) => stringLocalizerFactory.Create(typeof(SingleType));
            });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app)
    {
        app.UseDeveloperExceptionPage();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture("en-US", "en-US"),
            SupportedCultures = new List<CultureInfo>
                {
                    new CultureInfo("en-US")
                },
            SupportedUICultures = new List<CultureInfo>
                {
                    new CultureInfo("en-US")
                }
        });

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
        });
    }
}
